using System;
using System.Collections.Generic;
using System.Linq;
using Server.Data;

namespace Server.Logic
{
    public static partial class Actions
    {
        public class Idle : IAction
        {
            public int Execute(IEnumerable<IGameClient> clients, GameState gs, Actor actor) {
                foreach (var c in clients)
                    c.OnAddLogMessage("[c:r f:blue]Actor[c:u] just chills out for a bit...");
                return 10;
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

            public int Execute(IEnumerable<IGameClient> clients, GameState gs, Actor actor)
            {
                var dstX = actor.position.x + dx;
                var dstY = actor.position.y + dy;
                if (Map.CanMoveInto(gs.map, gs.actors, dstX, dstY))
                {
                    foreach (var c in clients)
                        c.OnEntityMove(actor, actor.position.x, actor.position.y, dx, dy);

                    (actor.position.x, actor.position.y) = (dstX, dstY);

                    return 20;
                }
                return 0;
            }
        }

        public class TryAttack : IAction
        {
            // TODO: Should probably take an attack ID or something
            public TryAttack() { }

            public int Execute(IEnumerable<IGameClient> clients, GameState gs, Actor actor)
            {
                // Determine attack targets

                // For now, just take all actors surrounding target
                var targets = gs.actors.Where(a =>
                    a != actor &&
                    Math.Abs(a.position.x - actor.position.x) <= 1 &&
                    Math.Abs(a.position.y - actor.position.y) <= 1);

                // Calculate + deal damage to each actor and store result in AttackResults
                var results = new AttackResults(
                    targets.Select(target => {
                        return new AttackResult { Target=target, DamageDealt=3 };
                    }));

                // Emit AttackResult to clients
                foreach (var c in clients)
                    c.OnEntityAttack(actor, results);

                return 50;
            }
        }
    }
}