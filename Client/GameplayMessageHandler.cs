using Server;
using Server.Message;

namespace Client
{
    public class GameplayMessageHandler : IGameClient
    {
        Consoles.Gameplay Client { get; }
        public GameplayMessageHandler(Consoles.Gameplay client) => this.Client = client;

        public void HandleMessage(EntityAppeared msg)
        {
            Client.MapActors.Add(new MapActor(Client.TileMap, Client.Server, msg.Actor));
        }

        public void HandleMessage(EntityVanished msg)
        {
            Client.MapActors.Remove(msg.Actor);
        }

        public void HandleMessage(EntityMoved msg)
        {
            var vis = Client.MapActors.Lookup(msg.Actor);
            if (vis != null)
                Client.Choreographer.AddMotion(
                    vis,
                    Motions.LerpMove(vis, msg.SourceTile, msg.DestTile, 10),
                    Choreographer.Ordering.Simultaneous);
        }

        public void HandleMessage(MapChanged msg)
        {
            Client.Server.QueryData(msg.NewMapData, Client.TileMap.RebuildTileMap);
        }

        public void HandleMessage(AddedToLog msg)
        {
            Client.MessageLog.AddMessage(msg.MessageId);
        }

        public void HandleMessage(EntityAttacked msg)
        {
            var attacker = Client.MapActors.Lookup(msg.Actor);
            if (attacker != null)
            {
                Client.Choreographer.AddMotion(
                    attacker,
                    Motions.Wiggle(attacker, 20, 1, 10),
                    Choreographer.Ordering.Solo);
            }
            foreach (var a in msg.Results)
            {
                var target = Client.MapActors.Lookup(a.Target);
                if (target != null)
                {
                    Client.Choreographer.AddMotion(
                        target,
                        Motions.Wiggle(target, 60, 4, 8),
                        Choreographer.Ordering.Solo);
                }
                Client.MessageLog.AddMessage($"Actor attacks Actor!");
                Client.MessageLog.AddMessage($"{a.DamageDealt} damage!");
            }
        }
    }
}