using System.Collections.Generic;
using System.Linq;
using Server.Data;

namespace Server.Logic
{
    public static partial class Map
    {
        public static bool IsTileWall(TileType tileType)
            => tileType switch
            {
                TileType.Wall => true,
                _ => false,
            };

        public static bool IsTileWalkable(TileType tileType)
            => !IsTileWall(tileType) && tileType switch
            {
                TileType.Floor => true,
                TileType.Corridor => true,
                _ => false,
            };
        public static bool IsTileWalkable(TileMap map, int x, int y) => IsTileWalkable(map[x,y]);

        public static IEnumerable<Vec2i> GetTilesInRoom(MapRoom room)
        {
            for (int j = 0; j < room.Size.y; j++)
                for (int i = 0; i < room.Size.x; i++)
                    yield return (room.Pos.x + i, room.Pos.y + j);
        }

        public static IEnumerable<Vec2i> GetWalkableTilesInRoom(MapRoom room, TileMap map)
            => GetTilesInRoom(room).Where(x => Logic.Map.IsTileWalkable(map[x]));

        public static IEnumerable<Vec2i> GetUnoccupiedTilesInRoom(MapRoom room, TileMap map, IEnumerable<Actor> actors)
            => GetTilesInRoom(room).Where(x => Logic.Map.IsTileOccupiable(map, actors, x.x, x.y));

        public static bool IsTileOccupied(IEnumerable<Actor> actors, int x, int y)
        {
            var target = new Vec2i { x=x, y=y };
            foreach (var a in actors)
                if (a.Position == target)
                    return true;
            return false;
        }

        public static bool IsInBounds(TileMap map, int x, int y)
            => (x >= 0 && y >= 0 && x < map.Width && y < map.Height);

        public static bool IsTileOccupiable(TileMap map, IEnumerable<Actor> actors, int x, int y)
            => IsInBounds(map, x, y) && 
               IsTileWalkable(map, x, y) && 
               !IsTileOccupied(actors, x, y);

        public static bool IsMoveBlockedByDiagonalWall(TileMap map, Vec2i moveSrc, Vec2i moveDst)
        {
            var delta = moveDst - moveSrc;

            // if we're moving in a straight line, we're not trying to move diagonally, so we're good
            if (delta.x == 0 || delta.y == 0)
                return false;

            var diag1 = new Vec2i(delta.x, 0);
            var diag2 = new Vec2i(0, delta.y);

            return IsTileWall(map[moveSrc + diag1]) || IsTileWall(map[moveSrc + diag2]);
        }

        public static bool CanMoveFromAToB(TileMap map, IEnumerable<Actor> actors, Vec2i a, Vec2i b)
             => IsTileOccupiable(map, actors, b.x, b.y) && 
                !IsMoveBlockedByDiagonalWall(map, a, b);
    }
}