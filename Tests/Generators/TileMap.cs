using FsCheck;
using Server.Data;

namespace Tests.Server.Generators
{
    public static partial class TileMapGen
    {
        private static TileMap<byte> MapWithTiles(int w, int h, byte[,] tiles)
        {
            var m = new TileMap<byte>(w, h);
            for (int i = 0; i < w; i++)
                for (int j = 0; j < h; j++)
                    m[i, j] = tiles[i, j];
            return m;
        }

        public static Arbitrary<TileMap<byte>> Default()
             => Arb.From(
                    from w in Gen.Choose(1, 100)
                    from h in Gen.Choose(1, 100)
                    from tiles in Gen.Choose(0, 1).Select(x => (byte)x).Array2DOf(w, h)
                    select MapWithTiles(w, h, tiles));
    }
}