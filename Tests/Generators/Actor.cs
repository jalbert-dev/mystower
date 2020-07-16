using System.Linq;
using FsCheck;
using Server.Data;

namespace Tests.Server.Generators
{
    public static partial class ActorGen
    {
        public static Gen<Vec2i> RandomFloorTilePos(TileMap m)
            => Gen.Elements(m.Where(x => x.type == 0).Select(x => new Vec2i(x.x, x.y)));

        public static Gen<Vec2i> RandomFacing()
             => Gen.Choose(-1, 1)
                    .Two()
                    .Where(x => !(x.Item1 == 0 && x.Item2 == 0))
                    .Select(x => new Vec2i(x.Item1, x.Item2));

        public static Gen<StatBlock> DefaultStatBlock(int lvl = 1)
             => Gen.Choose(1, 5)
                    .Select(x => x * lvl)
                    .Three()
                    .Select(x => new StatBlock(x.Item1, x.Item2, x.Item3));


        public static Gen<ActorStatus> DefaultActorStatus(StatBlock baseStats)
             => from hp in Gen.Choose(1, baseStats.Hp)
                select new ActorStatus(hp: hp);

        public static Arbitrary<Actor> Default()
             => Arb.From(
                    from pos in Gen.Choose(0, 30).Two()
                    from facing in RandomFacing()
                    from lvl in Gen.Choose(1, 99)
                    from ct in Gen.Choose(0, 999)
                    from baseStats in DefaultStatBlock(lvl)
                    from status in DefaultActorStatus(baseStats)
                    select new Actor(
                        position: new Vec2i{x=pos.Item1, y=pos.Item2},
                        facing: facing,
                        aiType: "Idle",
                        timeUntilAct: ct,
                        level: lvl,
                        baseStatus: baseStats,
                        status: status));

        public static Arbitrary<Actor> WithTimeUntilAct(this Arbitrary<Actor> arb, int ct)
            => arb.Generator.Select(x => {
                x.TimeUntilAct = ct;
                return x;
            }).ToArbitrary();

        private static Actor SetPosition(this Actor a, Vec2i p)
        {
            a.Position = p;
            return a;
        }

        public static Arbitrary<Actor> WithPositionOnMap(this Arbitrary<Actor> arb, TileMap map)
             => Arb.From(
                    from pos in RandomFloorTilePos(map)
                    from actor in arb.Generator
                    select actor.SetPosition(pos));
    }
}