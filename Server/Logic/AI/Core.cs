using Server.Util;

namespace Server.Logic
{
    public static partial class AIType
    {
        // Player-controlled actors never select an action for themselves (for now!)
        public static ActionSelector PlayerControlled
            => (_, __) => null;

        public static ActionSelector Idle
            => (_, __) => new Actions.Idle();

        public static ActionSelector MoveRandomly => (gs, actor) => {
            return new Actions.Move(1, 1);
        };
    }
}