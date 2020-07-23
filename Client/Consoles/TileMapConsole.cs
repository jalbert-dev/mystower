using System;
using System.Collections.Generic;
using SadConsole;
using SadRogue.Primitives;

using static SadConsole.Font;

namespace Client
{
    public static partial class Consoles
    {
        public class TileMap
        {
            private readonly ScreenObject Root = new ScreenObject();

            private readonly Dictionary<string, PerPixelTileMap> layers = new Dictionary<string, PerPixelTileMap>();

            private readonly FixedCenterCamera mapCamera;

            private PerPixelTileMap Grid => layers["grid"];
            private PerPixelTileMap Map => layers["map"];
            private PerPixelTileMap Entity => layers["entity"];

            public IScreenObject EntityLayer => Entity.TransformRoot;

            public Point TileSize => Map.FontSize;

            public bool IsGridVisible { set => Grid.IsVisible = value; }

            public TileMap(IScreenObject parent) : base()
            {
                Root.Parent = parent;

                var mapFont = GameHost.Instance.Fonts["Tileset"];
                var mapFontSize = mapFont.GetFontSize(Sizes.Four);

                var gridFont = GameHost.Instance.Fonts["Directionals"];
                var gridFontSize = gridFont.GetFontSize(Sizes.Four);

                var widthPixels = Settings.Rendering.RenderWidth;
                var heightPixels = Settings.Rendering.RenderHeight;

                mapCamera = new FixedCenterCamera(widthPixels, heightPixels);

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

                mapCamera.OnViewSizeChanged += OnCameraViewSizeChange;
                mapCamera.OnCameraPosChanged += OnCameraViewPosChange;
            }

            private void OnCameraViewSizeChange(ICamera cam)
            {
                foreach (var layer in layers.Values)
                    layer.SyncViewSizeWithCamera(cam);
            }

            private void OnCameraViewPosChange(ICamera cam)
            {
                foreach (var layer in layers.Values)
                    layer.SyncViewPosWithCamera(cam);
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
                mapCamera.SetCenter(
                    cameraFocus.Position.X + cameraFocus.Animation.FontSize.X / 2,
                    cameraFocus.Position.Y + cameraFocus.Animation.FontSize.Y / 2);
            }

            public void ResizeViewportPx(int width, int height)
            {
                mapCamera.ViewSize = new Point(width, height);
            }

            public void SetMapSize(int tileWidth, int tileHeight)
            {
                foreach (var layer in layers.Values)
                    layer.SetMapSize(tileWidth, tileHeight);
            }
        }
    }
}