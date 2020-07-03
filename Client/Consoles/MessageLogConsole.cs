using System;
using System.Collections.Generic;
using System.Linq;
using SadConsole;
using SadRogue.Primitives;

namespace Client.Consoles
{
    public class MessageLog : SadConsole.Console
    {
        private List<string> messages = new List<string>(256);
        private bool dirty = true;
        private SadConsole.Console msgDisplay;

        // notice that this is oldest message first!
        private IEnumerable<string> RecentMessages
            => AllMessages.Take(Height).Reverse();

        public IEnumerable<string> AllMessages
            => messages.Reverse<string>();

        public MessageLog() : base(1, 1) 
        { 
            DefaultBackground = Color.Gray;
            UsePixelPositioning = true;

            msgDisplay = new SadConsole.Console(1, 1);
            msgDisplay.Position = new Point(2, 1);
            msgDisplay.DefaultBackground = Color.Transparent;

            msgDisplay.Cursor.PrintAppearance = new SadConsole.ColoredGlyph 
            { 
                Foreground=Color.Black, 
                Background=Color.Transparent 
            };

            Children.Add(msgDisplay);
        }

        public void Reposition(int x, int y, int w, int h)
        {
            Resize(w, h, w, h, false);

            // By default, the origin of the message log is at the center-bottom
            Position = new Point(x - w * FontSize.X / 2, y - h * FontSize.Y);

            msgDisplay.Resize(w - 4, h - 2, w - 4, h - 2, false);

            dirty = true;
        }

        public override void Update(TimeSpan timeElapsed)
        {
            base.Update(timeElapsed);
        }

        public override void Draw(TimeSpan timeElapsed)
        {
            if (dirty)
            {
                dirty = false;

                this.Clear();
                var boxCell = new SadConsole.ColoredGlyph { Glyph=219, Foreground=Color.WhiteSmoke };
                this.DrawBox(new Rectangle(0, 0, Width, Height), boxCell);
                this.DrawLine(new Point(1, 0), new Point(Width-2, 0), boxCell.Foreground, DefaultBackground, 223);
                this.DrawLine(new Point(1, Height-1), new Point(Width-2, Height-1), boxCell.Foreground, DefaultBackground, 220);

                msgDisplay.Clear();
                msgDisplay.Cursor.AutomaticallyShiftRowsUp = true;
                msgDisplay.Cursor.UseLinuxLineEndings = true;

                msgDisplay.Cursor.Move(0, 0);
                msgDisplay.Cursor.Print(string.Join("\n", RecentMessages));
            }

            base.Draw(timeElapsed);
        }

        public void AddMessage(string msg) 
        {
            dirty = true;
            messages.Add(msg);
            this.IsVisible = true;
        }

        public void ToggleVisible()
        {
            this.IsVisible = !this.IsVisible;
        }

        public void ClearMessages()
        {
            dirty = false;
            messages.Clear();
        }
    }
}