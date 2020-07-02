using System;
using SadConsole;
using SadRogue.Primitives;

namespace Client
{
    public class DebugStatsConsole : SadConsole.Console
    {
        public long LastSimulationTime { get; set; }
        public long UpdateDelta { get; set; }
        public long RenderDelta { get; set; }
        public long Frame { get; set; }

        public DebugStatsConsole() : base(40, 4)
        {
            DefaultBackground = Color.Transparent;
            IsVisible = false;

            Cursor.UseLinuxLineEndings = true;
            Cursor.PrintAppearance = new SadConsole.ColoredGlyph 
            { 
                Foreground=Color.White, 
                Background=Color.Transparent 
            };
        }

        public override void Draw(TimeSpan delta)
        {
            Frame = (Frame+1) % 1000;

            this.Clear();
            Cursor.Position = new Point(0, 0);
            Cursor.Print($"Last simulation time: {LastSimulationTime}ms\n");
            Cursor.Print($"   Last update delta: {UpdateDelta}ms\n");
            Cursor.Print($"   Last render delta: {RenderDelta}ms\n");
            Cursor.Print($"               Frame: {Frame}");
            base.Draw(delta);
        }
    }
}