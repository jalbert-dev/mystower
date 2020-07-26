using System.Collections.Generic;
using SadConsole;
using SadRogue.Primitives;

namespace Client
{
    public static partial class Consoles
    {
        public class MiniMap
        {
            private readonly SadConsole.Console terrainLayer;
            private readonly SadConsole.Console actorLayer;

            private static readonly ColoredGlyph WALL = new ColoredGlyph(Color.White, Color.Transparent, 1);
            private static readonly ColoredGlyph FLOOR = new ColoredGlyph(Color.CornflowerBlue, Color.Transparent, 1);

            private static readonly ColoredGlyph PLAYER_ACTOR = new ColoredGlyph(Color.Yellow, Color.Transparent, 6);
            private static readonly ColoredGlyph ENEMY_ACTOR = new ColoredGlyph(Color.Red, Color.Transparent, 6);

            public float Alpha 
            {
                get => terrainLayer.Renderer.Opacity / 255;
                set
                {
                    terrainLayer.Renderer.Opacity = (byte)(value * 255);
                }
            }
            public int MarginPx { get; set; } = 40;

            public bool IsVisible
            {
                get => terrainLayer.IsVisible;
                set => terrainLayer.IsVisible = value;
            }

            public MiniMap(IScreenObject parent)
            {
                terrainLayer = new Console(1, 1)
                {
                    UsePixelPositioning = true,
                    DefaultBackground = Color.Transparent,
                    Parent = parent,
                    Font = SadConsole.GameHost.Instance.Fonts["Minimap"],
                    FontSize = SadConsole.GameHost.Instance.Fonts["Minimap"].GetFontSize(SadConsole.Font.Sizes.One),
                };

                actorLayer = new Console(1, 1)
                {
                    UsePixelPositioning = true,
                    DefaultBackground = Color.Transparent,
                    Parent = terrainLayer,
                    Font = terrainLayer.Font,
                    FontSize = terrainLayer.FontSize,
                };

                Alpha = 0.5f;
            }

            public void RebuildTerrain(Server.Data.TileMap terrainData)
            {
                terrainLayer.Resize(terrainData.Width, terrainData.Height, terrainData.Width, terrainData.Height, true);
                actorLayer.Resize(terrainData.Width, terrainData.Height, terrainData.Width, terrainData.Height, false);
                
                for (int i = 0; i < terrainData.Width; i++)
                    for (int j = 0; j < terrainData.Height; j++)
                        terrainLayer.SetCellAppearance(i, j, terrainData[i, j] == 0 ? FLOOR : WALL);
            }

            public void RebuildLocalActorDisplay(IEnumerable<MapActor> actors, Point mapTileSize)
            {
                actorLayer.Clear();
                foreach (var actor in actors)
                {
                    var tilePos = actor.Position / mapTileSize;
                    actorLayer.SetCellAppearance(
                        tilePos.X,
                        tilePos.Y,
                        ENEMY_ACTOR);
                }
            }

            public void Reposition(int screenWidth, int screenHeight)
            {
                terrainLayer.Position = new Point(
                    screenWidth - terrainLayer.AbsoluteArea.Size.X - MarginPx,
                    MarginPx);
            }
        }
    }
}