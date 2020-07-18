using System;
using System.Collections.Generic;
using Server.Data;
using Util.Functional;

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
                if (actor.TimeUntilAct < min)
                {
                    rv = actor;
                    min = actor.TimeUntilAct;
                }
            }
            return rv.ToOption();
        }

        public static void AdvanceTime(Actor actor, int dt)
        {
            actor.TimeUntilAct = actor.TimeUntilAct - dt;
        }

        public static void AdvanceTime(GameState gs, int dt)
        {
            foreach (var actor in gs.Actors)
                AdvanceTime(actor, dt);
        }
    }
}