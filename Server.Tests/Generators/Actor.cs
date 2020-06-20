using FsCheck;
using Server.Data;

namespace Server.Tests.Generators
{
    public partial class ActorGen
    {
        public static Arbitrary<Actor> Default()
        {
            return Arb.From(
                from pos in Gen.Choose(0, 30).Two()
                from int ct in Gen.Choose(0, 999)
                select new Actor
                {
                    position = new Vec2i{x=pos.Item1, y=pos.Item2},
                    timeUntilAct = ct,
                }
            );
        }
    }
}