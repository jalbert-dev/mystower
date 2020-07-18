using System;
using Util;

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

        // TODO!: Move into Archetype data structure!
        StatBlock baseStatus;

        int set_timeUntilAct(int value) => Math.Max(0, value);
    }

    [CodeGen.GameDataNode]
    public partial class StatBlock
    {
        int hp;
        int atk;
        int def;
    }

    [CodeGen.GameDataNode]
    public partial class ActorStatus
    {
        int hp;

        int set_hp(int value) => Math.Max(0, value);
    }
}