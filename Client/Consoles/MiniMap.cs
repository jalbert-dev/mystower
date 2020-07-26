using System.Collections.Generic;
using SadConsole;
using SadRogue.Primitives;

namespace Client
{
    public static partial class Consoles
    {
        public class MiniMap
        {
            private class MiniMapRoot : SadConsole.ScreenObject
            {
                private readonly MiniMap host;
                public MiniMapRoot(MiniMap host) => this.host = host;

                public override void Draw(System.TimeSpan delta)
                {
                    if (host.dirty)
                    {
                        byte newAlpha = (byte)(255 * MathHelpers.Clamp(host.Alpha, 0.0f, 1.0f));

                        foreach (var t in host.terrainLayer.Cells)
                        {
                            t.Foreground = t.Foreground.SetAlpha(newAlpha);
                        }
                    }
                    base.Draw(delta);
                }
            }

            private readonly MiniMapRoot root;
            private readonly SadConsole.Console terrainLayer;
            private readonly SadConsole.Console actorLayer;

            private bool dirty = false;

            private static readonly ColoredGlyph WALL = new ColoredGlyph(Color.White, Color.Transparent, 1);
            private static readonly ColoredGlyph FLOOR = new ColoredGlyph(Color.CornflowerBlue, Color.Transparent, 1);

            private static readonly ColoredGlyph PLAYER_ACTOR = new ColoredGlyph(Color.Yellow, Color.Transparent, 6);
            private static readonly ColoredGlyph ENEMY_ACTOR = new ColoredGlyph(Color.Red, Color.Transparent, 6);

            public float Alpha { get; set; } = 0.8f;
            public bool IsVisible
            {
                get => terrainLayer.IsVisible;
                set => terrainLayer.IsVisible = value;
            }

            public MiniMap(IScreenObject parent)
            {
                root = new MiniMapRoot(this)
                {
                    Parent = parent
                };

                terrainLayer = new Console(1, 1)
                {
                    UsePixelPositioning = true,
                    DefaultBackground = Color.Transparent,
                    Parent = root,
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
            }

            public void RebuildTerrain(Server.Data.TileMap terrainData)
            {
                terrainLayer.Resize(terrainData.Width, terrainData.Height, terrainData.Width, terrainData.Height, true);
                actorLayer.Resize(terrainData.Width, terrainData.Height, terrainData.Width, terrainData.Height, false);
                
                for (int i = 0; i < terrainData.Width; i++)
                    for (int j = 0; j < terrainData.Height; j++)
                        terrainLayer.SetCellAppearance(i, j, terrainData[i, j] == 0 ? FLOOR : WALL);
                
                dirty = true;
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
        }
    }
}