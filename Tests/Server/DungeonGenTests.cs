using System;
using System.Linq;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using Server.Data;
using Server.Logic;
using Tests.Server.Generators;

namespace Tests.Server.MapGenTests
{
    public static class Dungeon
    {
        [Property(MaxTest=1000)] public static Property ValidateGeneratedMap()
            => Prop.ForAll(
                DungeonMapGen.Default(),
                map => {
                    AssertGeneratedRoomsHaveNoAdjacentPorts(map);
                    AssertBoundsOfMapMustBeWalls(map);
                    AssertGeneratedRoomsAreAllReachable(map);
                });

        private static void AssertGeneratedRoomsHaveNoAdjacentPorts(global::Server.Data.TileMap map)
        {
            foreach (var room in map.Rooms)
            {
                var pairs = from p1 in room.Ports
                            from p2 in room.Ports
                            where p1 != p2
                            select (p1, p2);
                foreach (var (a, b) in pairs)
                {
                    // if a is within 1 tile of b, the ports are adjacent and the room is invalid
                    var dist = b - a;
                    (Math.Abs(dist.x) <= 1 && Math.Abs(dist.y) <= 1).Should().BeFalse();
                }
            }
        }

        private static void AssertBoundsOfMapMustBeWalls(global::Server.Data.TileMap map)
        {
            for (int x = 0; x < map.Width; x++)
            {
                map[x, 0].Should().Be(TileType.Wall);
                map[x, map.Height-1].Should().Be(TileType.Wall);
            }
            for (int y = 0; y < map.Height; y++)
            {
                map[0, y].Should().Be(TileType.Wall);
                map[map.Width-1, y].Should().Be(TileType.Wall);
            }
        }

        private static void AssertGeneratedRoomsAreAllReachable(global::Server.Data.TileMap map)
        {
            var pairs = from r1 in map.Rooms
                        from r2 in map.Rooms
                        where r1 != r2
                        select (r1, r2);
            foreach (var (a, b) in pairs)
            {
                // must exist some BFS path from center of A to center of B
                var cA = a.Pos + a.Size / 2;
                var cB = b.Pos + b.Size / 2;

                Map.FindPathBFS(map, cA, cB, desc => Map.IsTileWalkable(desc.type))
                   .Should()
                   .NotBeNullOrEmpty();
            }
        }

        // [Fact] public static void RoomCountMustBeGreaterThanZero()
        // {
        //     return null;
        // }

        // [Fact] public static void RoomWidthHeightMinMustBeBetween3AndMapWidthHeight()
        // {
        //     return null;
        // }

        // [Fact] public static void RoomWidthHeightMaxMustNotBeSmallerThanRoomMinSize()
        // {
        //     return null;
        // }

        // [Fact] public static void RoomMarginMustBeGreaterThanOne()
        // {
        //     return null;
        // }

        // [Fact] public static void MapWidthHeightMustBeAtLeast8()
        // {
        //     return null;
        // }
    }
}