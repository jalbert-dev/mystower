using System;

namespace Client
{
    class Program
    {
        public static int GameSizeW = 168;
        public static int GameSizeH = 52;

        static void Main(string[] args)
        {

            SadConsole.Game.Create(GameSizeW, GameSizeH);
            SadConsole.Game.Instance.OnStart = GameInit;
            SadConsole.Game.Instance.Run();
            SadConsole.Game.Instance.Dispose();
        }

        static void GameInit()
        {
            Console.WriteLine("Initializing game");

            SadConsole.GameHost.Instance.LoadFont("Resources/tiles.font");

            var fsm = new StateManager(new State.TitleScreen(GameSizeW, GameSizeH));
            SadConsole.GameHost.Instance.Screen.Children.Add(fsm);
        }
    }
}
