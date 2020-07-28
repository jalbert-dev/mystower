using System;
using Newtonsoft.Json;

namespace Server.Random
{
    [JsonObject(MemberSerialization.OptIn)]
    public class LCG64RandomSource : IRandomSource, IEquatable<LCG64RandomSource>, Util.IDeepCloneable<LCG64RandomSource>
    {
        private const UInt64 a = 2862933555777941757;
        private const UInt64 b = 3037000493;

        [JsonProperty]
        private UInt64 x;

        private UInt64 _next() => x = a * x + b;

        public LCG64RandomSource() : this(unchecked((UInt64)DateTime.Now.Ticks)) {}
        public LCG64RandomSource(UInt64 seed) => x = seed;

        public int Next() => (int)_next();
        public int Next(int min, int max)
        {
            var range = (UInt64)(max + 1 - min);
            return (int)((_next() % (range * 4)) / 4 + (UInt64)min);
        }

        public bool Equals(LCG64RandomSource other) => x == other.x;
        public LCG64RandomSource DeepClone() => new LCG64RandomSource(x);
    }
}