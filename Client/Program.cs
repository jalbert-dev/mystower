using System;
using System.IO;
using Server;

namespace Client
{
    class Program
    {
        public static int GameSizeW = 168;
        public static int GameSizeH = 52;

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
            
            SadConsole.Global.LoadFont("Resources/tiles.font");
            
            client = new GameplayConsole(GameSizeW, GameSizeH, GameServer.NewGame());
            client.Init();

            SadConsole.Global.CurrentScreen = client;
            SadConsole.Global.CurrentScreen.IsFocused = true;
        }
    }
}
