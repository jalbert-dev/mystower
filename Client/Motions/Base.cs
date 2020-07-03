using System.Collections;
using SadRogue.Primitives;

namespace Client
{
    public static partial class Motions
    {
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
    }
}