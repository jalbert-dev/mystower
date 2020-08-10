using Util;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using Newtonsoft.Json;
using Server.Random;

namespace Server.Data
{
    [CodeGen.GameDataNode]
    public partial class MapRoom
    {
        Rect region;
        ValueList<Vec2i> ports;
    }

    public enum TileType
    {
        None,
        Floor,
        Wall,
        Corridor,
    }

    public struct TileDesc
    {
        public Vec2i pos;
        public TileType type;

        public TileDesc(Vec2i pos, TileType type)
        {
            this.pos = pos;
            this.type = type;
        }

        public void Deconstruct(out Vec2i p, out TileType t) => (p, t) = (pos, type);
        public void Deconstruct(out int x, out int y, out TileType t) => (x, y, t) = (pos.x, pos.y, type);
    }

    [JsonObject(MemberSerialization.Fields)]
    public class TileMap : IEquatable<TileMap>, IEnumerable<TileDesc>, IDeepCloneable<TileMap>
    {

        /// <summary>
        /// A 2D array representing the tiles of the map. Each entry of the array
        /// is a value representing a tile type ID.
        /// </summary>
        TileType[,] tiles;

        List<MapRoom> rooms = new List<MapRoom>();

        public TileMap(int w, int h, TileType initialValue = 0)
        {
            tiles = new TileType[w,h];
            for (int i = 0; i < w; i++)
                for (int j = 0; j < h; j++)
                    tiles[i,j] = initialValue;
        }

        public MapRoom DefineRoom(Rect region, IEnumerable<Vec2i> ports)
        {
            var room = new MapRoom(region, ports.ToValueList());
            rooms.Add(room);
            return room;
        }

        public int Width => tiles.GetLength(0);
        public int Height => tiles.GetLength(1);
        public IEnumerable<MapRoom> Rooms => rooms;

        public TileType this[int x, int y]
        {
            get => tiles[x, y];
            set => tiles[x, y] = value;
        }

        public TileType this[Vec2i xy]
        {
            get => tiles[xy.x, xy.y];
            set => tiles[xy.x, xy.y] = value;
        }

        public bool Equals(TileMap? other)
        {
            if (other == null)
                return false;
            if (Width != other.Width || Height != other.Height)
                return false;
            return Tiles().SequenceEqual(other.Tiles());
        }

        private bool TryGetTile(int x, int y, out TileDesc v)
        {
            if (x >= 0 && y >= 0 && x < Width && y < Height)
            {
                v = new TileDesc((x, y), this[x,y]);
                return true;
            }
            v = default;
            return false;
        }

        public IEnumerable<TileDesc> Tiles()
        {
            for (int i = 0; i < Width; i++)
                for (int j = 0; j < Height; j++)
                    yield return new TileDesc((i, j), tiles[i, j]);
        }

        public IEnumerable<TileDesc> SurroundingTiles(int x, int y, bool includeDiagonal = true)
        {
            TileDesc v;

            if (TryGetTile(x-1, y+0, out v)) yield return v;
            if (TryGetTile(x+1, y+0, out v)) yield return v;
            if (TryGetTile(x+0, y-1, out v)) yield return v;
            if (TryGetTile(x+0, y+1, out v)) yield return v;

            if (!includeDiagonal)
                yield break;
            
            if (TryGetTile(x-1, y-1, out v)) yield return v;
            if (TryGetTile(x-1, y+1, out v)) yield return v;
            if (TryGetTile(x+1, y-1, out v)) yield return v;
            if (TryGetTile(x+1, y+1, out v)) yield return v;
        }

        public IEnumerator<TileDesc> GetEnumerator()
        {
            for (int i = 0; i < Width; i++)
                for (int j = 0; j < Height; j++)
                    yield return new TileDesc((i, j), tiles[i, j]);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public TileMap DeepClone()
        {
            var result = new TileMap(0, 0)
            {
                tiles = tiles
            };
            return result;
        }
    }
}