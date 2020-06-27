using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Server;
using Server.Data;
using Server.Logic;
using SadConsole.Entities;

using static SadConsole.RectangleExtensions;
using SadConsole.Components;
using System.Linq;

namespace Client
{
    public class MapActor : Entity
    {
        public Actor Actor { get; }
        private SadConsole.ScrollingConsole ScrollingParent { get; }

        public void SnapToActualPosition()
        {
            UsePixelPositioning = true;

            Position = new Point(Actor.position.x, Actor.position.y).ConsoleLocationToPixel(Parent.Font);
            Position -= ScrollingParent.ViewPort.Location.ConsoleLocationToPixel(Parent.Font);
        }

        public MapActor(SadConsole.ScrollingConsole parent, Actor actor) :
            base(Color.White, Color.Black, actor.aiType == nameof(Server.Logic.AIType.PlayerControlled) ? 1 : 'e')
        {
            this.Actor = actor;
            Font = parent.Font;

            parent.Children.Add(this);
            this.Parent = parent;
            ScrollingParent = parent;
        }

    }

    public class GameplayConsole : SadConsole.ContainerConsole, IGameClient
    {
        private GameServer server;

        private SadConsole.ScrollingConsole mapLayer;

        private MessageLogConsole msgLogLayer;

        private List<MapActor> mapActors = new List<MapActor>(64);
        private MapActor LookupMapActor(Actor actor)
            => mapActors.FirstOrDefault(x => x.Actor == actor);

        private Choreographer Choreographer { get; } = new Choreographer();

        public GameplayConsole(int w, int h, GameServer s) : base()
        {
            server = s;

            mapLayer = new SadConsole.ScrollingConsole(w / 3, h / 3);
            mapLayer.DefaultBackground = Color.Black;
            mapLayer.Font = mapLayer.Font.Master.GetFont(SadConsole.Font.FontSizes.Three);
            Children.Add(mapLayer);

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
            base.Update(timeElapsed);
        }

        public void OnEntityAppear(Actor actor)
        {
            mapActors.Add(new MapActor(mapLayer, actor));
        }

        public void OnEntityMove(Actor actor, int dx, int dy)
        {
            Choreographer.AddEffect(new Effects.LerpMove(dx, dy, 4, LookupMapActor(actor)));
        }

        public void OnEntityVanish(Actor actor)
        {
            mapActors.RemoveAll(x => x.Actor == actor);
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
        }

        public void OnMapChange(MapData newMapData)
        {
            RebuildLayerData(newMapData);
        }

        public override bool ProcessKeyboard(SadConsole.Input.Keyboard info)
        {
            // need to handle input for any actor the server needs an action for
            if (!Choreographer.Busy && server.WaitingOn != null)
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

                if (info.IsKeyPressed(Keys.P))
                {
                    foreach (var x in mapActors.Select((x, i) => (x, i)))
                        Choreographer.AddEffect(new Effects.Wiggle(x.x, x.i % 3 == 0));
                }

                if (info.IsKeyPressed(Keys.L))
                    msgLogLayer.ToggleVisible();

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

            Choreographer.PrepareDraw(mapActors, timeElapsed);
            
            base.Draw(timeElapsed);
        }
    }
}