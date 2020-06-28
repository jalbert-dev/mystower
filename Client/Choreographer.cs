using System;
using System.Collections.Generic;
using System.Linq;

namespace Client
{
    public class Choreographer
    {
        List<IChoreography> effects = new List<IChoreography>(32);
        IEnumerable<IChoreography> ActiveEffects => effects.Where(x => !x.IsDone);
        IEnumerable<IChoreography> ActiveEffectsThisStep
        {
            get
            {
                if (ActiveEffects.Count() == 0)
                    yield break;

                var firstEffect = ActiveEffects.First();
                yield return firstEffect;
                
                if (!firstEffect.IsSolo)
                {
                    foreach (var eff in ActiveEffects.Skip(1).TakeWhile(x => !x.IsSolo))
                        yield return eff;
                }
                yield break;
            }
        }

        public bool Busy => effects.Count != 0;

        public void PrepareDraw(IEnumerable<MapActor> actors, TimeSpan timeElapsed)
        {
            var activeEffects = ActiveEffectsThisStep;
            foreach (var actor in actors)
            {
                foreach (var eff in activeEffects.Where(x => x.MapActor == actor))
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