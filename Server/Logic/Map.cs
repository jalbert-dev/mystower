using System.Collections.Generic;
using Server.Data;

namespace Server.Logic
{
    public static class Map
    {
        public static bool IsTileWalkable(TileMap map, int x, int y)
            => map[x, y] == 0;

        public static bool IsTileOccupied(TileMap map, IEnumerable<Actor> actors, int x, int y)
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
               !IsTileOccupied(map, actors, x, y);
    }
}