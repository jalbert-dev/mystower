using Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Client
{
    public class ChoreographyStep
    {
        private const int INITIAL_MOTION_LIST_LEN = 4;

        Dictionary<MapActor, List<Coroutine>> motions = new Dictionary<MapActor, List<Coroutine>>();

        public bool IsDone => motions.Values.All(x => x.Count == 0);

        public void Clear() => motions.Clear();

        public void QueueMotion(MapActor target, Func<MapActor, ChoreographyStep, IEnumerable> coroutineProducer)
        {
            motions.TryGetValue(target, out var motionList);
            if (motionList == null)
                motions.Add(target, new List<Coroutine>(INITIAL_MOTION_LIST_LEN));
            motions[target].Add(new Coroutine(coroutineProducer(target, this)));
        }

        public void Update()
        {
            foreach (var motionList in motions.Values)
            {
                var frontMotion = motionList.FirstOrDefault();
                if (frontMotion != null)
                {
                    frontMotion.Step();
                    if (frontMotion.IsDone)
                        motionList.RemoveAt(0);
                }
            }
        }
    }

    /// <summary>
    /// A Choreographer coordinates the motion of multiple MapActors.
    /// Individual motions are expressed by IActorMotion instances.
    /// </summary>
    public class Choreographer
    {
        List<ChoreographyStep> steps = new List<ChoreographyStep>(16);

        /// <summary>
        /// Returns whether the choreographer has motions to execute.
        /// </summary>
        public bool IsBusy => steps.Count != 0;

        public void PrepareDraw(TimeSpan timeElapsed)
        {
            var currentStep = steps.ElementAtOrDefault(0);

            if (currentStep != null)
            {
                currentStep.Update();
                
                // prune finished steps from the list after updating
                if (currentStep.IsDone)
                    steps.RemoveAt(0);
            }
        }

        public enum Ordering
        {
            /// <summary>
            /// Solo motions are placed into their own exclusive choreography step
            /// at the end of the step queue.
            /// </summary>
            Solo,
            /// <summary>
            /// Simultaneous motions are appended to the step at the end of the step queue.
            /// </summary>
            Simultaneous,
        }

        ChoreographyStep QueueNewStep()
        {
            var lastStep = steps.LastOrDefault();
            // if there exists an empty step at the end of the queue, there's
            // no need to create another; just reuse it
            if (lastStep != null && lastStep.IsDone)
            {
                lastStep.Clear();
                return lastStep;
            }

            // otherwise make and return a new step
            steps.Add(new ChoreographyStep());
            return steps.Last();
        }

        public void AddMotion(MapActor target, Func<MapActor, ChoreographyStep, IEnumerable> coroutineProducer, Ordering ordering)
        {
            if (ordering == Ordering.Solo)
            {
                // solo motions force creation of an exclusive step,
                // after which we pad the end with an extra blank step
                // to avoid new motions from adding to the exclusive step

                QueueNewStep().QueueMotion(target, coroutineProducer);
                QueueNewStep();
            }
            else if (ordering == Ordering.Simultaneous)
            {
                // simultaneous motions are put into the last step in the queue.
                // if no steps exist, we need to create one
                var lastStep = steps.LastOrDefault();
                if (lastStep != null)
                {
                    lastStep.QueueMotion(target, coroutineProducer);
                }
                else
                {
                    QueueNewStep().QueueMotion(target, coroutineProducer);
                }
            }
        }

        public void AddMotion(MapActor target, Func<MapActor, IEnumerable> coroutineProducer, Ordering ordering)
            => AddMotion(target, (actor, _) => coroutineProducer(actor), ordering);

        public void AddMotion(MapActor target, IEnumerable coroutine, Ordering ordering)
            => AddMotion(target, (_, __) => coroutine, ordering);
    }
}