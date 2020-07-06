using System.Collections;
using Xunit;
using Client;
using FluentAssertions;
using System;

namespace Tests.Client
{
    public class ChoreographyStepTests
    {
        public class TestActor
        {
            public int position = 0;
        }
        ChoreographyStep<TestActor> Step = new ChoreographyStep<TestActor>();

        static IEnumerable YieldInstantly(Action? action = null) 
        {
            if (action != null)
                action();
            yield break;
        }
        static IEnumerable ForNUpdatesDo(int count = 1, Action? action = null)
        {
            for (int i = 0; i < count; i++)
            {
                if (action != null)
                    action();
                yield return null;
            }
        }

        [Fact] public void UpdatingEmptyStepDoesntBreak() => Step.Update();

        [Fact] public void IsDoneIfEmpty() => Step.IsDone.Should().BeTrue();

        [Fact] public void IsNotDoneIfHasQueuedWork()
        {
            Step.QueueMotion(new TestActor(), YieldInstantly());
            Step.IsDone.Should().BeFalse();
        }

        [Fact] public void ClearRemovesQueuedWork()
        {
            var actor = new TestActor();

            Step.QueueMotion(actor, YieldInstantly(() => actor.position = 17));
            Step.Clear();
            
            Step.IsDone.Should().BeTrue();
            actor.position.Should().Be(default(int));
        }

        [Fact] public void UpdatePerformsQueuedWork()
        {
            var actor = new TestActor();
            Step.QueueMotion(actor, ForNUpdatesDo(1, () => actor.position++));
            Step.Update();
            actor.position.Should().Be(1);            
        }

        [Fact] public void UpdatePerformsOneYieldOfWork()
        {
            var actor = new TestActor();
            Step.QueueMotion(actor, ForNUpdatesDo(5, () => actor.position++));

            Step.Update();
            actor.position.Should().Be(1);
            Step.IsDone.Should().BeFalse();
            Step.Update();
            actor.position.Should().Be(2);
            Step.IsDone.Should().BeFalse();
        }

        [Fact] public void CompletedWorkIsDestroyed()
        {
            Step.QueueMotion(new TestActor(), YieldInstantly());
            Step.Update();
            Step.IsDone.Should().BeTrue();
        }

        [Fact] public void WorkForDifferentActorsIsExecutedInParallel()
        {
            var a = new TestActor();
            var b = new TestActor();

            Step.QueueMotion(a, YieldInstantly(() => a.position = 2));
            Step.QueueMotion(b, YieldInstantly(() => b.position = 3));
            Step.Update();

            a.position.Should().Be(2);
            b.position.Should().Be(3);
            Step.IsDone.Should().BeTrue();
        }

        [Fact] public void IsNotDoneUntilAllActorsWorkIsFinished()
        {
            var a = new TestActor();
            var b = new TestActor();

            Step.QueueMotion(a, YieldInstantly());
            Step.QueueMotion(a, ForNUpdatesDo(2));
            Step.QueueMotion(b, YieldInstantly());
            Step.Update();
            
            Step.IsDone.Should().BeFalse();

            Step.Update();
            Step.Update();
            Step.Update();
            Step.IsDone.Should().BeTrue();
        }

        [Fact] public void AfterCompletingWorkImmediatelyBeginsNextInQueue()
        {
            var actor = new TestActor();
            Step.QueueMotion(actor, YieldInstantly());
            Step.QueueMotion(actor, YieldInstantly(() => actor.position = 5));
            Step.QueueMotion(actor, YieldInstantly(() => actor.position--));
            Step.Update();

            Step.IsDone.Should().BeTrue();
            actor.position.Should().Be(4);
        }

        [Fact] public void WorkCanAddOtherWorkDuringExecution()
        {
            var actor = new TestActor();
            Step.QueueMotion(actor, (actor, step) => YieldInstantly(() => {
                step.QueueMotion(actor, YieldInstantly(() => actor.position = 7));
            }));
            Step.Update();
            Step.Update();

            actor.position.Should().Be(7);
            Step.IsDone.Should().BeTrue();
        }

        [Fact] public void WorkCanClearWorkQueueDuringExecution()
        {
            var actor = new TestActor();
            Step.QueueMotion(actor, (actor, step) => YieldInstantly(() => step.Clear()));
            Step.QueueMotion(actor, YieldInstantly(() => actor.position = 20));

            FluentActions.Invoking(() => Step.Update()).Should().NotThrow(
                "because queued work should be able to clear the queue during execution");
            Step.IsDone.Should().BeTrue();
            actor.position.Should().Be(default(int));
        }
    }
}