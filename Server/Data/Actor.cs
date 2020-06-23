using Server.Util;

namespace Server.Data
{
    /// A serializable structure representing a living actor in the game world.
    public class Actor
    {
        public Vec2i position;
        public string aiType = "";
        public int timeUntilAct;

        public override string ToString() => this.ToJsonString();
    }
}