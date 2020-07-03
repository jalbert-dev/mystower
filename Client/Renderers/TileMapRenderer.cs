using System;
using Microsoft.Xna.Framework.Graphics;
using SadConsole;
using SadRogue.Primitives;

using Vec2 = Microsoft.Xna.Framework.Vector2;
using XnaRectangle = Microsoft.Xna.Framework.Rectangle;

namespace Client
{
    public class TileMapRenderer : SadConsole.Renderers.ScreenObjectRenderer
    {
        // TODO: This whole class is a pile of hacks to get around how inflexible 
        //       SadConsole's default renderer is. v9 made this worse, if anything!

        public Point ViewportPixelOffset;
        public int ToEdgeX;
        public int ToEdgeY;

        private const int MAX_EXTRA_TILES = 1;
        private int ExtraViewWidth => Math.Clamp(ToEdgeX, 0, MAX_EXTRA_TILES);
        private int ExtraViewHeight => Math.Clamp(ToEdgeY, 0, MAX_EXTRA_TILES);

        public override void Render(IScreenSurface screen)
        {
            GameHost.Instance.DrawCalls.Enqueue(
                new SadConsole.DrawCalls.DrawCallTexture(
                    BackingTexture, 
                    new Vec2(
                        screen.AbsoluteArea.Position.X, 
                        screen.AbsoluteArea.Position.Y)
                        -
                    new Vec2(
                        ViewportPixelOffset.X,
                        ViewportPixelOffset.Y)));
        }

        public override void Refresh(IScreenSurface screen, bool force = false)
        {
            if (!force && !screen.IsDirty && BackingTexture != null) return;

            var extW = screen.AbsoluteArea.Width + screen.FontSize.X * MAX_EXTRA_TILES;
            var extH = screen.AbsoluteArea.Height + screen.FontSize.Y * MAX_EXTRA_TILES;

            var exVW = ExtraViewWidth;
            var exVH = ExtraViewHeight;
            
            var vW = screen.Surface.View.Width + exVW;
            var vH = screen.Surface.View.Height + exVH;

            // Update texture if something is out of size.
            if (BackingTexture == null || extW != BackingTexture.Width || extH != BackingTexture.Height)
            {
                BackingTexture?.Dispose();
                BackingTexture = new RenderTarget2D(SadConsole.MonoGame.Global.GraphicsDevice, extW, extH, false, SadConsole.MonoGame.Global.GraphicsDevice.DisplayMode.Format, DepthFormat.Depth24);
            }

            // Update cached drawing rectangles if something is out of size.
            if (_renderRects == null || _renderRects.Length != vW * vH || _renderRects[0].Width != screen.FontSize.X || _renderRects[0].Height != screen.FontSize.Y)
            {
                _renderRects = new XnaRectangle[vW * vH];

                for (int i = 0; i < _renderRects.Length; i++)
                {
                    var position = SadRogue.Primitives.Point.FromIndex(i, vW);
                    _renderRects[i] = screen.Font.GetRenderRect(position.X, position.Y, screen.FontSize).ToMonoRectangle();
                }
            }

            screen.Surface.ViewWidth += exVW;
            screen.Surface.ViewHeight += exVH;

            // Render parts of the surface
            RefreshBegin(screen);

            if (screen.Tint.A != 255)
                RefreshCells(screen.Surface, screen.Font);

            RefreshEnd(screen);

            screen.Surface.ViewWidth -= exVW;
            screen.Surface.ViewHeight -= exVH;

            screen.IsDirty = false;
        }
    }
}