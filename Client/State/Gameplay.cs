using Server;
using Util;

namespace Client.State
{
    public class Gameplay : IState<StateManager>
    {
        public Consoles.Gameplay GameplayConsole { get; }

        public Gameplay(IClientContext ctx, GameServer s) => GameplayConsole = new Consoles.Gameplay(ctx, s);

        public IState<StateManager>? OnEnter(StateManager obj)
        {
            obj.Children.Add(GameplayConsole);
            GameplayConsole.IsFocused = true;
            return null;
        }

        public IState<StateManager>? Exec(StateManager obj)
        {
            if (GameplayConsole.ShouldReturnToTitle)
                return new State.TitleScreen();
            return null;
        }

        public IState<StateManager>? OnExit(StateManager obj)
        {
            obj.Children.Remove(GameplayConsole);
            return null;
        }
    }
}