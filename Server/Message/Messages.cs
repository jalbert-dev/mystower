using System.Collections.Generic;
using System.Linq;
using Server.Data;

namespace Server.Message
{
    public class EntityAppeared : IGameMessage
    {
        public Data.Actor Actor { get; }

        public EntityAppeared(Actor actor)
        {
            Actor = actor;
        }

        public void Dispatch(IGameClient c) => c.HandleMessage(this);
    }

    public class EntityVanished : IGameMessage
    {
        public Data.Actor Actor { get; }

        public EntityVanished(Actor actor)
        {
            Actor = actor;
        }

        public void Dispatch(IGameClient c) => c.HandleMessage(this);
    }

    public class EntityMoved : IGameMessage
    {
        public Data.Actor Actor { get; }
        public Vec2i SourceTile;
        public Vec2i DestTile;

        public EntityMoved(Actor actor, int sx, int sy, int dx, int dy)
        {
            Actor = actor;
            SourceTile.x = sx;
            SourceTile.y = sy;
            DestTile.x = dx;
            DestTile.y = dy;
        }

        public void Dispatch(IGameClient c) => c.HandleMessage(this);
    }

    public class MapChanged : IGameMessage
    {
        public Data.MapData NewMapData { get; }

        public MapChanged(MapData newMapData)
        {
            NewMapData = newMapData;
        }

        public void Dispatch(IGameClient c) => c.HandleMessage(this);
    }

    public class AddedToLog : IGameMessage
    {
        public string MessageId { get; }

        public AddedToLog(string messageId)
        {
            MessageId = messageId;
        }

        public void Dispatch(IGameClient c) => c.HandleMessage(this);
    }

    public class EntityAttacked : IGameMessage
    {
        public Data.Actor Actor { get; }
        public Data.AttackResult[] Results { get; }

        public EntityAttacked(Data.Actor actor, IEnumerable<Data.AttackResult> results)
        {
            Actor = actor;
            Results = results.ToArray();
        }

        public void Dispatch(IGameClient c) => c.HandleMessage(this);
    }
}
