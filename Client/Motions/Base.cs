using System;
using System.Collections;
using Microsoft.Xna.Framework;
using SadRogue.Primitives;
using Point = SadRogue.Primitives.Point;

namespace Client
{
    public static partial class Motions
    {
        public static IEnumerable SetFacing(MapActor actor, Server.Data.Vec2i facing)
        {
            actor.Facing = facing;
            yield break;
        }

        public static IEnumerable Wiggle(MapActor actor, int duration, int speed, int amplitude)
        {
            Point offset = default(Point);
            bool dir = false;

            for (int t = 0; t <= duration; t++)
            {
                offset = offset.WithX(offset.X + (dir ? speed : -speed));
                if (offset.X <= -amplitude)
                    dir = true;
                else if (offset.X >= amplitude)
                    dir = false;

                actor.PositionOffset += offset;
                yield return null;
            }
        }

        public static IEnumerable Lunge(SadRogue.Primitives.Point tileSize,
                                        MapActor actor,
                                        int t1,
                                        int t2,
                                        float depth,
                                        Action middle)
        {
            var offset = default(Point);
            var extent = tileSize * depth * new Point(actor.Facing.x, actor.Facing.y);
            var extentf = new Vector2(extent.X, extent.Y);
            
            for (int i = 1; i <= t1; i++)
            {
                offset = Vector2.LerpPrecise(Vector2.Zero, extentf, (float)i / t1).ToPoint().ToPoint();
                actor.PositionOffset += offset;
                yield return null;
            }

            middle();

            for (int i = 1; i <= t2; i++)
            {
                offset = Vector2.LerpPrecise(extentf, Vector2.Zero, (float)i / t2).ToPoint().ToPoint();
                actor.PositionOffset += offset;
                yield return null;
            }
        }

        public static IEnumerable Death(Consoles.MessageLog msg, MapActor actor, Action whenDone)
        {
            msg.AddMessage("Actor is defeated!");
            for (int i = 0; i < 10; i++)
                yield return null;
            whenDone();
        }
    }
}