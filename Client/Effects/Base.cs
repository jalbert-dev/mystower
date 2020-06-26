using System;
using Microsoft.Xna.Framework;

namespace Client.Effects
{
    public abstract class Base : IChoreography
    {
        public MapActor MapActor { get; private set; }
        public virtual bool IsDone { get; private set; } = false;
        public abstract bool IsSolo { get; }
        public abstract void Apply(TimeSpan timeElapsed);

        public Base(MapActor actor) => MapActor = actor;
    }

    public class Wiggle : Base
    {
        int t = 0;

        Point offset = new Point(0, 0);
        bool dir = false;

        public Wiggle(MapActor actor, bool blocking) : base(actor) { blocks = blocking; }

        bool blocks;
        public override bool IsSolo => blocks;
        public override bool IsDone => t > 60;

        public override void Apply(TimeSpan timeElapsed)
        {
            offset.X += dir ? 1 : -1;
            if (offset.X <= -8)
                dir = true;
            else if (offset.X >= 8)
                dir = false;

            MapActor.Position += offset;

            t++;
        }
    }
}