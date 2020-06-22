namespace Server
{
    public interface IGameClient
    {
        void OnEntityAppear(Data.Actor actor);
        void OnEntityVanish(Data.Actor actor);
        void OnEntityMove(Data.Actor actor, int dx, int dy);
        
        void OnMapChange(Data.MapData newMapData);
    }
}