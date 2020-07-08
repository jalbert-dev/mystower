using System;
using System.Collections.Generic;
using System.Linq;
using Server.Data;

namespace Server.Logic
{
    public static class DamageHandling
    {
        public static int CalcDamage(Actor attacker, Actor target)
        {
            var dmg = Math.Max(1, attacker.baseStatus.atk - target.baseStatus.def);
            return dmg;
        }

        public static IEnumerable<Actor> GetDeadActors(IEnumerable<Actor> actors)
            => actors.Where(x => x.status.hp <= 0);
    }
}