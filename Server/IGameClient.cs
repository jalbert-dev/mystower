namespace Server
{
    public interface IGameClient
    {
        /// <summary>
        /// Invoked when an entity that should be known to the client appears,
        /// whether due to spawning or for some other reason.
        /// </summary>
        void OnEntityAppear(Data.Actor actor);
        /// <summary>
        /// Invoked when an entity known to the client disappears,
        /// whether due to death or for some other reason.
        /// </summary>
        void OnEntityVanish(Data.Actor actor);
        /// <summary>
        /// Invoked when an entity known to the client moves from one tile
        /// to another.
        /// </summary>
        /// <param name="sx">Source tile X.</param>
        /// <param name="sy">Source tile Y.</param>
        /// <param name="dx">Dest tile X.</param>
        /// <param name="dy">Dest tile Y.</param>
        void OnEntityMove(Data.Actor actor, int sx, int sy, int dx, int dy);
        
        /// <summary>
        /// Invoked when the server sends new map data to the client.
        /// </summary>
        void OnMapChange(Data.MapData newMapData);

        /// <summary>
        /// Invoked when the server wants the client to display a message by the
        /// given ID in the message log.
        /// </summary>
        void OnAddLogMessage(string messageId);

        /// <summary>
        /// Invoked when the server wants the client to display the results of
        /// an actor's attack.
        /// </summary>
        /// <param name="actor">The attacking actor.</param>
        /// <param name="result">The table of results of the attack.</param>
        void OnEntityAttack(Data.Actor actor, Data.AttackResults result);
    }
}