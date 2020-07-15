using System.Collections.Generic;
using System.Linq;
using Server.Data;

namespace Server.Message
{
    public class ActorAppeared : IGameMessage
    {
        public DataHandle<Actor> Actor { get; }

        internal ActorAppeared(Actor actor)
        {
            Actor = actor.ToDataHandle();
        }

        public void Dispatch(IGameClient c) => c.HandleMessage(this);
    }

    public class ActorDead : IGameMessage
    {
        public DataHandle<Actor> Actor { get; }

        internal ActorDead(Actor actor)
        {
            Actor = actor.ToDataHandle();
        }

        public void Dispatch(IGameClient c) => c.HandleMessage(this);
    }

    public class ActorMoved : IGameMessage
    {
        public DataHandle<Actor> Actor { get; }
        public Vec2i SourceTile;
        public Vec2i DestTile;

        internal ActorMoved(Actor actor, int sx, int sy, int dx, int dy)
        {
            Actor = actor.ToDataHandle();
            SourceTile.x = sx;
            SourceTile.y = sy;
            DestTile.x = dx;
            DestTile.y = dy;
        }

        public void Dispatch(IGameClient c) => c.HandleMessage(this);
    }

    public class ActorFaced : IGameMessage
    {
        public DataHandle<Actor> Actor { get; }
        public Vec2i NewFacingDir { get; }

        internal ActorFaced(Actor actor, Vec2i facingDir)
        {
            Actor = actor.ToDataHandle();
            NewFacingDir = facingDir;
        }

        public void Dispatch(IGameClient c) => c.HandleMessage(this);
    }

    public class MapChanged : IGameMessage
    {
        public DataHandle<TileMap<byte>> NewMapData { get; }

        internal MapChanged(TileMap<byte> newMapData)
        {
            NewMapData = newMapData.ToDataHandle();
        }

        public void Dispatch(IGameClient c) => c.HandleMessage(this);
    }

    public class AddedToLog : IGameMessage
    {
        public string MessageId { get; }

        internal AddedToLog(string messageId)
        {
            MessageId = messageId;
        }

        public void Dispatch(IGameClient c) => c.HandleMessage(this);
    }

    public struct AttackResult
    {
        public DataHandle<Actor> Target;
        public int DamageDealt;

        public AttackResult(Actor target)
        {
            Target = target.ToDataHandle();
            DamageDealt = 0;
        }
    }
    public class ActorAttacked : IGameMessage
    {

        public DataHandle<Actor> Actor { get; }
        public AttackResult[] Results { get; }

        internal ActorAttacked(Data.Actor actor, IEnumerable<AttackResult> results)
        {
            Actor = actor.ToDataHandle();
            Results = results.ToArray();
        }

        public void Dispatch(IGameClient c) => c.HandleMessage(this);
    }
}
