using System;
using System.Collections.Generic;
using System.Linq;
using SadRogue.Primitives;

namespace Client
{
    public class Choreographer
    {
        List<IChoreography> effects = new List<IChoreography>(32);
        IEnumerable<IChoreography> ActiveEffects => effects.Where(x => !x.IsDone);
        /// <summary>
        /// A list of all effects up to the first globally blocking effect.
        /// </summary>
        IEnumerable<IChoreography> ActiveEffectsThisStep
        {
            get
            {
                if (ActiveEffects.Count() == 0)
                    yield break;

                var firstEffect = ActiveEffects.First();
                yield return firstEffect;
                
                if (!firstEffect.IsGlobalSolo)
                    foreach (var eff in ActiveEffects.Skip(1).TakeWhile(x => !x.IsGlobalSolo))
                        yield return eff;
                yield break;
            }
        }

        /// <summary>
        /// Given an enumerable of active effects, filters it by actor and returns
        /// an enumerable of all effects up to the first local blocking effect.
        /// </summary>
        static IEnumerable<IChoreography> ActiveLocalEffects(MapActor actor, IEnumerable<IChoreography> activeGlobalEffects)
        {
            var actorEffects = activeGlobalEffects.Where(x => x.MapActor == actor);

            if (actorEffects.Count() == 0)
                yield break;

            var first = actorEffects.First();
            yield return first;

            if (!first.IsLocalSolo)
                foreach (var eff in actorEffects.Skip(1).TakeWhile(x => !x.IsLocalSolo))
                    yield return eff;
            yield break;
        }

        public bool Busy => effects.Count != 0;

        public void PrepareDraw(IEnumerable<MapActor> actors, TimeSpan timeElapsed)
        {
            var activeEffects = ActiveEffectsThisStep;
            foreach (var actor in actors)
            {
                actor.PositionOffset = default(Point);
                foreach (var eff in ActiveLocalEffects(actor, activeEffects))
                {
                    eff.Apply(timeElapsed);
                }
            }
            
            // prune finished effects from the list after updating
            effects.RemoveAll(x => x.IsDone);
        }

        public void AddEffect(IChoreography instance) => effects.Add(instance);
    }
}