using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Server.Data;
using Xunit;

namespace Tests.Server.PathingTests
{
    public class BFS
    {
        private static bool Passability(TileDesc tile) => tile.type == TileType.Floor;

        private static TileMap BuildMap(byte[,] tiles)
        {
            var map = new TileMap(tiles.GetLength(0), tiles.GetLength(1), TileType.Wall);
            for (int j = 0; j < map.Height; j++)
                for (int i = 0; i < map.Width; i++)
                    map[i,j] = tiles[j,i] == 0 ? TileType.Floor : TileType.Wall;
            return map;
        }
        
        private static IEnumerable<Vec2i>? Pathfind(byte[,] map, Vec2i start, Vec2i destination)
             => global::Server.Logic.Map.FindPathBFS(
                    BuildMap(map),
                    start,
                    destination,
                    Passability,
                    true);

        private static IEnumerable<Vec2i>? Pathfind(byte[,] map)
        {
            Vec2i src = (-1,-1);
            Vec2i dst = (-1,-1);
            for (int i = 0; i < map.GetLength(1); i++)
            {
                for (int j = 0; j < map.GetLength(0); j++)
                {
                    if (map[j,i] == 2)
                        src = (i, j);
                    if (map[j,i] == 3)
                        dst = (i, j);
                }
            }

            if (src.x < 0 || src.y < 0 || dst.x < 0 || dst.y < 0)
                throw new System.Exception("Pathfinding test data is missing source or destination point");

            map[src.y, src.x] = 0;
            map[dst.y, dst.x] = 0;

            return Pathfind(map, src, dst);
        }

        [Fact] public void PathFromTileToItselfHasZeroLength()
        {
            var path = Pathfind(new byte[,] {
                    { 0, 0, 0 },
                    { 0, 0, 0 },
                    { 0, 0, 0 },
                },
                (1, 1),
                (1, 1));
            path.Should().NotBeNull();
            path.Should().BeEmpty();
        }

        [Fact] public void PathToAdjacentFloorTileHasOneLength()
        {
            var path = Pathfind(new byte[,] {
                    { 0, 0, 0 },
                    { 0, 2, 3 },
                    { 0, 0, 0 },
                });
            path.Should().NotBeNull();
            path.Count().Should().Be(1);
            path.ElementAt(0).Should().Be(new Vec2i(2, 1));
        }

        [Fact] public void PathToAdjacentWallTileIsNull()
        {
            var path = Pathfind(new byte[,] {
                    { 0, 0, 0 },
                    { 0, 0, 1 },
                    { 0, 0, 0 },
                },
                (1, 1),
                (2, 1));
            path.Should().BeNull();
        }

        [Fact] public void PathAroundWalls()
        {
            var path = Pathfind(new byte[,] {
                { 0, 0, 3 },
                { 0, 1, 1 },
                { 2, 1, 0 },
            });
            path.Should().NotBeNull();
            path.Should().BeEquivalentTo(
                new Vec2i(0, 1),
                new Vec2i(1, 0),
                new Vec2i(2, 0)
            );
        }

        [Fact] public void PassabilityFunctionCanPreventMovingThroughCorners()
        {
            Pathfind(new byte[,] {
                { 0, 0, 3 },
                { 0, 1, 1 },
                { 2, 1, 0 },
            }).Should().BeEquivalentTo(
                new Vec2i(0,1),
                new Vec2i(0,0),
                new Vec2i(1,0),
                new Vec2i(2,0)
            );
        }

        [Fact] public void PathThroughWallsIsNotPossible()
        {
            Pathfind(new byte[,] {
                { 0, 0, 0 },
                { 0, 1, 1 },
                { 2, 1, 3 },
            }).Should().BeNull();
        }

        [Fact] public void MazeTest()
        {
            var path = Pathfind(new byte[,] {
                {0, 0, 0, 0, 0},
                {1, 0, 1, 1, 0},
                {0, 0, 1, 2, 0},
                {0, 1, 1, 1, 1},
                {0, 0, 0, 3, 1},
            });
            path.Should().NotBeNull();
            path.Should().StartWith(new object[] {
                new Vec2i(4,1),
                new Vec2i(3,0),
                new Vec2i(2,0),
                new Vec2i(1,1)
            }, (a, b) => a.Equals(b));
            path.Should().EndWith(new object[] {
                new Vec2i(0,3),
                new Vec2i(1,4),
                new Vec2i(2,4),
                new Vec2i(3,4),
            }, (a, b) => a.Equals(b));
        }
    }
}