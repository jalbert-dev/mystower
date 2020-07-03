using System;
using System.Collections.Generic;
using SadConsole;
using SadRogue.Primitives;
using Server.Data;

using static SadConsole.Font;
using static SadConsole.RectangleExtensions;

namespace Client.Consoles
{
    public class TileMap : SadConsole.Console
    {
        private TileMapRenderer _tilemapRenderer;

        public TileMap(int w, int h) : base(w / 2, h)
        {
            DefaultBackground = Color.Black;
            Font = SadConsole.GameHost.Instance.Fonts["Tileset"];
            FontSize = Font.GetFontSize(Sizes.Four);
            Renderer = _tilemapRenderer = new TileMapRenderer();
        }

        public void ResizePx(int w, int h)
        {
            // https://stackoverflow.com/a/53520604 -- handy ceil implementation for ints!
            var neededWidth = (w / FontSize.X) + (w % FontSize.X == 0 ? 0 : 1);
            var neededHeight = (h / FontSize.Y) + (h % FontSize.Y == 0 ? 0 : 1);
            Resize(neededWidth, neededHeight, BufferWidth, BufferHeight, false);
        }

        public void RebuildTileMap(MapData map)
        {
            int w = map.Width;
            int h = map.Height;

            this.Resize(ViewWidth, ViewHeight, w, h, false);

            this.Clear();

            // VERY temporary...
            var r = new Random();
            var Grass = new List<int> { 5, 6, 7, 1 };
            var Tree = new List<int> { 48, 49, 50, 51 };

            for (int i = 0; i < w; i++)
                for (int j = 0; j < h; j++)
                    this.SetGlyph(i, j,
                        map.tiles[i,j] == 0 ? Grass[r.Next(4)] : Tree[r.Next(4)],
                        map.tiles[i,j] == 0 ? Color.Lerp(Color.DarkGreen, Color.DarkOliveGreen, (float)r.NextDouble()) : Color.DarkGreen,
                        Color.Lerp(new Color(0, 40, 0), new Color(0, 34, 0), (float)r.NextDouble()));
        }

        public void CenterViewOn(MapActor actor)
        {
            var vp = Surface.View;
            // ! .ToPixels(Point) is currently broken
            var viewPortPx = vp.ToPixels(FontSize.X, FontSize.Y);
            var halfSize = new Point(FontSize.X / 2, FontSize.Y / 2);
            var centered = viewPortPx.WithCenter(actor.Position + halfSize);

            // bounds check the centered viewport
            int nx = centered.X;
            int ny = centered.Y;
            if (centered.MaxExtentX + 1 > BufferWidth * FontSize.X)
                nx -= centered.MaxExtentX + 1 - BufferWidth * FontSize.X;
            else if (nx < 0)
                nx = 0;
            if (centered.MaxExtentY + 1 > BufferHeight * FontSize.Y)
                ny -= centered.MaxExtentY + 1 - BufferHeight * FontSize.Y;
            else if (ny < 0)
                ny = 0;
            
            centered = centered.WithX(nx).WithY(ny);
            
            Surface.View = vp
                .WithX(centered.X / FontSize.X)
                .WithY(centered.Y / FontSize.Y);

            _tilemapRenderer.ViewportPixelOffset = new Point(centered.X % FontSize.X, centered.Y % FontSize.Y);
            _tilemapRenderer.ToEdgeX = BufferWidth - 1 - Surface.View.MaxExtentX;
            _tilemapRenderer.ToEdgeY = BufferHeight - 1 - Surface.View.MaxExtentY;
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
                if (c is SadConsole.Entities.Entity e)
                    e.PositionOffset -= (ViewPosition.SurfaceLocationToPixel(FontSize) + _tilemapRenderer.ViewportPixelOffset);
            base.Draw(timeElapsed);
        }
    }
}