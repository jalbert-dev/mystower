using System.Collections.Generic;
using Util.Functional;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Linq;
using System.Reflection;
using System;
using System.Text.RegularExpressions;
using System.Runtime.Serialization;

namespace Util.Error
{
    public abstract class ObjectValidationError : IError
    {
        public abstract string InnerMessage { get; }

        protected readonly string objectKey;
        public ObjectValidationError(string objKey) => objectKey = objKey;
        public string Message => $"Error validating {objectKey}: {InnerMessage}";
    }

    public class NoDatabaseForType : IError
    {
        readonly string typename;
        public NoDatabaseForType(string n) => typename = n;
        public string Message => $"No database exists for type '{typename}'!";
    }

    public class KeyNotFoundInDatabase : IError
    {
        readonly string typename;
        readonly string key;
        public KeyNotFoundInDatabase(string n, string k)
            => (typename, key) = (n, k);
        public string Message => $"No key '{key}' exists in database for '{typename}'!";
    }

    public class DatabaseReverseLookupFailed<T> : IError
    {
        readonly string typename;
        readonly T value;
        public DatabaseReverseLookupFailed(string n, T k)
            => (typename, value) = (n, k);
        public string Message => $"Failed to reverse-lookup key corresponding to '{value}' in database for '{typename}'!";
    }

    public class JsonParseFailed : IError
    {
        public readonly JsonException Exception;
        public JsonParseFailed(JsonException exception) => Exception = exception;
        public string Message => $"Parse error: '{Exception.Message}'";
    }

    public class ArchetypeNotFound : ObjectValidationError
    {
        readonly string archetypeName;
        public ArchetypeNotFound(string objKey, string n) : base(objKey) 
            => archetypeName = n;
        public override string InnerMessage => $"No archetype found by name '{archetypeName}'!";
    }

    public class FieldNotFound : ObjectValidationError
    {
        readonly string objKey;
        readonly string fieldName;
        public FieldNotFound(string key, FieldInfo field) : base(key)
            => (objKey, fieldName) = (key, field.Name);
        public FieldNotFound(string key, string field) : base(key)
            => (objKey, fieldName) = (key, field);
        public override string InnerMessage => $"Field '{fieldName}' not found in '{objKey}' or any parent archetype.";
    }

    public class ArchetypeCycleDetected : ObjectValidationError
    {
        // TODO: Should probably take some info regarding the cycle ("a -> b -> c -> a ...")
        public ArchetypeCycleDetected(string objKey) : base(objKey) { }
        public override string InnerMessage => "Archetype cycle detected!";
    }

    public class NodeNotJsonObject : ObjectValidationError
    {
        public NodeNotJsonObject(string objKey) : base(objKey) { }
        public override string InnerMessage => "Dictionary value must be an object.";
    }

    public class ArchetypeFieldNotString : ObjectValidationError
    {
        public ArchetypeFieldNotString(string objKey) : base(objKey) { }
        public override string InnerMessage => "Archetype ID must be a string!";
    }

    public class RootNotJsonObject : IError
    {
        public string Message => "Root JSON node must be an object!";
    }
}

namespace Util
{
    public class Database
    {
        private struct DBPair<T>
        {
            public Dictionary<string, T> lookup;
            public Dictionary<T, string> reverse;

            public DBPair(Dictionary<string, T> d)
            {
                lookup = d;
                reverse = lookup.ToDictionary(pair => pair.Value, pair => pair.Key);
            }
        }

        private readonly Dictionary<Type, object> databases = new Dictionary<Type, object>();

        public void AddDatabase<T>(Dictionary<string, T> d)
        {
            databases[typeof(T)] = new DBPair<T>(d);
        }

        public Result<T> Lookup<T>(string key)
             => from DBPair<T> db in databases.TryGetValue(typeof(T))
                    .ErrorIfNone(() => new Error.NoDatabaseForType(typeof(T).Name))
                from value in db.lookup.TryGetValue(key)
                    .ErrorIfNone(() => new Error.KeyNotFoundInDatabase(typeof(T).Name, key))
                select value;

        public Result<string> LookupKey<T>(T value)
             => from DBPair<T> db in databases.TryGetValue(typeof(T))
                    .ErrorIfNone(() => new Error.NoDatabaseForType(typeof(T).Name))
                from key in db.reverse.TryGetValue(value)
                    .ErrorIfNone(() => new Error.DatabaseReverseLookupFailed<T>(typeof(T).Name, value))
                select key;
    }

    public class DatabaseTypeConverter<T> : Newtonsoft.Json.JsonConverter<T> where T : class
    {
        private static Util.Database GetContextDatabase(JsonSerializer serializer)
        {
            var lookup = (serializer.Context.Context as Util.Database);
            if (lookup == null)
                throw new JsonException("JSON serializer not supplied with database context!");
            return lookup;
        }

        public override void WriteJson(JsonWriter writer, T? value, JsonSerializer serializer)
        {
            if (value == null)
                return;

            GetContextDatabase(serializer)
                .LookupKey(value)
                .Match(
                    ok: key => writer.WriteValue(key),
                    err: err => throw new JsonException($"Exception looking up key for instance of '{typeof(T).FullName}': {err.Message}")
                );
        }

        public override T ReadJson(JsonReader reader, Type objectType, T? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.Value == null)
                return null!;

            var key = reader.Value as string;

            if (key == null)
                throw new JsonException($"Unable to read '{typeof(T).FullName}' string key from JSON.");

