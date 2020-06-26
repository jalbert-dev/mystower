using System;
using Server;

namespace Client
{
    class Program
    {
        public static int GameSizeW = 162;
        public static int GameSizeH = 51;

        static GameplayConsole? client = null;

        static void Main(string[] args)
        {
            SadConsole.Game.Create(GameSizeW, GameSizeH);
            SadConsole.Game.OnInitialize = GameInit;
            SadConsole.Game.Instance.Run();
            SadConsole.Game.Instance.Dispose();
        }

        static void GameInit()
        {
            Console.WriteLine("Initializing game");
            client = new GameplayConsole(GameSizeW, GameSizeH, GameServer.NewGame());
            client.Init();

            SadConsole.Global.CurrentScreen = client;
            SadConsole.Global.CurrentScreen.IsFocused = true;
        }
    }
}
