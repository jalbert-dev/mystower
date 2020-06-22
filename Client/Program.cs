using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Server;
using Server.Data;

namespace Client
{
    public class GameplayConsole : SadConsole.Console, IGameClient
    {
        private GameServer server;

        public GameplayConsole(int w, int h, GameServer s) : base(w, h)
        {
            server = s;
        }

        public void Init()
        {
            server.RegisterClient(this);
        }

        public override void Update(TimeSpan timeElapsed)
        {
            Server.IError? err = server.Run();
            if (err != null)
                Console.WriteLine(err.Message);
        }

        public void OnEntityAppear(Actor actor)
        {
            SetGlyph(actor.position.x, actor.position.y, 1);
        }

        public void OnEntityMove(Actor actor, int dx, int dy)
        {
            SetGlyph(actor.position.x-dx, actor.position.y-dy, 46);
            SetGlyph(actor.position.x, actor.position.y, 1);
        }

        public void OnEntityVanish(Actor actor)
        {
        }

        public void OnMapChange(MapData newMapData)
        {
            for (int i = 0; i < newMapData.tiles.GetLength(0); i++)
                for (int j = 0; j < newMapData.tiles.GetLength(1); j++)
                    SetGlyph(i+1, j+1, 46);
        }

        public override bool ProcessKeyboard(SadConsole.Input.Keyboard info)
        {
            // need to handle input for any actor the server needs an action for
            if (server.WaitingOn != null)
            {
                int dx = 0, dy = 0;
                if (info.IsKeyPressed(Keys.Left)) dx -= 1;
                if (info.IsKeyPressed(Keys.Right)) dx += 1;
                if (info.IsKeyPressed(Keys.Up)) dy -= 1;
                if (info.IsKeyPressed(Keys.Down)) dy += 1;

                if (dx != 0 || dy != 0)
                {
                    // try movement
                    var err = server.AssignActionForWaitingActor(new Server.Logic.Actions.Move(dx, dy));
                    if (err != null)
                        Console.WriteLine(err.Message);
                    // TODO: think this's technically wrong but I'm still trying to figure
                    //       out what's going on with SadConsole's keyboard input system
                    return true;
                }
            }
            return false;
        }
    }

    class Program
    {
        static GameplayConsole? client = null;
        static GameServer server = new GameServer();

        static void Main(string[] args)
        {
            SadConsole.Game.Create(80, 25);
            SadConsole.Game.OnInitialize = GameInit;
            SadConsole.Game.Instance.Run();
            SadConsole.Game.Instance.Dispose();
        }

        static void GameInit()
        {
            Console.WriteLine("Initializing game");
            client = new GameplayConsole(80, 25, server);
            client.Init();

            SadConsole.Global.CurrentScreen = client;
            SadConsole.Global.CurrentScreen.IsFocused = true;
        }
    }
}
