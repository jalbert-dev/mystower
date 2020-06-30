using System;
using SadRogue.Primitives;

namespace Client.Motions
{
    public abstract class Base : IActorMotion
    {
        public MapActor MapActor { get; private set; }
        public virtual bool IsFinished { get; private set; } = false;
        public abstract bool IsGlobalSequential { get; }
        public abstract bool IsActorSequential { get; }
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
        public override bool IsGlobalSequential => blocks;
        public override bool IsActorSequential => blocks;
        public override bool IsFinished => t > duration;

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