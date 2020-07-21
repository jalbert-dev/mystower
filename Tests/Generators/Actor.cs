using System.Linq;
using FsCheck;
using Server.Data;
using Server.Database;

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
        
        public static Gen<ActorStatus> DefaultActorStatus()
             => from hp in Gen.Choose(1, 30)
                select new ActorStatus(hp: hp);

        private static readonly ActorArchetype defaultArchetype =
            new ActorArchetype(
                lvlMinStatus: new StatBlock(),
                lvlMaxStatus: new StatBlock(),
                defaultAiType: "",
                nameId: "",
                appearanceId: "");

        public static global::Util.Database DefaultDatabase =
            new global::Util.Database();
        
        static ActorGen()
        {
            DefaultDatabase.AddDatabase(
                new System.Collections.Generic.Dictionary<string, ActorArchetype>()
                {
                    ["DefaultArchetype"] = defaultArchetype
                }
            );
        }

        public static Arbitrary<Actor> Default()
             => Arb.From(
                    from pos in Gen.Choose(0, 30).Two()
                    from facing in RandomFacing()
                    from lvl in Gen.Choose(1, 99)
                    from ct in Gen.Choose(0, 999)
                    from status in DefaultActorStatus()
                    select new Actor(
                        position: new Vec2i{x=pos.Item1, y=pos.Item2},
                        facing: facing,
                        aiType: "Idle",
                        timeUntilAct: ct,
                        level: lvl,
                        status: status,
                        archetype: defaultArchetype));

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