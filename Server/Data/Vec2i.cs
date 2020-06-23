
using System;
using Server.Util;

namespace Server.Data
{
    public struct Vec2i : IEquatable<Vec2i>
    {
        public int x;
        public int y;

        public bool Equals(Vec2i other) => x == other.x && y == other.y;

        public static Vec2i Zero = new Vec2i { x = 0, y = 0 };

        public override string ToString() => this.ToJsonString();
    }
}