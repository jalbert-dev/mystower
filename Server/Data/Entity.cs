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

        public Option<Actor> NextToAct
        {
            get
            {
                // the lack of MinBy in LINQ is... surprising! and verbose
                Actor? rv = null;
                int min = int.MaxValue;
                foreach (var actor in actors)
                {
                    if (actor.timeUntilAct < min)
                    {
                        rv = actor;
                        min = actor.timeUntilAct;
                    }
                }
                return rv.ToOption();
            }
        }

        public GameState AdvanceTime(int dt)
        {
            foreach (var actor in actors)
                actor.timeUntilAct = Math.Max(0, actor.timeUntilAct - dt);
            return this;
        }
    }
}