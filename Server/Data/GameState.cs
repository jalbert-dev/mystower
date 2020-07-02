using System.Collections.Generic;
using Util;

namespace Server.Data
{
    /// A serializable structure representing the game state, including world, actors, etc.
    public class GameState
    {
        public List<Actor> actors = new List<Actor>();
        public MapData map = new MapData();

        public override string ToString() => this.ToJsonString();
    }
}