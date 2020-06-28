using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Server.Data;
using static SadConsole.Font;
using static SadConsole.RectangleExtensions;

namespace Client
{
    public class TileMapConsole : SadConsole.ScrollingConsole
    {
        private Point ViewportPixelOffset;

        public TileMapConsole(int w, int h) : base(w / 2, h, SadConsole.Global.Fonts["Tileset"].GetFont(FontSizes.Four))
        {
            DefaultBackground = Color.Black;
            //Font = Font.Master.GetFont(FontSizes.Four);
        }

        public void RebuildTileMap(MapData map)
        {
            int w = map.Width;
            int h = map.Height;

            Resize(w, h, false);

            Clear();

            // VERY temporary...
            var r = new Random();
            var Grass = new List<int> { 5, 6, 7, 1 };
            var Tree = new List<int> { 48, 49, 50, 51 };

            for (int i = 0; i < w; i++)
                for (int j = 0; j < h; j++)
                    SetGlyph(i, j,
                        map.tiles[i,j] == 0 ? Grass[r.Next(4)] : Tree[r.Next(4)],
                        Color.DarkGreen,
                        Color.Black);
        }

        public void CenterViewOn(MapActor actor)
        {
            var vp = ViewPort;
            var viewPortPx = new Rectangle
            {
                X = vp.X * Font.Size.X,
                Y = vp.Y * Font.Size.Y,
                Width = vp.Width * Font.Size.X,
                Height = vp.Height * Font.Size.Y,
            };
            var halfSize = new Point { X=Font.Size.X / 2, Y=Font.Size.Y / 2 };
            var centered = viewPortPx.CenterOnPoint(
                actor.Position + halfSize, 
                Width * Font.Size.X, 
                Height * Font.Size.Y);

            vp.X = centered.X / Font.Size.X;
            vp.Y = centered.Y / Font.Size.Y;
            ViewPort = vp;

            ViewportPixelOffset.X = centered.X % Font.Size.X;
            ViewportPixelOffset.Y = centered.Y % Font.Size.Y;
        }

        public override void SetRenderCells()
        {
            // TODO: Render cell range needs to extend 1 row and 1 column past
            //       the tile bounds of the screen so we can scroll smoothly
            // TODO: Implement a cell wrap-around option that will take
            //       cells outside of world bounds from the other side of the world

            base.SetRenderCells();

            RenderRects = RenderRects.Select(x => {
                x.Location -= ViewportPixelOffset;
                return x;
            }).ToArray();
        }

        public override void Draw(System.TimeSpan timeElapsed)
        {
            // TODO: this is pretty unexpected and has a hidden requirement that
            //       child elements must have their positions reset every tick.
            //       dig deeper into SadConsole and figure out where to best put
            //       adjustment of child position for viewport
            //       Or just cache old positions and reset after base.Draw...

            // Adjust position of child elements to account for viewport location
            foreach (var c in Children)
                if (c.UsePixelPositioning)
                    c.Position -= ViewPort.Location.ConsoleLocationToPixel(Font) + ViewportPixelOffset;
            base.Draw(timeElapsed);
        }
    }
}