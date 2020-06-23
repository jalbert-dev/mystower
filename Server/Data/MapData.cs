using Server.Util;

namespace Server.Data
{
    public class MapData
    {
        /// <summary>
        /// A 2D array representing the tiles of the map. Each entry of the array
        /// is a value representing a tile type ID.
        /// </summary>
        public byte[,] tiles = new byte[0,0];

        public override string ToString() => this.ToJsonString();
    }
}