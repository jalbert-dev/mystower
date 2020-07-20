using System;
using System.Collections.Generic;
using SadConsole;
using SadRogue.Primitives;
using Server.Data;

using static SadConsole.Font;
using static SadConsole.RectangleExtensions;

namespace Client.Consoles
{
    public class TileMap
    {
        private ScreenObject Root = new ScreenObject();

        private Dictionary<string, PerPixelTileMap> layers = new Dictionary<string, PerPixelTileMap>();

        private PerPixelTileMap Grid => layers["grid"];
        private PerPixelTileMap Map => layers["map"];
        private PerPixelTileMap Entity => layers["entity"];

        public SadConsole.IScreenObject EntityLayer => Entity.TransformRoot;

        public Point TileSize => Map.FontSize;

        public bool IsGridVisible { set => Grid.IsVisible = value; }

        public TileMap(IScreenObject parent) : base()
        {
            Root.Parent = parent;

            var mapFont = SadConsole.GameHost.Instance.Fonts["Tileset"];
            var mapFontSize = mapFont.GetFontSize(Sizes.Four);

            var gridFont = SadConsole.GameHost.Instance.Fonts["Directionals"];
            var gridFontSize = gridFont.GetFontSize(Sizes.Four);

            var pixelWidth = SadConsole.Settings.Rendering.RenderWidth;
            var pixelHeight = SadConsole.Settings.Rendering.RenderHeight;

            layers["map"] = new PerPixelTileMap(Root)
            {
                DefaultBackground = Color.Black,
                Font = mapFont,
                FontSize = mapFontSize,
            };

            layers["grid"] = new PerPixelTileMap(Root)
            {
                DefaultBackground = Color.Transparent,
                Font = gridFont,
                FontSize = gridFontSize,
            };

            layers["entity"] = new PerPixelTileMap(Root)
            {
                DefaultBackground = Color.Transparent,
                Font = mapFont,
                FontSize = mapFontSize,
            };

            ResizeViewportPx(pixelWidth, pixelHeight);
        }

        public void RebuildTileMap(Server.Data.TileMap map)
        {
            int w = map.Width;
            int h = map.Height;

            foreach (var layer in layers.Values)
            {
                layer.SetMapSize(w, h);
                layer.Clear();
            }

            // VERY temporary...
            var r = new Random();
            var Grass = new List<int> { 5, 6, 7, 1 };
            var Tree = new List<int> { 48, 49, 50, 51 };

            for (int i = 0; i < w; i++)
            {
                for (int j = 0; j < h; j++)
                {
                    Map.SetGlyph(i, j,
                        map[i,j] == 0 ? Grass[r.Next(4)] : Tree[r.Next(4)],
                        map[i,j] == 0 ? Color.Lerp(Color.DarkGreen, Color.DarkOliveGreen, (float)r.NextDouble()) : Color.DarkGreen,
                        Color.Lerp(new Color(0, 40, 0), new Color(0, 34, 0), (float)r.NextDouble()));
                    Grid.SetGlyph(i, j, 10, Color.Black.SetAlpha(128));
                }
            }
        }

        public void CenterViewOn(MapActor cameraFocus)
        {
            foreach (var layer in layers.Values)
                layer.CenterViewOn(cameraFocus.Position.X, cameraFocus.Position.Y);
        }

        public void ResizeViewportPx(int width, int height)
        {
            foreach (var layer in layers.Values)
                layer.ResizeViewportPx(width, height);
        }

        public void SetMapSize(int tileWidth, int tileHeight)
        {
            foreach (var layer in layers.Values)
                layer.SetMapSize(tileWidth, tileHeight);
        }
    }
}