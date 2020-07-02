using Util;
using Xunit;
using FluentAssertions;
using System;
using System.Collections;
using FsCheck.Xunit;
using FsCheck;

namespace Server.Tests
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
            yield break;
        }

        static IEnumerable Noop() { yield break; }
        static IEnumerable OneFrameCoroutine() { yield return null; yield break; }

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
    }
}