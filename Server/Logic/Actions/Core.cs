using System.Collections.Generic;
using Server.Data;

namespace Server.Logic
{
    public static partial class Actions
    {
        public class Idle : IAction
        {
            public void Execute(IEnumerable<IGameClient> clients, GameState gs, Actor actor) {
                actor.timeUntilAct = 10;
            }
        }

        public class Move : IAction
        {
            public Move(int dx, int dy)
            {
                this.dx = dx;
                this.dy = dy;
            }

            public int dx { get; }
            public int dy { get; }

            public void Execute(IEnumerable<IGameClient> clients, GameState gs, Actor actor)
            {
                var dstX = actor.position.x + dx;
                var dstY = actor.position.y + dy;
                if (Map.CanMoveInto(gs.map, gs.actors, dstX, dstY))
                {
                    (actor.position.x, actor.position.y) = (dstX, dstY);

                    foreach (var c in clients)
                        c.OnEntityMove(actor, dx, dy);

                    // if action was successful, set time till next action accordingly
                    actor.timeUntilAct = 20;
                }
            }
        }
    }
}