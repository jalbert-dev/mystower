using System.Collections.Generic;
using Server.Data;

namespace Server.Logic
{
    public static class Map
    {
        public static bool IsTileWalkable(MapData map, int x, int y)
            => map.tiles[x, y] == 0;

        public static bool IsTileOccupied(MapData map, IEnumerable<Actor> actors, int x, int y)
        {
            var target = new Vec2i { x=x, y=y };
            foreach (var a in actors)
                if (a.position == target)
                    return true;
            return false;
        }

        public static bool IsInBounds(MapData map, int x, int y)
            => (x >= 0 && y >= 0 && x < map.Width && y < map.Height);

        public static bool CanMoveInto(MapData map, IEnumerable<Actor> actors, int x, int y)
            => IsInBounds(map, x, y) && 
               IsTileWalkable(map, x, y) && 
               !IsTileOccupied(map, actors, x, y);
    }
}