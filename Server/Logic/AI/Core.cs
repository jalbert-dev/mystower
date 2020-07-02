using Util;

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
            if (Map.CanMoveInto(gs.map, gs.actors, actor.position.x + 1, actor.position.y + 1))
                return new Actions.Move(1, 1);
            else
                return new Actions.Idle();
        };
    }
}