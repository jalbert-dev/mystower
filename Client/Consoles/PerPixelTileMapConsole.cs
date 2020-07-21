using SadConsole;
using SadRogue.Primitives;

namespace Client
{
    public static partial class Consoles
    {
        public class PerPixelTileMap : Console
        {
            private readonly TileMapRenderer renderer;
            // since SadConsole doesn't normally support per-pixel scrolling, the
            // size in pixels of the current viewport is only stored in tiles.
            // we need this, so we'll store it ourselves
            private int canvasWidthPx, canvasHeightPx;

            private readonly ScreenObject transformRoot;
            public IScreenObject TransformRoot => transformRoot;

            public PerPixelTileMap(SadConsole.ScreenObject parent) : base(3, 3)
            {
                Renderer = renderer = new TileMapRenderer();

                Parent = parent;

                transformRoot = new ScreenObject
                {
                    Parent = this
                };
            }

            public void SetMapSize(int tileWidth, int tileHeight) 
            {
                Resize(ViewWidth, ViewHeight, tileWidth, tileHeight, false);
            }
            public void ResizeViewportPx(int w, int h)
            {
                // https://stackoverflow.com/a/53520604 -- handy ceil implementation for ints!
                var neededWidth = (w / FontSize.X) + (w % FontSize.X == 0 ? 0 : 1);
                var neededHeight = (h / FontSize.Y) + (h % FontSize.Y == 0 ? 0 : 1);
                
                Resize(neededWidth, neededHeight, BufferWidth, BufferHeight, false);

                // cache the pixel size of the visible area for pixel-precise centering
                (canvasWidthPx, canvasHeightPx) = (w, h);
            }

            public void CenterViewOn(int pxX, int pxY)
            {
                var vp = Surface.View;
                // ! .ToPixels(Point) is currently broken
                var viewPortPx = vp.ToPixels(FontSize.X, FontSize.Y);

                // before anything, we need to clamp the viewport size to our canvas size
                // this is a hack piled on top of other hacks, but it prevents the topleft from
                // snapping to the nearest tile
                if (viewPortPx.Width > canvasWidthPx)
                    viewPortPx = viewPortPx.WithWidth(canvasWidthPx);
                if (viewPortPx.Height > canvasHeightPx)
                    viewPortPx = viewPortPx.WithHeight(canvasHeightPx);
                
                var halfSize = new Point(FontSize.X / 2, FontSize.Y / 2);
                var centered = viewPortPx.WithCenter(new Point(pxX, pxY) + halfSize);

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

                renderer.ViewportPixelOffset = new Point(centered.X % FontSize.X, centered.Y % FontSize.Y);
                renderer.ToEdgeX = BufferWidth - 1 - Surface.View.MaxExtentX;
                renderer.ToEdgeY = BufferHeight - 1 - Surface.View.MaxExtentY;
            }

            public override void Draw(System.TimeSpan delta)
            {
                // Adjust position of child elements to account for viewport location
                foreach (var c in Children)
                    c.Position = (ViewPosition.SurfaceLocationToPixel(FontSize) + renderer.ViewportPixelOffset) * -1;
                base.Draw(delta);
            }
        }
    }
}