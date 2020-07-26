using System;
using SadConsole;
using SadRogue.Primitives;

namespace Client
{
    public static partial class Consoles
    {
        public class PerPixelDisplay : SadConsole.Console
        {
            private readonly TileMapRenderer renderer;

            private readonly ScreenObject transformRoot;
            public IScreenObject TransformRoot => transformRoot;

            public PerPixelDisplay(SadConsole.ScreenObject parent) : base(3, 3)
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

            private void ResizeViewportPx(int w, int h)
            {
                // first calculate the smallest number of tiles in X and Y directions we'll need
                // to cover an area (w, h) pixels in size
                // https://stackoverflow.com/a/53520604 -- handy ceil implementation for ints!
                var neededWidth = (w / FontSize.X) + (w % FontSize.X == 0 ? 0 : 1);
                var neededHeight = (h / FontSize.Y) + (h % FontSize.Y == 0 ? 0 : 1);
                
                Resize(neededWidth, neededHeight, BufferWidth, BufferHeight, false);
            }

            public void SyncViewSizeWithCamera(ICamera cam)
            {
                ResizeViewportPx(cam.ViewWidth, cam.ViewHeight);
                SyncViewPosWithCamera(cam);
            }

            public void SyncViewPosWithCamera(ICamera cam)
            {
                var centered = cam.View;

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
                
                Surface.View = Surface.View
                    .WithX(centered.X / FontSize.X)
                    .WithY(centered.Y / FontSize.Y);

                renderer.ViewportPixelOffset = new Point(centered.X % FontSize.X, centered.Y % FontSize.Y);
                renderer.ToEdgeX = BufferWidth - 1 - Surface.View.MaxExtentX;
                renderer.ToEdgeY = BufferHeight - 1 - Surface.View.MaxExtentY;
            }

            public override void Render(System.TimeSpan delta)
            {
                // Adjust position of child elements to account for viewport location
                foreach (var c in Children)
                    c.Position = (ViewPosition.SurfaceLocationToPixel(FontSize) + renderer.ViewportPixelOffset) * -1;
                base.Render(delta);
            }
        }
    }
}