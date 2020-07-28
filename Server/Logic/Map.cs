using System.Collections.Generic;
using System.Linq;
using Server.Data;

namespace Server.Logic
{
    public static class Map
    {
        public static bool IsTileWalkable(byte tileType)
            => tileType switch
            {
                0 => true,
                2 => true,
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
            => GetTilesInRoom(room).Where(x => Logic.Map.CanMoveInto(map, actors, x.x, x.y));

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

        public static bool CanMoveInto(TileMap map, IEnumerable<Actor> actors, int x, int y)
            => IsInBounds(map, x, y) && 
               IsTileWalkable(map, x, y) && 
               !IsTileOccupied(actors, x, y);
    }
}