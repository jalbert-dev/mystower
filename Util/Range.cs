using System;
using Newtonsoft.Json;

namespace Util
{
    public class IntRangeConverter : Newtonsoft.Json.JsonConverter<IntRange>
    {
        public override IntRange ReadJson(JsonReader reader, Type objectType, IntRange existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            try
            {
                var data = serializer.Deserialize<int[]>(reader);
                if (data == null)
                    throw new JsonException("Deserialization of int[] returned null.");
                if (data.Length != 2)
                    throw new JsonException("IntRange requires a JSON array of two integers.");

                return new IntRange(data[0], data[1]);
            }
            catch (JsonException ex)
            {
                throw new JsonException("Error deserializing IntRange.", ex);
            }
            catch (ArgumentException ex)
            {
                throw new JsonException("Error deserializing IntRange.", ex);
            }
        }

        public override void WriteJson(JsonWriter writer, IntRange value, JsonSerializer serializer)
        {
            writer.WriteStartArray();

            writer.WriteValue(value.min);
            writer.WriteValue(value.max);

            writer.WriteEndArray();
        }
    }

    [JsonConverter(typeof(IntRangeConverter))]
    public struct IntRange : IEquatable<IntRange>
    {
        public readonly int min, max;

        private void ThrowIfInvalid()
        {
            if (max < min)
                throw new ArgumentException($"Attempted to create invalid range {ToString()} (max < min)");
        }

        public IntRange(int min, int max)
        {
            (this.min, this.max) = (min, max);
            ThrowIfInvalid();
        }

        public override bool Equals(object? obj) => obj is IntRange v && Equals(v);
        public bool Equals(IntRange other) => min == other.min && max == other.max;
        public override int GetHashCode() => (min, max).GetHashCode();
        public static bool operator==(IntRange a, IntRange b) => a.Equals(b);
        public static bool operator!=(IntRange a, IntRange b) => !(a == b);

        public override string ToString() => $"[{min}, {max}]";

        public void Deconstruct(out int min, out int max)
            => (min, max) = (this.min, this.max);
        public static implicit operator IntRange((int, int) minMax)
            => new IntRange(minMax.Item1, minMax.Item2);
    }
}