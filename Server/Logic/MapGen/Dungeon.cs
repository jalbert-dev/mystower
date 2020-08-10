using System;
using System.Collections.Generic;
using System.Linq;
using Server.Data;
using Server.Random;
using Util;
using Util.Functional;

namespace Server.Logic.MapGen
{
    public static partial class Dungeon
    {
        public static class Error
        {
            public class RoomCountMinimumMustBeGTZero : IError
            {
                public string Message => $"Room count must be greater than ${Dungeon.Parameters.MIN_ROOMS - 1}.";
            }
            public class MapMarginTooSmall : IError
            {
                public string Message => $"Map margin width/height must be greater than ${Dungeon.Parameters.MIN_ROOM_MARGIN - 1}.";
            }
            public class MapSizeMustBeGreaterThanMinimum : IError
            {
                public string Message => $"Map width/height must be greater than ${Dungeon.Parameters.MIN_MAP_SIZE - 1}.";
            }
            public class RoomSizeMinimumMustBeGreaterThanMinimum : IError
            {
                public string Message => $"Room width/height minimum must be greater than ${Dungeon.Parameters.MIN_ROOM_SIZE - 1}.";
            }
            public class RoomSizeMinimumMustBeLessThanMapSizeAndMargins : IError
            {
                public string Message => $"Room width/height minimum must be no greater than map width/height minus twice the map margin.";
            }
            public class RangeError : IError
            {
                private readonly string fieldName;
                private readonly int min, max;
                public RangeError(string fieldName, int min, int max) 
                    => (this.fieldName, this.min, this.max) = (fieldName, min, max);
                public string Message => $"Invalid range [{min}, {max}] in field '{fieldName}'.";
            }
        }

        [CodeGen.DatabaseType]
        public partial class Parameters
        {
            Vec2i mapSize;
            Vec2i mapMargin;

            IntRange roomWidth;
            IntRange roomHeight;
            IntRange roomCount;

            public const int MIN_MAP_SIZE = 8;
            public const int MIN_ROOM_SIZE = 3;
            public const int MIN_ROOMS = 1;
            public const int MIN_ROOM_MARGIN = 2;
        }

        private static bool IsRegionVacant(List<MapRoom> rooms, Rect region)
        {
            var r = Rect.ClampBounds(region, 0, 0, region.Right, region.Bottom);
            return !rooms.Any(room => Rect.Intersects(room.Region, r));
        }

        private static IEnumerable<Vec2i> GetGlobalPointsOnPerimeter(this MapRoom room)
        {
            for (int y = room.Region.Top + 1; y < room.Region.Bottom - 1; y++)
            {
                yield return (room.Region.Left, y);
                yield return (room.Region.Right - 1, y);
            }
            for (int x = room.Region.Left + 1; x < room.Region.Right - 1; x++)
            {
                yield return (x, room.Region.Top);
                yield return (x, room.Region.Bottom - 1);
            }
        }

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

        private static IError? ValidateParams(Parameters gen)
        {
            if (gen.RoomCount.min < Parameters.MIN_ROOMS)
                return new Error.RoomCountMinimumMustBeGTZero();
            if (gen.MapMargin.x < Parameters.MIN_ROOM_MARGIN ||
                gen.MapMargin.y < Parameters.MIN_ROOM_MARGIN)
                return new Error.MapMarginTooSmall();
            if (gen.MapSize.x < Parameters.MIN_MAP_SIZE ||
                gen.MapSize.y < Parameters.MIN_MAP_SIZE)
                return new Error.MapSizeMustBeGreaterThanMinimum();
            if (gen.RoomWidth.min < Parameters.MIN_ROOM_SIZE ||
                gen.RoomHeight.min < Parameters.MIN_ROOM_SIZE)
                return new Error.RoomSizeMinimumMustBeGreaterThanMinimum();
            if (gen.RoomWidth.min > gen.MapSize.x - gen.MapMargin.x * 2 ||
                gen.RoomHeight.min > gen.MapSize.y - gen.MapMargin.y * 2)
                return new Error.RoomSizeMinimumMustBeLessThanMapSizeAndMargins();
            return null;
        }

        public static Result<TileMap> Generate(Parameters gen, IRandomSource rng)
        {
            var valid = ValidateParams(gen);
            if (valid != null)
                return Result.Error(valid);

            var map = new TileMap(gen.MapSize.x, gen.MapSize.y, TileType.None);
            var rooms = new List<MapRoom>();
            var roomCount = rng.Next(gen.RoomCount);

            for (int i = 0; i < roomCount; i++)
            {
                for (int _ = 0; _ < 100; _++)
                {
                    int left = rng.Next(gen.MapMargin.x, map.Width - gen.MapMargin.x - gen.RoomWidth.min);
                    int top = rng.Next(gen.MapMargin.y, map.Height - gen.MapMargin.y - gen.RoomHeight.min);
                    int right = rng.Next(left + gen.RoomWidth.min, Math.Min(map.Width - gen.MapMargin.x, left + gen.RoomWidth.max));
                    int bottom = rng.Next(top + gen.RoomHeight.min, Math.Min(map.Height - gen.MapMargin.y, top + gen.RoomHeight.max));

                    var region = Rect.FromBounds(left, top, right, bottom);

                    if (IsRegionVacant(rooms, region))
                    {
                        rooms.Add(map.DefineRoom(region, new Util.ValueList<Vec2i>()));
                        break;
                    }
                }
            }

            // carve out allocated rooms
            foreach (var room in rooms)
            {
                // first fill with wall
                for (int j = room.Region.Top; j < room.Region.Bottom; j++)
                    for (int i = room.Region.Left; i < room.Region.Right; i++)
                        map[i, j] = TileType.Wall;

                // then dig the floor out of the center
                for (int j = room.Region.Top + 1; j < room.Region.Bottom - 1; j++)
                    for (int i = room.Region.Left + 1; i < room.Region.Right - 1; i++)
                        map[i, j] = TileType.Floor;
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
                                               (_, __, x) => IsValidCorridorPosition(map, x),
                                               false);

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

            return Result.Ok(map);
        }
    }
}