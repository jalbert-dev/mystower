using SadConsole;
using SadRogue.Primitives;

using Vec2 = Microsoft.Xna.Framework.Vector2;

namespace Client
{
    public class TileMapRenderer : SadConsole.Renderers.ScreenObjectRenderer
    {
        public Point ViewportPixelOffset;

        public override void Render(IScreenSurface screen)
        {
            // If the tint is covering the whole area, don't draw anything
            if (screen.Tint.A != 255)
            {
                // Draw call for surface
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
        }
    }
}