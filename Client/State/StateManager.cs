using System;
using Util;

namespace Client
{
    public interface IResizeHandler
    {
        void OnWindowResize(int width, int height);
    }

    public interface IExecutableState : IAbstractState
    {
        void OnExec();
    }

    public class GlobalScreenFSM : Util.BaseStateMachine<IExecutableState>
    {
        public StateManager ScreenManager { get; }
        public GlobalScreenFSM(StateManager screenManager) => ScreenManager = screenManager;

        public void Exec() => Do(x => x.OnExec());
    }

    public class StateManager : SadConsole.ScreenObject
    {
        GlobalScreenFSM Machine;

        public StateManager(Func<GlobalScreenFSM, IExecutableState> startStateProducer)
        {
            Machine = new GlobalScreenFSM(this);
            Machine.ChangeState(startStateProducer(Machine));
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

            Machine.Exec();
            base.Update(delta);
        }
    }
}