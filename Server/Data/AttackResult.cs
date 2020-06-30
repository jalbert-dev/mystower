using System.Collections.Generic;
using System.Linq;

namespace Server.Data
{
    public struct AttackResult
    {
        public Data.Actor Target;
        public int DamageDealt;
    }
    public class AttackResults
    {
        public AttackResult[] results;

        public AttackResults(IEnumerable<AttackResult> r) => results = r.ToArray();
    }
}