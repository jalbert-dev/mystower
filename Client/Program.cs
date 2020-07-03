using System;
using SadConsole.Input;

namespace Client
{
    class Program
    {
        public static int GameSizeW = 168;
        public static int GameSizeH = 52;

        private static StateManager? TopLevelUI;

        static void Main(string[] args)
        {
            SadConsole.Settings.WindowTitle = "- MYSTOWER -";
            // ? This appears to not work on Linux/MonoGame. TODO: test on PC
            //SadConsole.Settings.WindowMinimumSize = new SadRogue.Primitives.Point(640, 480);
            SadConsole.Settings.ResizeMode = SadConsole.Settings.WindowResizeOptions.None;
            
            SadConsole.Game.Create(GameSizeW, GameSizeH);

            SadConsole.Game.Instance.OnStart = GameInit;
            SadConsole.Game.Instance.OnEnd = GameTeardown;

            SadConsole.Game.Instance.FrameUpdate += (object? sender, SadConsole.GameHost host) => {
                // Not sure of a better way to do global hotkeys, so we'll put this here I guess!
                if (host.Keyboard.IsKeyPressed(Keys.F4))
                    SadConsole.Game.Instance.ToggleFullScreen();
            };

            SadConsole.Game.Instance.Run();
            SadConsole.Game.Instance.Dispose();
        }

        static void GameTeardown()
        {
            TopLevelUI?.UnregisterResizeHandler();
        }

        static void GameInit()
        {
            Console.WriteLine("Initializing game");

            SadConsole.GameHost.Instance.LoadFont("Resources/tiles.font");

            TopLevelUI = new StateManager(new State.TitleScreen());
            TopLevelUI.RegisterResizeHandler();

            SadConsole.GameHost.Instance.Screen.Children.Add(TopLevelUI);
        }
    }
}