            return GetContextDatabase(serializer)
                .Lookup<T>(key)
                .Match(
                    ok: obj => obj,
                    err: err => throw new JsonException($"No '{typeof(T).FullName}' found in database by key '{key}'.")
                );
        }
    }

    public static class ArchetypeJson
    {
        private static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> self, out TKey key, out TValue value)
            => (key, value) = (self.Key, self.Value);

        private static Result<JToken> ValidateIsStringToken(JToken token, string contextNodeName)
             => token.Type == JTokenType.String ? 
                    Result.Ok(token) :
                    Result.Error(new Error.ArchetypeFieldNotString(contextNodeName));

        private static Result<(string id, JToken token)> FindArchetypeNodeByName(JObject rootNode, string id, string contextNodeName)
             => rootNode.TryGetValue(id)
                    .Map(n => (id, n!))
                    .ErrorIfNone(() => new Error.ArchetypeNotFound(contextNodeName, id));

        private static Result<(string, JObject)> ConvertToJObject(string id, JToken token)
             => (token as JObject)
                .ErrorIfNull(() => new Error.NodeNotJsonObject(id))
                .Map(obj => (id, obj) );

        private static Result<Option<(string, JObject)>> GetArchetypeNode(JObject rootNode, string nodeId, JObject node)
            // note that we can't just load the archetype object into the database
            // recursively because archetypes are not required to have all
            // fields populated.
             => node.TryGetValue("_archetype").Match(
                    some: archetypeToken => 
                            ValidateIsStringToken(archetypeToken!, nodeId)
                            .Map(token => token.ToObject<string>()!)
                            .Bind(id => FindArchetypeNodeByName(rootNode, id, nodeId))
                            .Bind(pair => ConvertToJObject(pair.id, pair.token))
                            .Map(Option.Some),
                    none: () => 
                        Result.Ok(Option<(string, JObject)>.None()) );

        private static Result<List<JObject>> GetArchetypeHierarchy(JObject rootNode, string nodeId, JObject node, List<JObject> hierarchy)
             => Result.Ok(() => hierarchy.Add(node))
                    .Bind(_ => GetArchetypeNode(rootNode, nodeId, node))
                    .Bind(nameNodeOpt => nameNodeOpt.Match(
                        none: () => Result.Ok(hierarchy),
                        some: nameNodePair => {
                            if (hierarchy.Contains(nameNodePair.Item2))
                                return Result.Error(new Error.ArchetypeCycleDetected(nodeId));
                            return GetArchetypeHierarchy(rootNode, nameNodePair.Item1, nameNodePair.Item2, hierarchy);
                        }
                    ));

        private static Result<List<JObject>> GetArchetypeHierarchy(JObject rootNode, string nodeId, JObject node)
            => GetArchetypeHierarchy(rootNode, nodeId, node, new List<JObject>(4));
        
        private static Result<JToken> FindPropertyTokenInArchetypes(string leafName, List<JObject> archetypes, string propName)
        {
            foreach(var obj in archetypes)
            {
                var valueToken = obj.TryGetValue(propName);
                if (!valueToken.IsNone)
                    return Result.Ok(valueToken.Value!);
            }
            return Result.Error(new Error.FieldNotFound(leafName, propName));
        }

        private static T WithValue<T>(this T obj, FieldInfo field, object? value)
        {
            field.SetValue(obj, value);
            return obj;
        }

        private static Result<T> LoadDataFromArchetypes<T>(string leafName, List<JObject> archetypes)
             => typeof(T)
                    .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .FoldBind((T)FormatterServices.GetUninitializedObject(typeof(T)),
                    (newObj, field) =>
                        FindPropertyTokenInArchetypes(leafName, archetypes, field.Name)
                        .Map(token => token.ToObject(field.FieldType))
                        .Map(value => newObj.WithValue(field, value)));

        private static Result<T> CreateObjectFrom<T>(JObject rootNode, string id, JToken token)
             => (token as JObject)
                .ErrorIfNull(() => new Error.NodeNotJsonObject(id))
                .Bind(obj => GetArchetypeHierarchy(rootNode, id, obj))
                .Bind(archetypes => LoadDataFromArchetypes<T>(id, archetypes));

        private static Dictionary<T,U> WithKeyValue<T,U>(this Dictionary<T, U> self, T key, U value)
        {
            self[key] = value;
            return self;
        }

        private static string StripComments(string str)
            => Regex.Replace(str, @"^\s*#.*$", "", RegexOptions.Multiline);

        private static Result<JObject> ParseJsonString(string str)
        {
            try
            {
                return Result.Ok(JObject.Parse(str));
            }
            catch (JsonException ex)
            {
                return Result.Error(new Error.JsonParseFailed(ex));
            }
        }

        private static Result<Dictionary<string, T>> LoadNodesFromRoot<T>(JObject rootNode)
             => (rootNode as IEnumerable<KeyValuePair<string, JToken?>>)
                // object with id starting with "__" are only for being
                // archetypes of other objects, so we aren't guaranteed to
                // be able to load them (nor should we)
                .Where(kv => !kv.Key.StartsWith("__"))
                .FoldBind(new Dictionary<string, T>(rootNode.Children().Count()),
                    (db, kv) =>
                        CreateObjectFrom<T>(rootNode, kv.Key, kv.Value!)
                        .Map(newObject => db.WithKeyValue(kv.Key, newObject)));

        public static Result<Dictionary<string, T>> Read<T>(string str)
             => ParseJsonString(StripComments(str)).Bind(LoadNodesFromRoot<T>);
    }
}