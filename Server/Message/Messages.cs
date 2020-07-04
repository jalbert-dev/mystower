using System.Collections.Generic;
using System.Linq;
using Server.Data;

namespace Server.Message
{
    public class EntityAppeared : IGameMessage
    {
        public DataHandle<Actor> Actor { get; }

        internal EntityAppeared(Actor actor)
        {
            Actor = actor.ToDataHandle();
        }

        public void Dispatch(IGameClient c) => c.HandleMessage(this);
    }

    public class EntityVanished : IGameMessage
    {
        public DataHandle<Actor> Actor { get; }

        internal EntityVanished(Actor actor)
        {
            Actor = actor.ToDataHandle();
        }

        public void Dispatch(IGameClient c) => c.HandleMessage(this);
    }

    public class EntityMoved : IGameMessage
    {
        public DataHandle<Actor> Actor { get; }
        public Vec2i SourceTile;
        public Vec2i DestTile;

        internal EntityMoved(Actor actor, int sx, int sy, int dx, int dy)
        {
            Actor = actor.ToDataHandle();
            SourceTile.x = sx;
            SourceTile.y = sy;
            DestTile.x = dx;
            DestTile.y = dy;
        }

        public void Dispatch(IGameClient c) => c.HandleMessage(this);
    }

    public class EntityFaced : IGameMessage
    {
        public DataHandle<Actor> Actor { get; }
        public Vec2i NewFacingDir { get; }

        internal EntityFaced(Actor actor, Vec2i facingDir)
        {
            Actor = actor.ToDataHandle();
            NewFacingDir = facingDir;
        }

        public void Dispatch(IGameClient c) => c.HandleMessage(this);
    }

    public class MapChanged : IGameMessage
    {
        public DataHandle<MapData> NewMapData { get; }

        internal MapChanged(MapData newMapData)
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
    public class EntityAttacked : IGameMessage
    {

        public DataHandle<Actor> Actor { get; }
        public AttackResult[] Results { get; }

        internal EntityAttacked(Data.Actor actor, IEnumerable<AttackResult> results)
        {
            Actor = actor.ToDataHandle();
            Results = results.ToArray();
        }

        public void Dispatch(IGameClient c) => c.HandleMessage(this);
    }
}
