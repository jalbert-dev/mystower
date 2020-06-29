using System;

namespace Client
{
    public class StateManager : SadConsole.ScreenObject
    {
        StateMachine<StateManager> Machine;

        public StateManager(IState<StateManager> startState)
        {
            Machine = new StateMachine<StateManager>(startState);
        }

        public override void Update(TimeSpan delta)
        {
            Machine.Exec(this);
            base.Update(delta);
        }
    }
}