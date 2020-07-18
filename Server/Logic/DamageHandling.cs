using System;
using System.Collections.Generic;
using System.Linq;
using Server.Data;
using Server.Database;

namespace Server.Logic
{
    public static class DamageHandling
    {
        public static int CalcDamage(StatBlock attacker, StatBlock target)
        {
            var dmg = Math.Max(1, attacker.atk - target.def);
            return dmg;
        }

        public static IEnumerable<Actor> GetDeadActors(IEnumerable<Actor> actors)
            => actors.Where(x => x.Status.Hp <= 0);
    }
}