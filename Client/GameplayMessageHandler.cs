using Server;
using Server.Message;

namespace Client
{
    public class GameplayMessageHandler : IGameClient
    {
        Consoles.Gameplay Client { get; }
        public GameplayMessageHandler(Consoles.Gameplay client) => this.Client = client;

        public void HandleMessage(ActorAppeared msg)
        {
            var a = new MapActor(msg.Actor);
            a.Sync(Client.ClientContext, Client.Server);
            Client.MapActors.Add(a);

            Client.UpdateActorsOnMinimap();
        }

        public void HandleMessage(ActorDead msg)
        {
            var mapActor = Client.MapActors.Lookup(msg.Actor);
            if (mapActor != null)
                Client.Choreographer.AddMotion(
                    mapActor,
                    Motions.Death(Client.MessageLog, mapActor, () => Client.MapActors.Remove(msg.Actor)),
                    ChoreographyOrder.Solo);
        }

        public void HandleMessage(ActorMoved msg)
        {
            var vis = Client.MapActors.Lookup(msg.Actor);
            if (vis != null)
                Client.Choreographer.AddMotion(
                    vis,
                    Motions.LerpMove(Client.MapTileSize, vis, msg.SourceTile, msg.DestTile, 10),
                    ChoreographyOrder.Simultaneous);
        }

        public void HandleMessage(ActorFaced msg)
        {
            var mapActor = Client.MapActors.Lookup(msg.Actor);
            if (mapActor != null)
            {
                Client.Choreographer.AddMotion(
                    mapActor, 
                    Motions.SetFacing(mapActor, msg.NewFacingDir),
                    ChoreographyOrder.Simultaneous);
            }
        }

        public void HandleMessage(MapChanged msg)
        {
            Client.Server.QueryData(msg.NewMapData, Client.UpdateMapTerrain);
        }

        public void HandleMessage(AddedToLog msg)
        {
            Client.MessageLog.AddMessage(msg.MessageId);
        }

        public void HandleMessage(ActorAttacked msg)
        {
            var attacker = Client.MapActors.Lookup(msg.Actor);
            if (attacker != null)
            {
                Client.Choreographer.AddMotion(
                    attacker,
                    step => Motions.Lunge(Client.MapTileSize, attacker, 4, 6, 0.33f, 
                        () => {
                            foreach (var a in msg.Results)
                            {
                                var target = Client.MapActors.Lookup(a.Target);
                                if (target != null)
                                    Client.MessageLog.AddMessage($"{attacker.DisplayName} attacks {target.DisplayName}!");
                            }
                        },
                        () => {
                            foreach (var a in msg.Results)
                            {
                                var target = Client.MapActors.Lookup(a.Target);
                                if (target != null)
                                    step.QueueMotion(target, Motions.Wiggle(target, 20, 4, 8));
                                Client.MessageLog.AddMessage($"{a.DamageDealt} damage!");
                            }
                        }),
                    ChoreographyOrder.Solo);
            }
        }
    }
}