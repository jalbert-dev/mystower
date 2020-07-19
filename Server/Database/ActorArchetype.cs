using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Server.Database
{
    public struct StatBlock
    {
        [JsonProperty(Required=Required.Always)]
        public int hp;
        [JsonProperty(Required=Required.Always)]
        public int atk;
        [JsonProperty(Required=Required.Always)]
        public int def;
    }

    [CodeGen.DatabaseType, JsonConverter(typeof(DatabaseTypeConverter<ActorArchetype>))]
    public partial class ActorArchetype
    {
        private StatBlock lvlMinStatus = default(StatBlock);
        private StatBlock lvlMaxStatus = default(StatBlock);
        private string defaultAiType = "";

        private string nameId = "";
        private string appearanceId = "";

        private const int MIN_LEVEL = 1;
        private const int MAX_LEVEL = 99;
        public static int ClampLevel(int lvl) => Math.Min(MAX_LEVEL, Math.Max(MIN_LEVEL, lvl));

        public StatBlock StatusAtLevel(int level)
            => new StatBlock
            {
                hp = Lerp(lvlMinStatus.hp, lvlMaxStatus.hp, level),
                atk = Lerp(lvlMinStatus.atk, lvlMaxStatus.atk, level),
                def = Lerp(lvlMinStatus.def, lvlMaxStatus.def, level),
            };

        private int Lerp(int a, int b, int lvl)
        {
            lvl = ClampLevel(lvl);
            float t = (float)(lvl - 1) / (float)(MAX_LEVEL - 1);
            return (int)((1 - t) * a + t * b);
        }
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
}