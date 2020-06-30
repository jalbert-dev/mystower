using System;
using SadRogue.Primitives;

namespace Client.Effects
{
    public abstract class Base : IChoreography
    {
        public MapActor MapActor { get; private set; }
        public virtual bool IsDone { get; private set; } = false;
        public abstract bool IsGlobalSolo { get; }
        public abstract bool IsLocalSolo { get; }
        public abstract void Apply(TimeSpan timeElapsed);

        public Base(MapActor actor) => MapActor = actor;
    }

    public class Wiggle : Base
    {
        int t = 0;
        readonly int duration;

        Point offset = new Point(0, 0);
        bool dir = false;

        public Wiggle(MapActor actor, bool blocking, int time = 60) : base(actor) 
        { 
            duration = time;
            blocks = blocking; 
        }

        bool blocks;
        public override bool IsGlobalSolo => blocks;
        public override bool IsLocalSolo => blocks;
        public override bool IsDone => t > duration;

        public override void Apply(TimeSpan timeElapsed)
        {
            offset = offset.WithX(offset.X + (dir ? 1 : -1));
            if (offset.X <= -8)
                dir = true;
            else if (offset.X >= 8)
                dir = false;

            MapActor.PositionOffset += offset;

            t++;
        }
    }
}