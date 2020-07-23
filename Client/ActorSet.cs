using System.Collections.Generic;
using System.Linq;
using Server;
using Server.Data;

namespace Client
{
    public class ActorSet
    {
        private List<MapActor> MapActors { get; } = new List<MapActor>(64);
        public IEnumerable<MapActor> Actors => MapActors;

        public void Remove(DataHandle<Actor> actor)
        {
            MapActors.RemoveAll(x => {
                var shouldRemove = x.Actor.HandleEquals(actor);
                if (shouldRemove)
                    OnRemoveActor?.Invoke(x);
                return shouldRemove;
            });
        }

        public MapActor? Lookup(DataHandle<Actor> actor)
            => MapActors.FirstOrDefault(x => x.Actor.HandleEquals(actor));

        public void Add(MapActor mapActor)
        {
            if (MapActors.Any(x => x.Actor.HandleEquals(mapActor.Actor)))
                return;
            OnAddActor?.Invoke(mapActor);
            MapActors.Add(mapActor);
        }

        public MapActor? this[DataHandle<Actor> index] => Lookup(index);

        public delegate void ActorSetHandler(MapActor actor);
        public event ActorSetHandler? OnRemoveActor;
        public event ActorSetHandler? OnAddActor;
    }
}