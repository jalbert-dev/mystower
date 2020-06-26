using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Server;
using Server.Data;
using Server.Logic;

using static SadConsole.RectangleExtensions;

namespace Client
{
    public class MapActor
    {
        public Actor actor;

        public MapActor(Actor actor)
        {
            this.actor = actor;
        }

        public int glyph;
    }

    public class GameplayConsole : SadConsole.ContainerConsole, IGameClient
    {
        private GameServer server;

        private SadConsole.ScrollingConsole mapLayer;
        private SadConsole.ScrollingConsole entityLayer;

        private MessageLogConsole msgLogLayer;

        private Dictionary<Actor, MapActor> mapActors = new Dictionary<Actor, MapActor>();

        public GameplayConsole(int w, int h, GameServer s) : base()
        {
            server = s;

            mapLayer = new SadConsole.ScrollingConsole(w / 3, h / 3);
            mapLayer.DefaultBackground = Color.Black;
            mapLayer.Font = mapLayer.Font.Master.GetFont(SadConsole.Font.FontSizes.Three);
            Children.Add(mapLayer);

            entityLayer = new SadConsole.ScrollingConsole(w / 3, h / 3);
            entityLayer.Font = mapLayer.Font;
            entityLayer.DefaultBackground = Color.Transparent;
            entityLayer.Clear();
            Children.Add(entityLayer);

            msgLogLayer = new MessageLogConsole(
                Program.GameSizeW * 3 / 4, 
                Program.GameSizeH * 1 / 5);
            msgLogLayer.Position = new Point(
                Program.GameSizeW / 8, 
                Program.GameSizeH * 4 / 5);
            msgLogLayer.DefaultBackground = Color.Gray;
            Children.Add(msgLogLayer);
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

        public void OnAddLogMessage(string messageId)
        {
            msgLogLayer.AddMessage(messageId);
        }

        private void RebuildLayerData(MapData map)
        {
            int w = map.Width;
            int h = map.Height;

            mapLayer.Resize(w, h, false);

            mapLayer.Clear();

            for (int i = 0; i < w; i++)
                for (int j = 0; j < h; j++)
                    mapLayer.SetGlyph(i, j, map.tiles[i,j] == 0 ? 46 : '#');

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
                IAction? selectedAction = null;

                int dx = 0, dy = 0;
                if (info.IsKeyPressed(Keys.Left)) dx -= 1;
                if (info.IsKeyPressed(Keys.Right)) dx += 1;
                if (info.IsKeyPressed(Keys.Up)) dy -= 1;
                if (info.IsKeyPressed(Keys.Down)) dy += 1;

                if (dx != 0 || dy != 0)
                {
                    // try movement
                    selectedAction = new Actions.Move(dx, dy);
                }

                if (info.IsKeyPressed(Keys.Space))
                {
                    selectedAction = new Actions.Idle();
                }

                if (selectedAction != null)
                {
                    var err = server.AssignActionForWaitingActor(selectedAction);
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
                var pos = server.WaitingOn.position;
                mapLayer.CenterViewPortOnPoint(new Point(pos.x, pos.y));
            }

            entityLayer.Clear();
            foreach (var visibleActor in mapActors.Values)
            {
                entityLayer.SetGlyph(visibleActor.actor.position.x, 
                                     visibleActor.actor.position.y,
                                     visibleActor.glyph,
                                     Color.White,
                                     mapLayer.DefaultBackground);
            }
            
            entityLayer.ViewPort = mapLayer.ViewPort;
            base.Draw(timeElapsed);
        }
    }
}