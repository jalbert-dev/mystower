using Server.Data;

namespace Client
{
    public static class Vec2iExtensions
    {
        public static SadRogue.Primitives.Point ToPoint(this Vec2i self)
            => new SadRogue.Primitives.Point(self.x, self.y);
    }
}