using Util;
using System.Linq;
using Server.Data;
using System;
using Server.Random;

namespace Server.Logic
{
    public static partial class AIType
    {
        // Player-controlled actors never select an action for themselves (for now!)
        public static ActionSelector PlayerControlled
            => (_, __) => null;

        public static ActionSelector Idle
            => (_, __) => new Actions.Idle();

        public static ActionSelector MoveRandomly => (gs, actor) =>
            gs.Rng.PickFrom(
                gs.Map.SurroundingTiles(actor.Position.x, actor.Position.y)
                .Where(desc => Map.CanMoveFromAToB(gs.Map, gs.Actors, actor.Position, desc.pos)))
            .Match<IAction>(
                some: desc => new Actions.Move((desc.pos - actor.Position).ToClosestDirection()),
                none: () => new Actions.Idle()
            );

        private static bool IsAdjacent(Vec2i v1, Vec2i v2)
             => v1 != v2 &&
                Math.Abs(v1.x - v2.x) <= 1 &&
                Math.Abs(v1.y - v2.y) <= 1;

        public static ActionSelector GenericEnemy => (gs, actor) => {
            var target = gs.Actors
                .Where(x => x.AiType == nameof(PlayerControlled))
                .OrderBy(x => actor.Position.Distance(x.Position))
                .FirstOrDefault();
            
            if (target == null)
                return MoveRandomly(gs, actor);
            
            var dir = target.Position - actor.Position;
            if (IsAdjacent(target.Position, actor.Position))
            {
                if (actor.Position + actor.Facing.ToVec() != target.Position)
                    return new Actions.Face(dir.ToClosestDirection());
                
                return new Actions.TryAttack();
            }
            else
            {
                int dx = dir.x > 1 ? 1 : dir.x < -1 ? -1 : dir.x;
                int dy = dir.y > 1 ? 1 : dir.y < -1 ? -1 : dir.y;

                if (!Map.CanMoveFromAToB(gs.Map, gs.Actors, actor.Position, actor.Position + (dx, dy)))
                    return new Actions.Idle();

                return new Actions.Move(dx, dy);
            }
        };
    }
}