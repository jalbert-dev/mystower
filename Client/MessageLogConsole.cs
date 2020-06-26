using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace Client
{
    public class MessageLogConsole : SadConsole.ScrollingConsole
    {
        private List<string> messages = new List<string>();
        private bool dirty = true;
        private SadConsole.ScrollingConsole msgDisplay;

        // notice that this is oldest message first!
        private IEnumerable<string> RecentMessages
            => MessageLog.Take(Height).Reverse();

        public IEnumerable<string> MessageLog
            => messages.Reverse<string>();

        public MessageLogConsole(int w, int h) : base(w, h) 
        { 
            msgDisplay = new SadConsole.ScrollingConsole(w - 4, h - 2);
            msgDisplay.Position = new Point(2, 1);
            msgDisplay.DefaultBackground = Color.Transparent;
            msgDisplay.Clear();

            Children.Add(msgDisplay);
        }

        public override void Draw(TimeSpan timeElapsed)
        {
            if (dirty)
            {
                dirty = false;

                Clear();
                var boxCell = new SadConsole.Cell { Glyph=219, Foreground=Color.WhiteSmoke };
                DrawBox(new Rectangle(0, 0, Width, Height), boxCell);
                DrawLine(new Point(1, 0), new Point(Width-2, 0), boxCell.Foreground, DefaultBackground, 223);
                DrawLine(new Point(1, Height-1), new Point(Width-2, Height-1), boxCell.Foreground, DefaultBackground, 220);

                msgDisplay.Clear();
                msgDisplay.Cursor.AutomaticallyShiftRowsUp = true;
                msgDisplay.Cursor.UseLinuxLineEndings = true;

                msgDisplay.Cursor.PrintAppearance =
                    new SadConsole.Cell { Foreground=Color.Black, Background=Color.Transparent };

                msgDisplay.Cursor.Move(0, 0);
                msgDisplay.Cursor.Print(string.Join("\n", RecentMessages));
            }

            base.Draw(timeElapsed);
        }

        public void AddMessage(string msg) 
        {
            dirty = true;
            messages.Add(msg);
        }

        public void ClearMessages()
        {
            dirty = false;
            messages.Clear();
        }
    }
}