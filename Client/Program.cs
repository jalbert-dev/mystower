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
            SadConsole.Game.Instance.OnStart = GameInit;
            SadConsole.Game.Instance.Run();
            SadConsole.Game.Instance.Dispose();
        }

        static void GameInit()
        {
            Console.WriteLine("Initializing game");
            
            SadConsole.GameHost.Instance.LoadFont("Resources/tiles.font");
            
            client = new GameplayConsole(GameSizeW, GameSizeH, GameServer.NewGame());
            client.Init();

            SadConsole.GameHost.Instance.Screen.Children.Add(client);
            client.IsFocused = true;
        }
    }
}
