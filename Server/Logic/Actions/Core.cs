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
            public int Execute(IServerProxy server, GameState gs, Actor actor) {
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

            public int Execute(IServerProxy server, GameState gs, Actor actor)
            {
                // should always set facing regardless of move success
                actor.Facing = (dx, dy);
                server.EmitClientMessage(new Message.ActorFaced(actor, actor.Facing));

                var dstX = actor.Position.x + dx;
                var dstY = actor.Position.y + dy;

                if (Map.CanMoveInto(gs.Map, gs.Actors, dstX, dstY))
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
            public int dx { get; }
            public int dy { get; }

            public Face(int dx, int dy) => (this.dx, this.dy) = (dx, dy);

            public int Execute(IServerProxy server, GameState gs, Actor actor)
            {
                actor.Facing = (dx, dy);
                server.EmitClientMessage(new Message.ActorFaced(actor, new Vec2i(dx, dy)));
                return 0;
            }
        }

        public class TryAttack : IAction
        {
            // TODO: Should probably take an attack ID or something
            public TryAttack() { }

            public int Execute(IServerProxy server, GameState gs, Actor actor)
            {
                // Determine attack targets

                // For now, find actor in facing adjacent tile
                var targets = gs.Actors.Where(a =>
                    a != actor &&
                    a.Position.x == actor.Position.x + actor.Facing.x &&
                    a.Position.y == actor.Position.y + actor.Facing.y);

                // Calculate + deal damage to each actor and store result in AttackResults
                var attackerStats = 
                    server.Database
                        .Lookup<ActorArchetype>(actor.ArchetypeId)
                        .Map(archetype => archetype.StatusAtLevel(actor.Level));
                var results = targets.Select(target => {
                    var targetStats = 
                        server.Database
                            .Lookup<ActorArchetype>(target.ArchetypeId)
                            .Map(archetype => archetype.StatusAtLevel(target.Level));

                    var dmgCalc = 
                        from a in attackerStats
                        from t in targetStats
                        select DamageHandling.CalcDamage(a, t);

                    int dmg = 0;
                    if (dmgCalc.IsSuccess)
                        dmg = dmgCalc.Value;
                    else
                        System.Console.WriteLine($"Damage calculation failed: {dmgCalc.Err}");
                    
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