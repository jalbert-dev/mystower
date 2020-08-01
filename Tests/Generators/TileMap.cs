using FsCheck;
using System.Linq;
using Server.Data;

namespace Tests.Server.Generators
{
    public static partial class TileMapGen
    {
        private static TileMap MapWithTiles(int w, int h, TileType[,] tiles)
        {
            var m = new TileMap(w, h);
            for (int i = 0; i < w; i++)
                for (int j = 0; j < h; j++)
                    m[i, j] = tiles[i, j];
            return m;
        }

        public static Arbitrary<TileMap> Default()
             => Arb.From(
                    from w in Gen.Choose(1, 100)
                    from h in Gen.Choose(1, 100)
                    from tiles in Gen.Elements(System.Enum.GetValues(typeof(TileType)).Cast<TileType>()).Array2DOf(w, h)
                    select MapWithTiles(w, h, tiles));
    }
}