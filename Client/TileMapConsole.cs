using Microsoft.Xna.Framework;
using Server.Data;
using static SadConsole.Font;
using static SadConsole.RectangleExtensions;

namespace Client
{
    public class TileMapConsole : SadConsole.ScrollingConsole
    {
        public TileMapConsole(int w, int h) : base(w, h)
        {
            DefaultBackground = Color.Black;
            Font = Font.Master.GetFont(FontSizes.Three);
        }

        public void RebuildTileMap(MapData map)
        {
            int w = map.Width;
            int h = map.Height;

            Resize(w, h, false);

            Clear();

            for (int i = 0; i < w; i++)
                for (int j = 0; j < h; j++)
                    SetGlyph(i, j, map.tiles[i,j] == 0 ? 46 : '#');
        }

        public void CenterViewOn(MapActor actor)
        {
            this.CenterViewPortOnPoint(actor.Position.PixelLocationToConsole(Font));
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
                    c.Position -= ViewPort.Location.ConsoleLocationToPixel(Font);
            base.Draw(timeElapsed);
        }
    }
}