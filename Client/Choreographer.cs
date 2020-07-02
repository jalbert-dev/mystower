using System;
using System.Collections.Generic;
using System.Linq;
using SadRogue.Primitives;

namespace Client
{
    /// <summary>
    /// A Choreographer coordinates the motion of multiple MapActors.
    /// Individual motions are expressed by IActorMotion instances.
    /// </summary>
    public class Choreographer
    {
        List<IActorMotion> motions = new List<IActorMotion>(32);
        IEnumerable<IActorMotion> ActiveMotions => motions.Where(x => !x.IsFinished);
        /// <summary>
        /// A list of all motions up to the first global-sequential motion.
        /// </summary>
        IEnumerable<IActorMotion> ActiveMotionsThisStep
        {
            get
            {
                if (ActiveMotions.Count() == 0)
                    yield break;

                var firstMotion = ActiveMotions.First();
                yield return firstMotion;
                
                if (!firstMotion.IsGlobalSequential)
                    foreach (var eff in ActiveMotions.Skip(1).TakeWhile(x => !x.IsGlobalSequential))
                        yield return eff;
                yield break;
            }
        }

        /// <summary>
        /// Given an enumerable of active motions, filters it by actor and returns
        /// an enumerable of all motions up to the first actor-sequential motion.
        /// </summary>
        static IEnumerable<IActorMotion> ActiveLocalMotions(MapActor actor, IEnumerable<IActorMotion> activeGlobalMotions)
        {
            var actorMotions = activeGlobalMotions.Where(x => x.MapActor == actor);

            if (actorMotions.Count() == 0)
                yield break;

            var first = actorMotions.First();
            yield return first;

            if (!first.IsActorSequential)
                foreach (var mot in actorMotions.Skip(1).TakeWhile(x => !x.IsActorSequential))
                    yield return mot;
            yield break;
        }

        /// <summary>
        /// Returns whether the choreographer has motions to execute.
        /// </summary>
        public bool IsBusy => motions.Count != 0;

        public void PrepareDraw(IEnumerable<MapActor> actors, TimeSpan timeElapsed)
        {
            // Globally-sequential motions can't be played until all motions
            // before it have finished, so if a global-sequential motion exists
            // and isn't at the front of the queue, process all motions up to it
            var activeMotions = ActiveMotionsThisStep;
            
            foreach (var actor in actors)
            {
                // Actors have their position offsets reset each frame to allow
                // multiple motions to sum their individual offsets.
                actor.PositionOffset = default(Point);

                // Actor-sequential motions must be played in order per-actor,
                // so we'll apply motions up to the first actor-sequential
                // motion, or the first motion if it is actor-sequential.
                foreach (var mot in ActiveLocalMotions(actor, activeMotions))
                {
                    mot.Apply(timeElapsed);
                }
            }
            
            // prune finished motions from the list after updating
            motions.RemoveAll(x => x.IsFinished);
        }

        public void AddMotion(IActorMotion instance) => motions.Add(instance);
    }
}