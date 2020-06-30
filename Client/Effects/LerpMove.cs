using System;
using Microsoft.Xna.Framework;

namespace Client.Effects
{
    public class LerpMove : Base
    {
        public Vector2 Source { get; set; }
        public Vector2 Dest { get; set; }
        public int Interval { get; }

        private int frameTime = 0;

        public LerpMove(int sx, int sy, int dx, int dy, int t, MapActor actor) : base(actor) 
        {
            var tileScale = actor.ScrollingParent.FontSize;
            Source = new Vector2(sx * tileScale.X, sy * tileScale.Y);
            Dest = Source + new Vector2(dx * tileScale.X, dy * tileScale.Y);
            Interval = t;
        }

        public override bool IsGlobalSolo => false;
        public override bool IsLocalSolo => true;
        public override bool IsDone => frameTime >= Interval;

        public override void Apply(TimeSpan timeElapsed)
        {
            frameTime++;
            var floatOffset = Vector2.LerpPrecise(Source, Dest, (float)frameTime / (float)Interval);
            MapActor.Position = floatOffset.ToPoint().ToPoint();
        }
    }
}