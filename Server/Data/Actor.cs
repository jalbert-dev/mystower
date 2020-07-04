using Util;

namespace Server.Data
{
    /// A serializable structure representing a living actor in the game world.
    public class Actor
    {
        public Vec2i position;
        public Vec2i facing = new Vec2i(0, 1);

        public string aiType = "";
        public int timeUntilAct;

        public int level = 1;
        public ActorStatus status;

        // TODO!: Move into Archetype data structure!
        public StatBlock baseStatus;

        public override string ToString() => this.ToJsonString();
    }

    public struct StatBlock
    {
        public int hp;
        public int atk;
        public int def;
    }

    public struct ActorStatus
    {
        public int hp;
    }
}