using Util;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

namespace Server.Data
{
    [Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptIn)]
    public class TileMap : IEquatable<TileMap>, IEnumerable<(int x, int y, byte type)>, IDeepCloneable<TileMap>
    {
        /// <summary>
        /// A 2D array representing the tiles of the map. Each entry of the array
        /// is a value representing a tile type ID.
        /// </summary>
        [Newtonsoft.Json.JsonProperty]
        byte[,] tiles;

        public TileMap(int w, int h)
        {
            tiles = new byte[w,h];
        }

        public int Width => tiles.GetLength(0);
        public int Height => tiles.GetLength(1);

        public byte this[int x, int y]
        {
            get => tiles[x, y];
            set => tiles[x, y] = value;
        }

        public bool Equals(TileMap? other)
        {
            if (other == null)
                return false;
            if (Width != other.Width || Height != other.Height)
                return false;
            return Tiles().SequenceEqual(other.Tiles());
        }

        public IEnumerable<byte> Tiles()
        {
            for (int i = 0; i < Width; i++)
                for (int j = 0; j < Height; j++)
                    yield return tiles[i, j];
        }

        public IEnumerator<(int x, int y, byte type)> GetEnumerator()
        {
            for (int i = 0; i < Width; i++)
                for (int j = 0; j < Height; j++)
                    yield return (i, j, tiles[i, j]);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public TileMap DeepClone()
        {
            var result = new TileMap(0, 0);
            result.tiles = tiles;
            return result;
        }
    }
}