using System;
using System.Collections.Generic;
using System.Linq;
using Server.Data;
using Server.Database;

namespace Server.Logic
{
    public static partial class Actions
    {
        public class Idle : IAction
        {
            public int Execute(IServerContext server, GameState gs, Actor actor) {
                return 10;
            }
        }

        public class Move : IAction
        {
            public Move(int dx, int dy) => (this.dx, this.dy) = (dx, dy);
            public Move(Direction direction) => (dx, dy) = direction.ToVec();

            public readonly int dx;
            public readonly int dy;

            public int Execute(IServerContext server, GameState gs, Actor actor)
            {
                // should always set facing regardless of move success
                actor.Facing = (dx, dy).ToClosestDirection();
                server.EmitClientMessage(new Message.ActorFaced(actor, actor.Facing));

                var dstX = actor.Position.x + dx;
                var dstY = actor.Position.y + dy;

                if (Map.CanMoveFromAToB(gs.Map, gs.Actors, actor.Position, (dstX, dstY)))
                {

                    server.EmitClientMessage(new Message.ActorMoved(actor, actor.Position.x, actor.Position.y, dx, dy));
                    actor.Position = (dstX, dstY);

                    return 20;
                }
                return 0;
            }
        }

        public class Face : IAction
        {
            public readonly Direction direction;

            public Face(Direction direction) => this.direction = direction;

            public int Execute(IServerContext server, GameState gs, Actor actor)
            {
                actor.Facing = direction;
                server.EmitClientMessage(new Message.ActorFaced(actor, direction));
                return 0;
            }
        }

        public class TryAttack : IAction
        {
            // TODO: Should probably take an attack ID or something
            public TryAttack() { }

            public int Execute(IServerContext server, GameState gs, Actor actor)
            {
                // Determine attack targets

                // For now, find actor in facing adjacent tile
                var facingVector = actor.Facing.ToVec();
                var targets = gs.Actors.Where(a =>
                    a != actor &&
                    a.Position.x == actor.Position.x + facingVector.x &&
                    a.Position.y == actor.Position.y + facingVector.y &&
                    !Logic.Map.IsMoveBlockedByDiagonalWall(gs.Map, actor.Position, a.Position));

                // Calculate + deal damage to each actor and store result in AttackResults
                var attackerStats = actor.Archetype.StatusAtLevel(actor.Level);
                var results = targets.Select(target => {
                    var targetStats = actor.Archetype.StatusAtLevel(target.Level);

                    var dmg = DamageHandling.CalcDamage(attackerStats, targetStats);
                    
                    target.Status.Hp = target.Status.Hp - dmg;

                    return new Message.AttackResult(target) 
                    { 
                        DamageDealt=dmg,
                    };
                });

                // Emit AttackResult to clients
                server.EmitClientMessage(new Message.ActorAttacked(actor, results));

                foreach (var deadActor in DamageHandling.GetDeadActors(targets).ToList())
                {
                    server.EmitClientMessage(new Message.ActorDead(deadActor));
                    gs.Actors.Remove(deadActor);
                }
                return 50;
            }
        }
    }
}