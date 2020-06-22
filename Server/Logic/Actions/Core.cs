using System.Collections.Generic;
using Server.Data;

namespace Server.Logic.Actions
{
    public class Idle : IAction
    {
        public void Execute(IEnumerable<IGameClient> clients, GameState gs, Actor actor) { }
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
            actor.position.x += dx;
            actor.position.y += dy;

            foreach (var c in clients)
                c.OnEntityMove(actor, dx, dy);
        }
    }
}