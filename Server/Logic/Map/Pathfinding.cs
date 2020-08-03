using System;
using System.Collections.Generic;
using System.Linq;
using Server.Data;

namespace Server.Logic
{
    public static partial class Map
    {
        public static IEnumerable<Vec2i>? FindPathBFS(this TileMap map,
                                                      Vec2i src,
                                                      Vec2i dst,
                                                      Func<TileDesc, bool> passabilityPredicate,
                                                      bool allowDiagonalMove = true)
        {
            var toVisit = new Queue<Vec2i>();
            var parents = new Dictionary<Vec2i, Vec2i>();

            toVisit.Enqueue(src);

            while (toVisit.Count > 0)
            {
                var current = toVisit.Dequeue();

                foreach (var child in map
                        .SurroundingTiles(current.x, current.y, allowDiagonalMove)
                        .Where(x => !parents.ContainsKey(x.pos))
                        .Where(passabilityPredicate))
                {
                    toVisit.Enqueue(child.pos);
                    parents.Add(child.pos, current);

                    if (child.pos == dst)
                    {
                        IList<Vec2i> path = new List<Vec2i>();
                        Vec2i ptr = dst;
                        while (ptr != src)
                        {
                            path.Add(ptr);
                            ptr = parents[ptr];
                        }
                        return path.Reverse();
                    }
                }
            }

            return null;
        }
    }
}