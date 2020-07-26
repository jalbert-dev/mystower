using System;
using SadConsole;
using SadRogue.Primitives;

namespace Client
{
    public static partial class Consoles
    {
        public class DebugStats
        {
            private class Cons : SadConsole.Console
            {
                private readonly DebugStats Data;
                private long frame;

                public Cons(DebugStats data) : base(40, 4)
                {
                    Data = data;

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
                    frame = (frame+1) % 1000;

                    this.Clear();
                    Cursor.Position = new Point(0, 0);
                    Cursor.Print($"Last simulation time: {Data.LastSimulationTime}ms\n");
                    Cursor.Print($"   Last update delta: {Data.UpdateDelta}ms\n");
                    Cursor.Print($"   Last render delta: {Data.RenderDelta}ms\n");
                    Cursor.Print($"               Frame: {frame}");
                    base.Draw(delta);
                }
            }

            public long LastSimulationTime { get; set; }
            public long UpdateDelta { get; set; }
            public long RenderDelta { get; set; }

            public bool IsVisible
            {
                get => display.IsVisible;
                set => display.IsVisible = value;
            }

            private readonly Cons display;

            public DebugStats(IScreenObject parent)
            {
                display = new Cons(this)
                {
                    Parent = parent
                };
            }
        }
    }
}