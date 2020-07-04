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
            public int Execute(IClientProxy client, GameState gs, Actor actor) {
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

            public int Execute(IClientProxy client, GameState gs, Actor actor)
            {
                var dstX = actor.position.x + dx;
                var dstY = actor.position.y + dy;
                if (Map.CanMoveInto(gs.map, gs.actors, dstX, dstY))
                {
                    client.EmitMessage(new Message.EntityMoved(actor, actor.position.x, actor.position.y, dx, dy));
                    (actor.position.x, actor.position.y) = (dstX, dstY);

                    (actor.facing.x, actor.facing.y) = (dx, dy);
                    client.EmitMessage(new Message.EntityFaced(actor, actor.facing));

                    return 20;
                }
                return 0;
            }
        }

        public class Face : IAction
        {
            public int dx { get; }
            public int dy { get; }

            public Face(int dx, int dy) => (this.dx, this.dy) = (dx, dy);

            public int Execute(IClientProxy client, GameState gs, Actor actor)
            {
                (actor.facing.x, actor.facing.y) = (dx, dy);
                client.EmitMessage(new Message.EntityFaced(actor, new Vec2i(dx, dy)));
                return 0;
            }
        }

        public class TryAttack : IAction
        {
            // TODO: Should probably take an attack ID or something
            public TryAttack() { }

            public int Execute(IClientProxy client, GameState gs, Actor actor)
            {
                // Determine attack targets

                // For now, just take all actors surrounding target
                var targets = gs.actors.Where(a =>
                    a != actor &&
                    Math.Abs(a.position.x - actor.position.x) <= 1 &&
                    Math.Abs(a.position.y - actor.position.y) <= 1);

                // Calculate + deal damage to each actor and store result in AttackResults
                var results = targets.Select(target => new Message.AttackResult(target) 
                { 
                    DamageDealt=3,
                });

                // Emit AttackResult to clients
                client.EmitMessage(new Message.EntityAttacked(actor, results));

                return 50;
            }
        }
    }
}