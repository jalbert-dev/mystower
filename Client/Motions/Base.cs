using System;
using System.Collections;
using Microsoft.Xna.Framework;
using SadRogue.Primitives;
using Server.Data;
using Point = SadRogue.Primitives.Point;

namespace Client
{
    public static partial class Motions
    {
        public static IEnumerable SetFacing(MapActor actor, Server.Data.Direction facing)
        {
            actor.Facing = facing;
            yield break;
        }

        public static IEnumerable Wiggle(MapActor actor, int duration, int speed, int amplitude)
        {
            Point offset = default;
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
                                        Action atBegin,
                                        Action atMiddle)
        {
            atBegin();

            var extent = tileSize * depth * actor.Facing.ToVec().ToPoint();
            var extentf = new Vector2(extent.X, extent.Y);

            Point offset;
            for (int i = 1; i <= t1; i++)
            {
                offset = Vector2.LerpPrecise(Vector2.Zero, extentf, (float)i / t1).ToPoint().ToPoint();
                actor.PositionOffset += offset;
                yield return null;
            }

            atMiddle();

            for (int i = 1; i <= t2; i++)
            {
                offset = Vector2.LerpPrecise(extentf, Vector2.Zero, (float)i / t2).ToPoint().ToPoint();
                actor.PositionOffset += offset;
                yield return null;
            }
        }

        public static IEnumerable Death(GameMessageLog msg, MapActor actor, Action whenDone)
        {
            msg.AddMessage($"{actor.DisplayName} is defeated!");
            for (int i = 0; i < 10; i++)
                yield return null;
            whenDone();
        }
    }
}