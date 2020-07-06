using Util;
using Xunit;
using FluentAssertions;
using System;
using System.Collections;
using FsCheck.Xunit;
using FsCheck;

namespace Tests.Server
{
    public class CoroutineTests
    {
        static IEnumerable Counter(int limit, Action<int> callback)
        {
            for (int i = 0; i < limit; i++)
            {
                callback(i);
                yield return null;
            }
        }

        static IEnumerable Noop() { yield break; }
        static IEnumerable OneFrameCoroutine() { yield return null; }
        static IEnumerable Nested(IEnumerable nest) { yield return nest; }

        [Property] public Property StepExecutesCoroutineBody()
            => Prop.ForAll(Gen.Choose(0, 50).ToArbitrary(), limit => {
                int v = -1;
                var co = new Coroutine(Counter(limit, x => v = x));
                v.Should().Be(-1);
                for (int i = 0; i < limit; i++)
                {
                    co.Step();
                    v.Should().Be(i);
                    co.IsDone.Should().BeFalse();
                }
                co.Step();
                v.Should().Be(limit - 1);
                return co.IsDone;
            });

        [Fact] public void SteppingFinishedCoroutineDoesNothing()
        {
            var co = new Coroutine(Noop());
            for (int i = 0; i < 999; i++)
                co.Step();
            co.IsDone.Should().BeTrue();
        }

        [Fact] public void IsDoneIsFalseAfterCoroutineConstruction()
            => new Coroutine(Noop()).IsDone.Should().BeFalse();

        [Fact] public void IsDoneIsFalseAfterStepIfCoroutineBodyNotDone()
        {
            var co = new Coroutine(OneFrameCoroutine());
            co.Step();
            co.IsDone.Should().BeFalse();
        }

        [Fact] public void IsDoneIsTrueWhenSteppingThroughYieldBreak()
        {
            var co = new Coroutine(Noop());
            co.Step();
            co.IsDone.Should().BeTrue();
        }

        [Fact] public void YieldingNestedEnumerableExecutesImmediately()
        {
            int result = -1;
            var co = new Coroutine(Nested(Counter(10, x => result = x)));
            co.Step();
            result.Should().Be(0);
        }

        [Fact] public void WhenNestedEnumerableFinishesParentResumesImmediately()
        {
            static IEnumerable SetAfterOneFrame<T>(T value, Action<T> callback)
            {
                yield return OneFrameCoroutine();
                callback(value);
            }

            int result = -1;
            var co = new Coroutine(SetAfterOneFrame(24, x => result = x));
            co.Step();
            result.Should().Be(-1);
            co.Step();
            result.Should().Be(24);
            co.IsDone.Should().BeTrue();
        }

        [Property] public Property NestedImmediateCoroutinesAllExecuteInOneStep()
            => Prop.ForAll(Gen.Choose(1, 24).ToArbitrary(), nestingLevels => {
                int node = 0;
                int leaf = 0;

                IEnumerable Node(IEnumerable next)
                {
                    node++;
                    yield return next;
                }
                IEnumerable Leaf()
                {
                    leaf++;
                    yield break;
                }

                IEnumerable en = Leaf();
                for (int i = 0; i < nestingLevels; i++)
                    en = Node(en);

                var co = new Coroutine(en);
                co.Step();

                node.Should().Be(nestingLevels);
                leaf.Should().Be(1);
                co.IsDone.Should().BeTrue();
            });
    }
}