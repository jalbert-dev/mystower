using System;
using SadConsole;
using SadRogue.Primitives;

namespace Client
{
    public static partial class Consoles
    {
        public class MessageLog
        {
            private readonly ScreenObject root;
            private readonly SadConsole.Console logFrame;
            private readonly SadConsole.Console msgDisplay;

            public float PercentOfScreenHeight { get; set; } = 1.0f / 5.0f;
            public float PercentOfScreenWidth { get; set; } = 3.0f / 5.0f;
            public int OffsetFromBottomPx { get; set; } = 40;

            public MessageLog(IScreenObject parent, ILogMessageSource msgSource)
            { 
                root = new ScreenObject();

                logFrame = new SadConsole.Console(1, 1)
                {
                    DefaultBackground = Color.Gray,
                    UsePixelPositioning = true
                };

                msgDisplay = new SadConsole.Console(1, 1)
                {
                    Position = new Point(2, 1),
                    DefaultBackground = Color.Transparent,
                    Font = logFrame.Font
                };

                msgDisplay.Cursor.PrintAppearance = new ColoredGlyph 
                { 
                    Foreground=Color.Black, 
                    Background=Color.Transparent 
                };

                root.Parent = parent;
                logFrame.Parent = root;
                msgDisplay.Parent = logFrame;

                msgSource.OnNewMessage += OnNewMessage;
            }

            public void Reposition(int screenWidthPx, int screenHeightPx, ILogMessageSource msgSource)
            {
                int x = screenWidthPx / 2;
                int y = screenHeightPx - OffsetFromBottomPx;
                int h = (int)(screenHeightPx * PercentOfScreenHeight);
                int w = (int)(screenWidthPx * PercentOfScreenWidth);

                int widthInTiles = w / msgDisplay.FontSize.X;
                int heightInTiles = h / msgDisplay.FontSize.Y;

                logFrame.Resize(widthInTiles, heightInTiles, widthInTiles, heightInTiles, false);

                // By default, the origin of the message log is at the center-bottom
                root.Position = new Point(x - w / 2, y - h);

                msgDisplay.Resize(widthInTiles - 4, heightInTiles - 2, widthInTiles - 4, heightInTiles - 2, false);

                RedrawMessages(msgSource);
            }

            private void OnNewMessage(ILogMessageSource msgSource)
            {
                RedrawMessages(msgSource);
                root.IsVisible = true;
            }

            public void RedrawMessages(ILogMessageSource msgSource)
            {
                var msgs = msgSource.GetRecentMessages(msgDisplay.Height);

                logFrame.Clear();
                var boxCell = new ColoredGlyph { Glyph=219, Foreground=Color.WhiteSmoke };
                logFrame.DrawBox(new Rectangle(0, 0, logFrame.Width, logFrame.Height), boxCell);
                logFrame.DrawLine(new Point(1, 0), new Point(logFrame.Width-2, 0), boxCell.Foreground, logFrame.DefaultBackground, 223);
                logFrame.DrawLine(new Point(1, logFrame.Height-1), new Point(logFrame.Width-2, logFrame.Height-1), boxCell.Foreground, logFrame.DefaultBackground, 220);

                msgDisplay.Clear();
                msgDisplay.Cursor.AutomaticallyShiftRowsUp = true;
                msgDisplay.Cursor.UseLinuxLineEndings = true;

                msgDisplay.Cursor.Move(0, 0);
                msgDisplay.Cursor.Print(string.Join("\n", msgs));
            }

            public void ToggleVisible()
            {
                root.IsVisible = !root.IsVisible;
            }
        }
    }
}