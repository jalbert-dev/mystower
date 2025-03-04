using System.Collections;
using Microsoft.Xna.Framework;
using Server.Data;

namespace Client
{
    public static partial class Motions
    {
        /// <summary>
        /// Creates a motion expressing lerped movement from one tile to another.
        /// </summary>
        /// <param name="src">Source tile coordinates.</param>
        /// <param name="dst">Destination tile coordinates.</param>
        /// <param name="duration">Duration of movement (in frames).</param>
        public static IEnumerable LerpMove(SadRogue.Primitives.Point tileSize,
                                           MapActor actor,
                                           Vec2i src,
                                           Vec2i dst,
                                           int duration)
        {
            var Source = new Vector2(src.x * tileSize.X, src.y * tileSize.Y);
            var Dest = Source + new Vector2(dst.x * tileSize.X, dst.y * tileSize.Y);

            for (int t = 1; t < duration; t++)
            {
                var floatOffset = Vector2.LerpPrecise(Source, Dest, (float)t / (float)duration);
                actor.Position = floatOffset.ToPoint().ToPoint();
                yield return null;
            }

            actor.Position = Dest.ToPoint().ToPoint();
        }
    }
}