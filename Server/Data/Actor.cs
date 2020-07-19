using System;
using Server.Database;
using Util;
using Util.Functional;

namespace Server.Data
{
    /// A serializable structure representing a living actor in the game world.
    [CodeGen.GameDataNode]
    public partial class Actor
    {
        Vec2i position;
        Vec2i facing;

        string aiType;
        int timeUntilAct;

        int level;
        ActorStatus status;

        string archetypeId;

        int set_level(int value) => ActorArchetype.ClampLevel(value);
        int set_timeUntilAct(int value) => Math.Max(0, value);

        public static Result<Actor> FromArchetype(int x, int y, int ct, int lvl, string archetypeId, Func<string, Result<ActorArchetype>> lookup)
             => lookup(archetypeId)
                    .Map(archetype => new Actor(
                        position: new Vec2i(x, y), 
                        facing: new Vec2i(0, 1), 
                        aiType: archetype.DefaultAiType, 
                        timeUntilAct: ct,
                        level: lvl,
                        status: ActorStatus.FromArchetype(archetype, lvl),
                        archetypeId: archetypeId));
    }

    [CodeGen.GameDataNode]
    public partial class ActorStatus
    {
        int hp;

        int set_hp(int value) => Math.Max(0, value);

        public static ActorStatus FromArchetype(ActorArchetype archetype, int lvl)
            => new ActorStatus(
                hp: archetype.StatusAtLevel(lvl).hp);
    }
}