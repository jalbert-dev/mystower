using Util;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

namespace Server.Data
{
    [Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptIn)]
    public class TileMap<T> : IEquatable<TileMap<T>>, IEnumerable<(int x, int y, T type)> where T : IEquatable<T>
    {
        /// <summary>
        /// A 2D array representing the tiles of the map. Each entry of the array
        /// is a value representing a tile type ID.
        /// </summary>
        [Newtonsoft.Json.JsonProperty]
        T[,] tiles;

        public TileMap(int w, int h)
        {
            tiles = new T[w,h];
        }

        public int Width => tiles.GetLength(0);
        public int Height => tiles.GetLength(1);

        public T this[int x, int y]
        {
            get => tiles[x, y];
            set => tiles[x, y] = value;
        }

        public bool Equals(TileMap<T>? other)
        {
            if (other == null)
                return false;
            if (Width != other.Width || Height != other.Height)
                return false;
            return Tiles().SequenceEqual(other.Tiles());
        }

        public IEnumerable<T> Tiles()
        {
            for (int i = 0; i < Width; i++)
                for (int j = 0; j < Height; j++)
                    yield return tiles[i, j];
        }

        public IEnumerator<(int x, int y, T type)> GetEnumerator()
        {
            for (int i = 0; i < Width; i++)
                for (int j = 0; j < Height; j++)
                    yield return (i, j, tiles[i, j]);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}