namespace Server
{
    public interface IGameClient
    {
        // Invoked when an entity that should be known to the client appears,
        // whether due to spawning or for some other reason.
        void OnEntityAppear(Data.Actor actor);
        // Invoked when an entity known to the client disappears,
        // whether due to death or for some other reason.
        void OnEntityVanish(Data.Actor actor);
        // Invoked when an entity known to the client moves.
        void OnEntityMove(Data.Actor actor, int sx, int sy, int dx, int dy);
        
        // Invoked when the server sends new map data to the client.
        void OnMapChange(Data.MapData newMapData);

        // Invoked when the server wants the client to display a message by the
        // given ID in the message log.
        void OnAddLogMessage(string messageId);

        void OnEntityAttack(Data.Actor actor, Data.AttackResults result);
    }
}