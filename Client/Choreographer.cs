using Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Client
{
    public class ChoreographyStep<T> where T : class
    {
        private const int INITIAL_MOTION_LIST_LEN = 4;

        Dictionary<T, List<Coroutine>> motions = new Dictionary<T, List<Coroutine>>();
        List<(T, IEnumerable)> deferred = new List<(T, IEnumerable)>();

        bool deferQueuing = false;

        public bool IsDone => motions.Values.All(x => x.Count == 0);

        public void Clear()
        {
            foreach (var motionList in motions.Values)
                motionList.Clear();
            motions.Clear();
        }

        public void QueueMotion(T target, IEnumerable coroutine)
        {
            if (deferQueuing)
            {
                deferred.Add( (target, coroutine) );
            }
            else
            {
                motions.TryGetValue(target, out var motionList);
                if (motionList == null)
                    motions.Add(target, new List<Coroutine>(INITIAL_MOTION_LIST_LEN));
                motions[target].Add(new Coroutine(coroutine));
            }
        }

        public void Update()
        {
            deferQueuing = true;
            foreach (var motionList in motions.Values)
            {
                var frontMotion = motionList.FirstOrDefault();
                while (frontMotion != null)
                {
                    frontMotion.Step();
                    if (frontMotion.IsDone && motionList.Count > 0)
                    {
                        motionList.RemoveAt(0);
                        frontMotion = motionList.FirstOrDefault();
                    }
                    else
                    {
                        frontMotion = null;
                    }
                }
            }
            deferQueuing = false;
            
            QueueDeferred();
        }

        private void QueueDeferred()
        {
            foreach (var (actor, producer) in deferred)
                QueueMotion(actor, producer);
            deferred.Clear();
        }
    }

    public enum ChoreographyOrder
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

    /// <summary>
    /// A Choreographer coordinates the motion of multiple MapActors.
    /// Individual motions are expressed by IActorMotion instances.
    /// </summary>
    public class Choreographer<T> where T : class
    {
        List<ChoreographyStep<T>> steps = new List<ChoreographyStep<T>>(16);

        /// <summary>
        /// Returns whether the choreographer has motions to execute.
        /// </summary>
        public bool IsBusy => steps.Count != 0;

        public void Update(TimeSpan timeElapsed)
        {
            var currentStep = steps.FirstOrDefault();

            if (currentStep != null)
            {
                currentStep.Update();
                
                // prune finished steps from the list after updating
                if (currentStep.IsDone)
                    steps.RemoveAt(0);
            }
        }

        ChoreographyStep<T> QueueNewStep()
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
            steps.Add(new ChoreographyStep<T>());
            return steps.Last();
        }

        public void AddMotion(T target, Func<ChoreographyStep<T>, IEnumerable> coroutineProducer, ChoreographyOrder ordering)
        {
            if (ordering == ChoreographyOrder.Solo)
            {
                // solo motions force creation of an exclusive step,
                // after which we pad the end with an extra blank step
                // to avoid new motions from adding to the exclusive step

                var soloStep = QueueNewStep();
                soloStep.QueueMotion(target, coroutineProducer(soloStep));
                QueueNewStep();
            }
            else if (ordering == ChoreographyOrder.Simultaneous)
            {
                // simultaneous motions are put into the last step in the queue.
                // if no steps exist, we need to create one

                var lastStep = steps.LastOrDefault();
                if (lastStep != null)
                {
                    lastStep.QueueMotion(target, coroutineProducer(lastStep));
                }
                else
                {
                    var newStep = QueueNewStep();
                    newStep.QueueMotion(target, coroutineProducer(newStep));
                }
            }
        }

        public void AddMotion(T target, IEnumerable coroutine, ChoreographyOrder ordering)
            => AddMotion(target, _ => coroutine, ordering);
    }
}