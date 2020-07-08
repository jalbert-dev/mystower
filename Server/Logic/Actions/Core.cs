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
                // should always set facing regardless of move success
                (actor.facing.x, actor.facing.y) = (dx, dy);
                client.EmitMessage(new Message.ActorFaced(actor, actor.facing));

                var dstX = actor.position.x + dx;
                var dstY = actor.position.y + dy;

                if (Map.CanMoveInto(gs.map, gs.actors, dstX, dstY))
                {

                    client.EmitMessage(new Message.ActorMoved(actor, actor.position.x, actor.position.y, dx, dy));
                    (actor.position.x, actor.position.y) = (dstX, dstY);

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
                client.EmitMessage(new Message.ActorFaced(actor, new Vec2i(dx, dy)));
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

                // For now, find actor in facing adjacent tile
                var targets = gs.actors.Where(a =>
                    a != actor &&
                    a.position.x == actor.position.x + actor.facing.x &&
                    a.position.y == actor.position.y + actor.facing.y);

                // Calculate + deal damage to each actor and store result in AttackResults
                var results = targets.Select(target => {
                    var dmg = DamageHandling.CalcDamage(actor, target);
                    target.status.hp = Math.Max(0, target.status.hp - dmg);

                    return new Message.AttackResult(target) 
                    { 
                        DamageDealt=dmg,
                    };
                });

                // Emit AttackResult to clients
                client.EmitMessage(new Message.ActorAttacked(actor, results));

                foreach (var deadActor in DamageHandling.GetDeadActors(targets).ToList())
                {
                    client.EmitMessage(new Message.ActorDead(deadActor));
                    gs.actors.Remove(deadActor);
                }
                return 50;
            }
        }
    }
}