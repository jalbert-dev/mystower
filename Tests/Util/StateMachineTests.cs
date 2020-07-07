using FluentAssertions;
using Xunit;
using U = global::Util;

namespace Tests.Util
{
    public class StateMachineTests
    {
        TestHost Host = new TestHost();

        class TestHost
        {
            public int value;
        }

        class CountTo : U.State<TestHost>
        {
            int n;
            U.IState<TestHost> next;
            public CountTo(int n, U.IState<TestHost> next)
            { 
                this.n = n;
                this.next = next;
            }

            public override U.IState<TestHost>? Exec(TestHost obj)
            {
                if (++obj.value >= n)
                    return next;
                return null;
            }
        }

        class SkipOnEnterTo : U.State<TestHost>
        {
            U.IState<TestHost> next;
            public SkipOnEnterTo(U.IState<TestHost> next)
                => this.next = next;

            public override U.IState<TestHost>? OnEnter(TestHost obj) => next;
            public override U.IState<TestHost>? Exec(TestHost obj)
            {
                throw new System.Exception("SkipOnEnterTo should never be executed!");
            }
        }

        class InterruptOnExit : U.State<TestHost>
        {
            U.IState<TestHost> next;
            U.IState<TestHost> interrupt;
            public InterruptOnExit(U.IState<TestHost> next, U.IState<TestHost> interrupt)
            {
                this.next = next;
                this.interrupt = interrupt;
            }

            public override U.IState<TestHost>? Exec(TestHost obj) => next;
            public override U.IState<TestHost>? OnExit(TestHost obj) => interrupt;
        }

        class DoneState : U.State<TestHost> 
        { 
            public override U.IState<TestHost>? OnEnter(TestHost obj)
            {
                obj.value = -1;
                return null;
            }
        }

        [Fact] public void IsInStateReportsCurrentStateType()
        {
            var fsm = new U.StateMachine<TestHost>(new DoneState());

            fsm.IsInState<CountTo>().Should().BeFalse();
            fsm.IsInState<DoneState>().Should().BeTrue();
            fsm.IsInState(typeof(CountTo)).Should().BeFalse();
            fsm.IsInState(typeof(DoneState)).Should().BeTrue();

            fsm = new U.StateMachine<TestHost>(new CountTo(5, new DoneState()));

            fsm.IsInState<CountTo>().Should().BeTrue();
            fsm.IsInState<DoneState>().Should().BeFalse();
            fsm.IsInState(typeof(CountTo)).Should().BeTrue();
            fsm.IsInState(typeof(DoneState)).Should().BeFalse();
        }

        [Fact] public void ExecRunsFSMOneTick()
        {
            var fsm = new U.StateMachine<TestHost>(new CountTo(3, new DoneState()));

            fsm.Exec(Host);
            Host.value.Should().Be(1);

            fsm.Exec(Host);
            Host.value.Should().Be(2);

            fsm.IsInState<CountTo>().Should().BeTrue();
        }

        [Fact] public void ExecImmediatelyEntersNextStateOnTransition()
        {
            var fsm = new U.StateMachine<TestHost>(new CountTo(2, new DoneState()));

            fsm.Exec(Host);
            fsm.Exec(Host);

            Host.value.Should().Be(-1);

            fsm.IsInState<DoneState>().Should().BeTrue();
        }

        [Fact] public void OnEnterTransitionsOccurImmediately()
        {
            var fsm = new U.StateMachine<TestHost>(new SkipOnEnterTo(new DoneState()));
            fsm.Exec(Host);

            fsm.IsInState<DoneState>().Should().BeTrue();
        }

        [Fact] public void OnEnterTransitionsAreChainableInOneStep()
        {
            var fsm = new U.StateMachine<TestHost>(
                new SkipOnEnterTo(
                    new SkipOnEnterTo(
                        new SkipOnEnterTo(
                            new SkipOnEnterTo(
                                new DoneState())))));
            fsm.Exec(Host);

            fsm.IsInState<DoneState>().Should().BeTrue();
        }

        [Fact] public void OnExitTransitionsInterruptExistingTransition()
        {
            var fsm = new U.StateMachine<TestHost>(
                new InterruptOnExit(
                    new CountTo(9999, new DoneState()),
                    new DoneState()));
            fsm.Exec(Host);

            fsm.IsInState<DoneState>().Should().BeTrue();
        }

        [Fact] public void OnExitTransitionsAreChainableWithOnEnterInOneStep()
        {
            var fsm = new U.StateMachine<TestHost>(
                new InterruptOnExit(
                    new CountTo(9999, new DoneState()),
                    new SkipOnEnterTo(
                        new DoneState())));

            fsm.Exec(Host);

            fsm.IsInState<DoneState>().Should().BeTrue();
        }
    }
}