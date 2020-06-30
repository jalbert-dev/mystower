using System;
using Microsoft.Xna.Framework;

namespace Client.Motions
{
    public class LerpMove : Base
    {
        public Vector2 Source { get; set; }
        public Vector2 Dest { get; set; }
        public int Interval { get; }

        private int frameTime = 0;

        /// <summary>
        /// Creates a motion expressing lerped movement from one tile to another.
        /// </summary>
        /// <param name="sx">Source tile X.</param>
        /// <param name="sy">Source tile Y.</param>
        /// <param name="dx">Dest tile X.</param>
        /// <param name="dy">Dest tile Y.</param>
        /// <param name="t">Duration of movement (in frames).</param>
        /// <param name="actor">The actor to move.</param>
        /// <returns></returns>
        public LerpMove(int sx, int sy, int dx, int dy, int t, MapActor actor) : base(actor) 
        {
            var tileScale = actor.ScrollingParent.FontSize;
            Source = new Vector2(sx * tileScale.X, sy * tileScale.Y);
            Dest = Source + new Vector2(dx * tileScale.X, dy * tileScale.Y);
            Interval = t;
        }

        public override bool IsGlobalSequential => false;
        public override bool IsActorSequential => true;
        public override bool IsFinished => frameTime >= Interval;

        public override void Apply(TimeSpan timeElapsed)
        {
            frameTime++;
            var floatOffset = Vector2.LerpPrecise(Source, Dest, (float)frameTime / (float)Interval);
            MapActor.Position = floatOffset.ToPoint().ToPoint();
        }
    }
}