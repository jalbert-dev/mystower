using System;
using Util;

namespace Client
{
    public interface IResizeHandler
    {
        void OnWindowResize(int width, int height);
    }

    public class StateManager : SadConsole.ScreenObject
    {
        private readonly StateMachine<StateManager> Machine;

        public StateManager(IState<StateManager> startState)
        {
            Machine = new StateMachine<StateManager>(startState);
        }

        private void OnResize(object? sender, EventArgs args)
        {
            var (w, h) = (SadConsole.Game.Instance.MonoGameInstance.WindowWidth, SadConsole.Game.Instance.MonoGameInstance.WindowHeight);
            this.TraverseChildren<IResizeHandler>(handler => handler.OnWindowResize(w, h));
        }

        public void RegisterResizeHandler()
        {
            SadConsole.Game.Instance.MonoGameInstance.WindowResized += OnResize;
        }

        public void UnregisterResizeHandler()
        {
            SadConsole.Game.Instance.MonoGameInstance.WindowResized -= OnResize;
        }

        public override void Update(TimeSpan delta)
        {
            // ? TODO: This is a giant pile of hacks to get around a weird... bug?? regarding ToggleFullScreen. Pay it no mind
            if (SadConsole.Game.Instance.MonoGameInstance.WindowWidth != SadConsole.Settings.Rendering.RenderWidth ||
                SadConsole.Game.Instance.MonoGameInstance.WindowHeight != SadConsole.Settings.Rendering.RenderHeight)
            {
                SadConsole.Settings.Rendering.RenderWidth = SadConsole.Game.Instance.MonoGameInstance.WindowWidth;
                SadConsole.Settings.Rendering.RenderHeight = SadConsole.Game.Instance.MonoGameInstance.WindowHeight;
                SadConsole.Game.Instance.MonoGameInstance.ResetRendering();
            }

            Machine.Exec(this);
            base.Update(delta);
        }
    }
}