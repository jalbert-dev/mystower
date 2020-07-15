using System.Collections.Generic;
using Util;

namespace Server.Data
{
    /// A serializable structure representing the game state, including world, actors, etc.
    [CodeGen.GameDataNode]
    public partial class GameState
    {
        Util.ValueList<Actor> actors;
        TileMap<byte> map;
    }
}