using System.Collections.Generic;
using SadConsole;
using SadRogue.Primitives;
using Microsoft.Xna.Framework.Graphics;
using Server.Data;

namespace Client
{
    public static partial class Consoles
    {
        public class MiniMap : System.IDisposable
        {
            private readonly SadConsole.Console terrainLayer;
            private readonly SadConsole.Console wallLayer;
            private readonly SadConsole.Console actorLayer;

            private Texture2D? wallAtlas;

            private static readonly ColoredGlyph FLOOR = new ColoredGlyph(Color.CornflowerBlue, Color.Transparent, 1);
            private static readonly ColoredGlyph ROAD = new ColoredGlyph(Color.DarkViolet, Color.Transparent, 1);

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

                wallLayer = new Console(1, 1)
                {
                    UsePixelPositioning = true,
                    DefaultBackground = Color.Transparent,
                    Parent = terrainLayer,
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

            private const int AtlasWidth = 8;
            private const int AtlasHeight = 8;

            private static (Color[] pixels, int[] ids) BuildOutlineAtlas(Server.Data.TileMap data, int tileWidth, int tileHeight)
            {
                var maskToId = new Dictionary<int, int>();

                Color[] pixels = new Color[tileWidth * tileHeight * AtlasWidth * AtlasHeight];
                int[] ids = new int[data.Width * data.Height];

                // tile 0 is blank, tile 1 is reserved for filled tile required by Font
                int atlasCursorId = 2;

                void setPixel(int x, int y, Color c) => pixels[y * AtlasWidth * tileWidth + x] = c;
                int fromCursorSpaceX(int x) => (atlasCursorId % AtlasWidth) * tileWidth + x;
                int fromCursorSpaceY(int y) => (atlasCursorId / AtlasHeight) * tileHeight + y;
                void draw(int x, int y) => setPixel(fromCursorSpaceX(x), fromCursorSpaceY(y), Color.White);
                void drawLineHoriz(int y)
                {
                    for (int i = 0; i < tileWidth; i++) 
                        setPixel(fromCursorSpaceX(i), fromCursorSpaceY(y), Color.White);
                }
                void drawLineVert(int x)
                {
                    for (int i = 0; i < tileHeight; i++) 
                        setPixel(fromCursorSpaceX(x), fromCursorSpaceY(i), Color.White);
                }
                int hasBorder(int x, int y) =>
                    (x >= 0 && x < data.Width &&
                    y >= 0 && y < data.Height &&
                    data[x,y] != TileType.Wall) ? (byte)0xFF : (byte)0x00;

                // fill tile 1 with white as required by Font
                for (int j = 0; j < tileHeight; j++)
                    for (int i = 0; i < tileWidth; i++)
                        setPixel(tileWidth + i, j, Color.White);

                for (int y = 0; y < data.Height; y++)
                {
                    for (int x = 0; x < data.Width; x++)
                    {
                        // compute bitmask of each Wall for surrounding non-Wall
                        if (data[x, y] != TileType.Wall)
                            continue;

                        int mask = 0;
                        bool maskFlagSet(int f) => (mask & f) == f;
                        //    8 bit mask. High bits are corner mask. Remove corner bits if corresponding low bits are set.
                        mask |= 0b00000001 & hasBorder(x-1, y);
                        mask |= 0b00000010 & hasBorder(x+1, y);
                        mask |= 0b00000100 & hasBorder(x, y-1);
                        mask |= 0b00001000 & hasBorder(x, y+1);
                        mask |= 0b00010000 & (hasBorder(x-1, y-1) & ~(hasBorder(x-1, y) | hasBorder(x, y-1)));
                        mask |= 0b00100000 & (hasBorder(x+1, y-1) & ~(hasBorder(x+1, y) | hasBorder(x, y-1)));
                        mask |= 0b01000000 & (hasBorder(x-1, y+1) & ~(hasBorder(x-1, y) | hasBorder(x, y+1)));
                        mask |= 0b10000000 & (hasBorder(x+1, y+1) & ~(hasBorder(x+1, y) | hasBorder(x, y+1)));


                        // if pixelmask is in Dictionary<PixelMask, int>, assign that to ids[i,j]
                        if (!maskToId.ContainsKey(mask))
                        {
                            // if not, reify the pixelmask to the current atlas tile, add that tile ID to dict, and move to next tile
                            maskToId[mask] = atlasCursorId;

                            // draw tile to atlas based on mask
                            if (maskFlagSet(0b00000001))
                                drawLineVert(0);
                            if (maskFlagSet(0b00000010))
                                drawLineVert(tileWidth - 1);
                            if (maskFlagSet(0b00000100))
                                drawLineHoriz(0);
                            if (maskFlagSet(0b00001000))
                                drawLineHoriz(tileHeight - 1);
                            if (maskFlagSet(0b00010000))
                                draw(0, 0);
                            if (maskFlagSet(0b00100000))
                                draw(tileWidth - 1, 0);
                            if (maskFlagSet(0b01000000))
                                draw(0, tileHeight - 1);
                            if (maskFlagSet(0b10000000))
                                draw(tileWidth - 1, tileHeight - 1);

                            atlasCursorId++;
                        }

                        ids[y * data.Width + x] = maskToId[mask];
                    }
                }

                return (pixels, ids);
            }

            public void RebuildTerrain(Server.Data.TileMap terrainData)
            {
                terrainLayer.Resize(terrainData.Width, terrainData.Height, terrainData.Width, terrainData.Height, true);
                wallLayer.Resize(terrainData.Width, terrainData.Height, terrainData.Width, terrainData.Height, true);
                actorLayer.Resize(terrainData.Width, terrainData.Height, terrainData.Width, terrainData.Height, false);

                DisposeAtlas();
                var (tileWidth, tileHeight) = (terrainLayer.Font.GlyphWidth, terrainLayer.Font.GlyphHeight);
                var (atlasPixels, atlasIndexes) = BuildOutlineAtlas(terrainData, tileWidth, tileHeight);
                wallAtlas = new Texture2D(SadConsole.Host.Global.GraphicsDevice,
                                          AtlasWidth * tileWidth,
                                          AtlasHeight * tileHeight);
                wallAtlas.SetData(atlasPixels);
                wallLayer.Font = new Font(tileWidth,
                                          tileHeight,
                                          0,
                                          AtlasHeight,
                                          AtlasWidth,
                                          1,
                                          new SadConsole.Host.GameTexture(wallAtlas),
                                          "",
                                          new Dictionary<int, Rectangle>());
                wallLayer.FontSize = terrainLayer.FontSize;
                
                for (int j = 0; j < terrainData.Height; j++)
                {
                    for (int i = 0; i < terrainData.Width; i++)
                    {
                        if (terrainData[i, j] == TileType.Floor)
                            terrainLayer.SetCellAppearance(i, j, FLOOR);
                        else if (terrainData[i, j] == TileType.Road)
                            terrainLayer.SetCellAppearance(i, j, ROAD);
                        
                        wallLayer.SetGlyph(i, j, atlasIndexes[j * terrainData.Width + i]);
                    }
                }
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

            public void Reposition(int screenWidth, int _)
            {
                terrainLayer.Position = new Point(
                    screenWidth - terrainLayer.AbsoluteArea.Size.X - MarginPx,
                    MarginPx);
            }

            private void DisposeAtlas()
            {
                if (wallAtlas != null && !wallAtlas.IsDisposed)
                {
                    wallAtlas.Dispose();
                    wallAtlas = null;
                }
            }

            public void Dispose()
            {
                DisposeAtlas();
            }
        }
    }
}