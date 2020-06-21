using System;
using System.Collections.Generic;
using Server.Data;
using Server.Util.Functional;

namespace Server.Logic
{
    public static class TurnController
    {
        public static Option<Actor> GetNextToAct(IEnumerable<Actor> actors)
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

        public static void AdvanceTime(Actor actor, int dt)
        {
            actor.timeUntilAct = Math.Max(0, actor.timeUntilAct - dt);
        }

        public static void AdvanceTime(GameState gs, int dt)
        {
            foreach (var actor in gs.actors)
                AdvanceTime(actor, dt);
        }
    }
}