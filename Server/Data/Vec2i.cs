
using System;
using Util;

namespace Server.Data
{
    public struct Vec2i : IEquatable<Vec2i>
    {
        public Vec2i(int x, int y) => (this.x, this.y) = (x, y);

        public int x;
        public int y;

        public override bool Equals(object obj) => obj is Vec2i v && Equals(v);
        public bool Equals(Vec2i other) => x == other.x && y == other.y;
        public override int GetHashCode() => (x, y).GetHashCode();
        public static bool operator==(Vec2i a, Vec2i b) => a.Equals(b);
        public static bool operator!=(Vec2i a, Vec2i b) => !(a == b);

        public static Vec2i Zero = new Vec2i { x = 0, y = 0 };

        public override string ToString() => this.ToJsonString();

        public void Deconstruct(out int ox, out int oy) 
            => (ox, oy) = (x, y);
        public static implicit operator Vec2i((int, int) xy)
            => new Vec2i(xy.Item1, xy.Item2);
    }
}