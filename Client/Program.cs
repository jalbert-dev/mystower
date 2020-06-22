using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Server;
using Server.Data;

namespace Client
{
    public class MapActor
    {
        public Server.Data.Actor actor;

        public MapActor(Actor actor)
        {
            this.actor = actor;
        }

        public int glyph;
    }

    public class GameplayConsole : SadConsole.ContainerConsole, IGameClient
    {
        private GameServer server;

        private SadConsole.Console mapLayer;
        private SadConsole.Console entityLayer;

        // SadConsole's default Viewport implementation is... not great,
        // so I'm having to roll my own here
        private Rectangle viewportRect;

        private Dictionary<Actor, MapActor> mapActors = new Dictionary<Actor, MapActor>();

        public GameplayConsole(int w, int h, GameServer s) : base()
        {
            server = s;

            mapLayer = new SadConsole.ScrollingConsole(w, h);
            Children.Add(mapLayer);

            entityLayer = new SadConsole.ScrollingConsole(w, h);
            Children.Add(entityLayer);
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
            mapActors[actor] = new MapActor(actor) 
            {
                glyph = actor.aiType == nameof(Server.Logic.AIType.PlayerControlled) ? 1 : 'e'
            };
        }

        public void OnEntityMove(Actor actor, int dx, int dy)
        {
        }

        public void OnEntityVanish(Actor actor)
        {
            mapActors.Remove(actor);
        }

        private void RebuildLayerData(MapData map)
        {
            int w = map.tiles.GetLength(0);
            int h = map.tiles.GetLength(1);

            mapLayer.Resize(w, h, false);

            //mapLayer.ViewPort = new Rectangle(-2, 0, 2, 2);

            // mapLayer.SetGlyph(0, 0, 'a');
            // mapLayer.SetGlyph(1, 0, 'b');
            // mapLayer.SetGlyph(2, 0, 'c');
            // mapLayer.SetGlyph(0, 0, 'a');
            // mapLayer.SetGlyph(0, 1, 'b');
            // mapLayer.SetGlyph(0, 2, 'c');
            mapLayer.SetSurface(
                map.tiles
                    .Cast<byte>()
                    .Select(tileId => new SadConsole.Cell {
                        Glyph = tileId == 0 ? 46 : '#'
                    })
                    .ToArray(),
                w,
                h);

            entityLayer.Resize(w, h, false);
        }

        public void OnMapChange(MapData newMapData)
        {
            RebuildLayerData(newMapData);
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

        public override void Draw(TimeSpan timeElapsed)
        {
            if (server.WaitingOn != null)
            {
                // move viewport to center the player character
                mapLayer.ViewPort = new Rectangle
                {
                    Width = 6,
                    Height = 6,
                    X = 20
                };
                
            }

            entityLayer.Clear();
            foreach (var visibleActor in mapActors.Values)
            {
                entityLayer.SetGlyph(visibleActor.actor.position.x, 
                                     visibleActor.actor.position.y,
                                     visibleActor.glyph);
            }

            entityLayer.ViewPort = mapLayer.ViewPort;
            
            base.Draw(timeElapsed);
        }
    }

    class Program
    {
        static GameplayConsole? client = null;

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
            client = new GameplayConsole(80, 25, GameServer.NewGame());
            client.Init();

            SadConsole.Global.CurrentScreen = client;
            SadConsole.Global.CurrentScreen.IsFocused = true;
        }
    }
}
