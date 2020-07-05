using System;

namespace Util
{
    public interface IAbstractState
    {
        void OnEnter();
        void OnExit();
    }

    public abstract class BaseState<StateMachineType> : IAbstractState
    {
        protected StateMachineType StateMachine { get; }
        public BaseState(StateMachineType host) => StateMachine = host;

        public virtual void OnEnter() { }
        public virtual void OnExit() { }
    }

    public class BaseStateMachine<StateInterface> where StateInterface : class, IAbstractState
    {
        StateInterface? current;

        protected void Do(Action<StateInterface> action) 
        {
            if (current != null)
                action(current);
        }

        private void ProcessPotentialRecursiveState(Action<StateInterface> code)
        {
            if (current != null)
            {
                var oldState = current;
                code(current);
                while (oldState != current)
                {
                    oldState = current;
                    current.OnEnter();
                }
            }
        }
        public void ChangeState(StateInterface newState)
        {
            ProcessPotentialRecursiveState(current => current.OnExit());
            current = newState;
            ProcessPotentialRecursiveState(current => current.OnEnter());
        }
    }
}