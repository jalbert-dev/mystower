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
            var a = new MapActor(Client.TileMap, msg.Actor);
            a.Sync(Client.Server);
            Client.MapActors.Add(a);
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

        public void HandleMessage(EntityFaced msg)
        {
            var mapActor = Client.MapActors.Lookup(msg.Actor);
            if (mapActor != null)
                mapActor.Facing = msg.NewFacingDir;
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
                    (actor, step) => Motions.Lunge(attacker, 4, 6, 0.33f, 
                        () => {
                            foreach (var a in msg.Results)
                            {
                                var target = Client.MapActors.Lookup(a.Target);
                                if (target != null)
                                    step.QueueMotion(target, Motions.Wiggle(target, 20, 4, 8));
                                Client.MessageLog.AddMessage($"{a.DamageDealt} damage!");
                            }
                        }),
                    Choreographer.Ordering.Solo);
            }
            foreach (var a in msg.Results)
            {
                Client.MessageLog.AddMessage($"Actor attacks Actor!");
            }
        }
    }
}