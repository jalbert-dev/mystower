using System;
using System.Collections.Generic;
using System.Linq;
using Server.Util;

namespace Server.Data
{
    public struct Vec2i : IEquatable<Vec2i>
    {
        public int x;
        public int y;

        public bool Equals(Vec2i other) => x == other.x && y == other.y;

        public static Vec2i Zero = new Vec2i { x = 0, y = 0 };
    }

    /// A serializable structure representing a living actor in the game world.
    public class Actor
    {
        public Vec2i position;
        public string aiType = "";
        public int timeUntilAct;
    }
    
    public class MapData
    {
        public byte[] tiles = new byte[0];
    }

    /// AI consists of a stateless function that takes a game state and actor,
    /// and returns an optional action to take. (If no action taken, delegates to client.)

    /// A serializable structure representing the game state, including world, actors, etc.
    public class GameState
    {
        public List<Actor> actors = new List<Actor>();
        public MapData map = new MapData();
    }
}