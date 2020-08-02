using System;
using System.Collections.Generic;
using System.Linq;
using Server.Data;
using Server.Random;
using Util.Functional;

namespace Server.Logic.MapGen
{
    public static partial class Dungeon
    {
        [CodeGen.DatabaseType]
        public partial class Parameters
        {
            int dungeonWidth;
            int dungeonHeight;

            int roomMinWidth;
            int roomMaxWidth;
            int roomMinHeight;
            int roomMaxHeight;
            
            int roomCountMin;
            int roomCountMax;

            int roomMarginX;
            int roomMarginY;

            public const int MIN_MAP_SIZE = 8;
            public const int MIN_ROOM_SIZE = 3;
            public const int MIN_ROOMS = 1;
            public const int MIN_ROOM_MARGIN = 2;
        }

        private static bool IsRegionVacant(List<MapRoom> rooms, Vec2i pos, Vec2i size)
        {
            pos = (pos.x < 0 ? 0 : pos.x, pos.y < 0 ? 0 : pos.y);

            // TODO: replace with rect and .Intersects
            return !rooms.Any(room =>
                pos.x <= room.Pos.x + room.Size.x && pos.x + size.x >= room.Pos.x &&
                pos.y <= room.Pos.y + room.Size.y && pos.y + size.y >= room.Pos.y);
        }

        private static IEnumerable<Vec2i> GetLocalPointsOnPerimeter(this MapRoom room)
        {
            for (int y = 1; y < room.Size.y - 1; y++)
            {
                yield return (0, y);
                yield return (room.Size.x - 1, y);
            }
            for (int x = 1; x < room.Size.x - 1; x++)
            {
                yield return (x, 0);
                yield return (x, room.Size.y - 1);
            }
        }

        private static IEnumerable<Vec2i> GetGlobalPointsOnPerimeter(this MapRoom room)
            => GetLocalPointsOnPerimeter(room).Select(x => x + room.Pos);

        private static Option<Vec2i> GetRandomPortPosition(this MapRoom room, TileMap map, IRandomSource rng)
             => rng.PickFrom(
                    room
                    .GetGlobalPointsOnPerimeter()
                    // ports can't be located at the bounding edges of the map
                    .Where(x => x.x != 0 && x.y != 0 && x.x != map.Width - 1 && x.y != map.Height - 1)
                    // ports can't be adjacent to other ports
                    .Where(x => room.Ports.All(port => !x.Adjacent(port))));

        private static bool IsValidCorridorPosition(TileMap map, TileDesc tile)
        {
            if (map[tile.pos] != TileType.None && map[tile.pos] != TileType.Corridor)
                return false;
            
            if (tile.pos.x == 0 || tile.pos.y == 0 || 
                tile.pos.x == map.Width - 1 || tile.pos.y == map.Height - 1)
                return false;

            return true;
        }

        public static TileMap Generate(Parameters gen, IRandomSource rng)
        {
            // TODO: Validate generation params (function should return Result<TileMap>)

            var map = new TileMap(gen.DungeonWidth, gen.DungeonHeight, TileType.None);
            var rooms = new List<MapRoom>();
            var roomCount = rng.Next(gen.RoomCountMin, gen.RoomCountMax);

            for (int i = 0; i < roomCount; i++)
            {
                for (int _ = 0; _ < 100; _++)
                {
                    int left = rng.Next(gen.RoomMarginX, map.Width - gen.RoomMarginX - gen.RoomMinWidth);
                    int top = rng.Next(gen.RoomMarginY, map.Height - gen.RoomMarginY - gen.RoomMinHeight);
                    int right = rng.Next(left + gen.RoomMinWidth, Math.Min(map.Width - gen.RoomMarginX, left + gen.RoomMaxWidth));
                    int bottom = rng.Next(top + gen.RoomMinHeight, Math.Min(map.Height - gen.RoomMarginY, top + gen.RoomMaxHeight));

                    Vec2i pos = (left, top);
                    Vec2i size = (right - left, bottom - top);

                    if (IsRegionVacant(rooms, pos, size))
                    {
                        rooms.Add(map.DefineRoom(pos, size, new Util.ValueList<Vec2i>()));
                        break;
                    }
                }
            }

            // carve out allocated rooms
            foreach (var room in rooms)
            {
                // first fill with wall
                for (int i = 0; i < room.Size.x; i++)
                    for (int j = 0; j < room.Size.y; j++)
                        map[room.Pos.x+i, room.Pos.y+j] = TileType.Wall;

                // then dig the floor out of the center
                for (int i = 1; i < room.Size.x-1; i++)
                    for (int j = 1; j < room.Size.y-1; j++)
                        map[i+room.Pos.x, j+room.Pos.y] = TileType.Floor;
            }

            // form corridors through pairwise dungeon rooms
            // ex. ([5 -> 2], [2 -> 3], [3 -> 0], [0 -> 1], ...)
            // this ensures the whole dungeon is walkable
            // TODO: could actually parameterize whether corridor intersection is allowed
            var nodes = rng.Shuffle(Enumerable.Range(0, rooms.Count)).ToList();
            List<(int from, int to)> edges = nodes.Zip(nodes.Skip(1), ValueTuple.Create).ToList();

            foreach (var (srcRoom, dstRoom) in edges.Select(x => (rooms[x.from], rooms[x.to])))
            {
                for (int _ = 0; _ < 100; _++)
                {
                    // get random tiles on perimeters of src and dst
                    var srcTile = srcRoom.GetRandomPortPosition(map, rng).Value;
                    var dstTile = dstRoom.GetRandomPortPosition(map, rng).Value;

                    map[srcTile] = map[dstTile] = TileType.Corridor;

                    // use BFS to find path from srcEntrance to dstEntrance
                    var path = Map.FindPathBFS(map,
                                               srcTile,
                                               dstTile,
                                               x => IsValidCorridorPosition(map, x));

                    // if we can't find a valid path from one room to the next, try again
                    if (path == null)
                    {
                        map[srcTile] = map[dstTile] = TileType.Wall;
                        continue;
                    }
                    
                    // set all tiles on path to corridor
                    foreach (var p in path)
                        if (map[p] != TileType.Floor)
                            map[p] = TileType.Corridor;

                    srcRoom.Ports.Add(srcTile);
                    dstRoom.Ports.Add(dstTile);
                    
                    break;
                }
            }

            // finally replace all placeholder tiles with wall
            for (int i = 0; i < map.Width; i++)
                for (int j = 0; j < map.Height; j++)
                    if (map[i,j] == TileType.None)
                        map[i,j] = TileType.Wall;

            return map;
        }
    }
}