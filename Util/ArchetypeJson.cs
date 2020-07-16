using System.Collections.Generic;
using Util.Functional;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Linq;
using System.Reflection;
using System;
using System.Text.RegularExpressions;

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
        private readonly Dictionary<Type, object> databases = new Dictionary<Type, object>();

        public void AddDatabase<T>(Dictionary<string, T> d) => databases[typeof(T)] = d;

        public Result<T> Lookup<T>(string key)
             => from Dictionary<string, T> db in databases.TryGetValue(typeof(T)).ErrorIfNone(() => new Error.NoDatabaseForType(typeof(T).Name))
                from value in db.TryGetValue(key).ErrorIfNone(() => new Error.KeyNotFoundInDatabase(typeof(T).Name, key))
                select value;
    }

    public static class ArchetypeJson
    {
        private static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> self, out TKey key, out TValue value)
            => (key, value) = (self.Key, self.Value);
        
        public static Result<T> ErrorIfNotNull<T>(this T? item, Func<IError> ifNull) where T : class
            => item == null ? Result.Error(ifNull()) : Result.Ok(item);

        private static Result<Option<(string, JObject)>> GetArchetypeNode(JObject rootNode, string nodeId, JObject node)
            // note that we can't just load the archetype object into the database
            // recursively because archetypes are not required to have all
            // fields populated.
             => node.TryGetValue("_archetype").Match(
                    some: archetypeToken => 
                        Result.Ok(archetypeToken!)
                            .Bind(token => 
                                token.Type != JTokenType.String ? Result.Error(new Error.ArchetypeFieldNotString(nodeId)) : Result.Ok(token))
                            .Map(token => 
                                token.ToObject<string>()!)
                            .Bind(id => 
                                rootNode
                                .TryGetValue(id)
                                .Map(n => (id, n!))
                                .ErrorIfNone(() => new Error.ArchetypeNotFound(nodeId, id)))
                            .Map(pair => 
                                (pair.id, pair.Item2 as JObject))
                            .Bind(pair => 
                                pair.Item2
                                .ErrorIfNotNull(() => new Error.NodeNotJsonObject(pair.id))
                                .Map(obj => Option.Some( (pair.id, obj) ))),
                    none: () => 
                        Result.Ok(Option<(string, JObject)>.None()) );

        private static IError? GetArchetypeHierarchy(JObject rootNode, string nodeId, JObject node, List<JObject> hierarchy)
        {
            hierarchy.Add(node);

            var archNodeResult = GetArchetypeNode(rootNode, nodeId, node);
            if (!archNodeResult.IsSuccess)
                return archNodeResult.Err;

            if (archNodeResult.Value.IsNone)
                return null;
                
            var (archId, archNode) = archNodeResult.Value.Value;
            if (hierarchy.Contains(archNode))
                return new Error.ArchetypeCycleDetected(nodeId);

            var err = GetArchetypeHierarchy(rootNode, archId!, archNode, hierarchy);
            if (err != null)
                return err;

            return null;
        }

        private static Result<JToken> FindInArchetypes(string leafName, List<JObject> archetypes, string propName)
        {
            foreach(var obj in archetypes)
            {
                obj.TryGetValue(propName, out var valueToken);
                if (valueToken != null)
                    return Result.Ok(valueToken);
            }
            return Result.Error(new Error.FieldNotFound(leafName, propName));
        }

        private static IError? LoadObjectFrom<T>(JObject rootNode, string id, JToken token, Dictionary<string, T> db) where T : new()
        {
            var obj = token as JObject;
            if (obj == null)
                return new Error.NodeNotJsonObject(id);

            var hierarchy = new List<JObject>(4);
            var err = GetArchetypeHierarchy(rootNode, id, obj, hierarchy);
            if (err != null)
                return err;

            var newObj = db[id] = new T();
            foreach (var field in typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                var valueTokenResult = FindInArchetypes(id, hierarchy, field.Name);

                if (!valueTokenResult.IsSuccess)
                    return valueTokenResult.Err;

                var valueObj = valueTokenResult.Value.ToObject(field.FieldType);
                field.SetValue(newObj, valueObj);
            }
            return null;
        }

        public static Result<Dictionary<string, T>> Read<T>(string str) where T : new()
        {
            // first strip out any comments
            str = Regex.Replace(str, @"^\s*#.*$", "", RegexOptions.Multiline);

            try
            {
                var root = JObject.Parse(str);

                var db = new Dictionary<string, T>(root.Children().Count());

                foreach (var (id, tok) in root)
                {
                    // object with id starting with "__" are only for being
                    // archetypes of other objects, so we aren't guaranteed to
                    // be able to load them (nor should we)
                    if (id.StartsWith("__"))
                        continue;

                    var err = LoadObjectFrom<T>(root, id, tok!, db);
                    if (err != null)
                        return Result.Error(err);
                }
                return Result.Ok(db);
            }
            catch (JsonException ex)
            {
                return Result.Error(new Error.JsonParseFailed(ex));
            }
        }
    }
}