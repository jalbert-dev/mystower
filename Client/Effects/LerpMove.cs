using System;
using Microsoft.Xna.Framework;

namespace Client.Effects
{
    public class LerpMove : Base
    {
        public Vector2 Source { get; }
        public int Interval { get; }

        private int frameTime = 0;

        public LerpMove(int dx, int dy, int t, MapActor actor) : base(actor) 
        {
            Source = new Vector2(
                -dx * actor.Parent.Font.Size.X,
                -dy * actor.Parent.Font.Size.Y);
            Interval = t;
        }

        public override bool IsSolo => false;
        public override bool IsDone => frameTime >= Interval;
        public override void Apply(TimeSpan timeElapsed)
        {
            frameTime++;
            var floatOffset = Vector2.LerpPrecise(Source, Vector2.Zero, (float)frameTime / (float)Interval);
            MapActor.Position += floatOffset.ToPoint();
        }
    }
}