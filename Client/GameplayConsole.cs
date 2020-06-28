using System;
using System.Collections.Generic;
using Server;
using Server.Data;
using Server.Logic;
using SadConsole.Entities;

using Point = SadRogue.Primitives.Point;
using Color = SadRogue.Primitives.Color;

using static SadConsole.PointExtensions;
using SadConsole.Components;
using System.Linq;
using SadConsole.Input;

namespace Client
{
    public class MapActor : Entity
    {
        public Actor Actor { get; }
        public SadConsole.Console ScrollingParent { get; }
        public Point VisualOffset { get; set; }

        public void SnapToActualPosition()
        {
            Position = new Point(Actor.position.x, Actor.position.y)
                .SurfaceLocationToPixel(ScrollingParent.FontSize.X, ScrollingParent.FontSize.Y);
            VisualOffset = default(Point);
        }

        public MapActor(SadConsole.Console parent, Actor actor) :
            base(Color.White, Color.Transparent, actor.aiType == nameof(Server.Logic.AIType.PlayerControlled) ? 707 : 125)
        {
            this.Actor = actor;
            Animation.Font = parent.Font;
            Animation.FontSize = parent.FontSize;
            
            Animation.UsePixelPositioning = true;

            parent.Children.Add(this);
            this.Parent = parent;
            ScrollingParent = parent;
        }

        public override void Draw(TimeSpan delta)
        {
            PositionOffset += VisualOffset;
            base.Draw(delta);
        }
    }

    public class GameplayConsole : SadConsole.Console, IGameClient
    {
        private GameServer server;

        private TileMapConsole mapLayer;

        private MessageLogConsole msgLogLayer;

        private List<MapActor> mapActors = new List<MapActor>(64);
        private MapActor? LookupMapActor(Actor actor)
            => mapActors.FirstOrDefault(x => x.Actor == actor);

        private Choreographer Choreographer { get; } = new Choreographer();

        public GameplayConsole(int w, int h, GameServer s) : base(w, h)
        {
            server = s;

            mapLayer = new TileMapConsole(w / 4, h / 4);
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
            var vis = LookupMapActor(actor);
            if (vis != null)
                Choreographer.AddEffect(new Effects.LerpMove(dx, dy, 10, vis));
        }

        public void OnEntityVanish(Actor actor)
        {
            mapActors.RemoveAll(x => x.Actor == actor);
        }

        public void OnAddLogMessage(string messageId)
        {
            msgLogLayer.AddMessage(messageId);
        }

        public void OnMapChange(MapData newMapData)
        {
            mapLayer.RebuildTileMap(newMapData);
        }

        public override bool ProcessKeyboard(SadConsole.Input.Keyboard info)
        {
            // need to handle input for any actor the server needs an action for
            if (!Choreographer.Busy && server.WaitingOn != null)
            {
                IAction? selectedAction = null;

                int dx = 0, dy = 0;
                if (info.IsKeyDown(Keys.Left)) dx -= 1;
                if (info.IsKeyDown(Keys.Right)) dx += 1;
                if (info.IsKeyDown(Keys.Up)) dy -= 1;
                if (info.IsKeyDown(Keys.Down)) dy += 1;

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
            foreach (var v in mapActors)
                v.SnapToActualPosition();

            Choreographer.PrepareDraw(mapActors, timeElapsed);
            
            if (server.WaitingOn != null)
            {
                var entity = LookupMapActor(server.WaitingOn);
                if (entity != null)
                    mapLayer.CenterViewOn(entity);
            }
            
            base.Draw(timeElapsed);
        }
    }
}