using System.Collections.Generic;
using Util.Functional;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Linq;
using System.Reflection;
using System;

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
        {
            if (!databases.TryGetValue(typeof(T), out var dbObj))
                return Result.Error(new Error.NoDatabaseForType(typeof(T).Name));
            
            var db = (Dictionary<string, T>)dbObj;
            if (!db.TryGetValue(key, out var resultObj))
                return Result.Error(new Error.KeyNotFoundInDatabase(typeof(T).Name, key));

            return Result.Ok((T)resultObj);
        }
    }

    public static class ArchetypeJson
    {
        private static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> self, out TKey key, out TValue value)
            => (key, value) = (self.Key, self.Value);

        private static Result<(string?, JObject?)> GetArchetypeNode(JObject rootNode, string nodeId, JObject node)
        {
            // find _archetype property and if exists find archetype object by that key 
            if (node.TryGetValue("_archetype", out var archetypeToken))
            {
                // note that we can't just load the archetype object into the database
                // recursively because archetypes are not required to have all
                // fields populated.

                var archetypeId = archetypeToken.ToObject<string>()!;

                // get the JSON node corresponding to the archetype object
                if (!rootNode.TryGetValue(archetypeId, out var archetypeNodeToken))
                    return Result.Error(new Error.ArchetypeNotFound(nodeId, archetypeId));
                var result = archetypeNodeToken as JObject;
                if (result == null)
                    return Result.Error(new Error.NodeNotJsonObject(archetypeId));
                
                return Result.Ok<(string?, JObject?)>( (archetypeId, result) );
            }
            return Result.Ok<(string?, JObject?)>( (null, null) );
        }

        private static Result<JToken> FindInArchetypes(JObject rootNode, string id, JObject node, string propName)
        {
            node.TryGetValue(propName, out var valueToken);
            if (valueToken == null)
            {
                var archNodeResult = GetArchetypeNode(rootNode, id, node);
                if (!archNodeResult.IsSuccess)
                    return Result.Error(archNodeResult.Err);
                var (archId, archNode) = archNodeResult.Value;

                // if no archetype, the property being searched for is not present in the hierarchy
                if (archNode == null)
                    return Result.Error(new Error.FieldNotFound(id, propName));

                // recursively find in archetype node
                return FindInArchetypes(rootNode, archId, archNode, propName);
            }
            return Result.Ok(valueToken);
        }

        private static IError? LoadObjectFrom<T>(JObject rootNode, string id, JToken token, Dictionary<string, T> db) where T : new()
        {
            var obj = token as JObject;
            if (obj == null)
                return new Error.NodeNotJsonObject(id);

            var newObj = db[id] = new T();
            foreach (var field in typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                var valueTokenResult = FindInArchetypes(rootNode, id, obj, field.Name);

                if (!valueTokenResult.IsSuccess)
                    return valueTokenResult.Err;

                var valueObj = valueTokenResult.Value.ToObject(field.FieldType);
                System.Console.WriteLine($"{typeof(T).Name}.{field.Name} <- {valueObj}");
                field.SetValue(newObj, valueObj);
            }
            return null;
        }

        public static Result<Dictionary<string, T>> Read<T>(string str) where T : new()
        {
            try
            {
                var root = JObject.Parse(str);

                var db = new Dictionary<string, T>(root.Children().Count());

                foreach (var (id, tok) in root)
                {
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