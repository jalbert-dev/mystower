using Server;
using Util;

namespace Client.State
{
    public class Gameplay : BaseState<GlobalScreenFSM>, IExecutableState
    {
        public Consoles.Gameplay GameplayConsole { get; }

        public Gameplay(GlobalScreenFSM fsm, GameServer s) : base(fsm)
        {
            GameplayConsole = new Consoles.Gameplay(s);
        }

        public void OnExec()
        {
            if (GameplayConsole.ShouldReturnToTitle)
                StateMachine.ChangeState(new TitleScreen(StateMachine));
        }

        public override void OnEnter()
        {
            StateMachine.ScreenManager.Children.Add(GameplayConsole);
            GameplayConsole.IsFocused = true;
        }

        public override void OnExit()
        {
            StateMachine.ScreenManager.Children.Remove(GameplayConsole);
        }
    }
}