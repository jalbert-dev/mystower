using System;
using System.Collections.Generic;
using System.Linq;
using SadConsole.Input;
using SadRogue.Primitives;
using Server;
using Server.Data;
using Server.Logic;

namespace Client.State
{
    public class Gameplay : SadConsole.Console, IGameClient, IState<StateManager>
    {
        private GameServer server;

        private TileMapConsole mapLayer;

        private MessageLogConsole msgLogLayer;

        private List<MapActor> mapActors = new List<MapActor>(64);
        private MapActor? LookupMapActor(Actor actor)
            => mapActors.FirstOrDefault(x => x.Actor == actor);

        private Choreographer Choreographer { get; } = new Choreographer();

        private bool returnToTitle = false;

        public Gameplay(int w, int h, GameServer s) : base(w, h)
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

        public void OnEntityMove(Actor actor, int sx, int sy, int dx, int dy)
        {
            var vis = LookupMapActor(actor);
            if (vis != null)
                Choreographer.AddMotion(new Motions.LerpMove(sx, sy, dx, dy, 10, vis));
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
                        Choreographer.AddMotion(new Motions.Wiggle(x.x, x.i % 3 == 0));
                }

                if (info.IsKeyPressed(Keys.F5))
                {
                    string save = server.ToSaveGame();
                    System.IO.Directory.CreateDirectory("Saves");
                    System.IO.File.WriteAllText("Saves/save.sav", save);
                }

                if (info.IsKeyPressed(Keys.Z))
                {
                    selectedAction = new Actions.TryAttack();
                }

                if (info.IsKeyPressed(Keys.Escape))
                {
                    returnToTitle = true;
                    return true;
                }

                if (info.IsKeyPressed(Keys.L))
                    msgLogLayer.ToggleVisible();

                if (selectedAction != null)
                {
                    System.Threading.Thread.Sleep(32);
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
            if (!Choreographer.Busy)
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

        public IState<StateManager>? OnEnter(StateManager obj)
        {
            Init();

            obj.Children.Add(this);
            IsFocused = true;

            return null;
        }

        public IState<StateManager>? Exec(StateManager obj)
        {
            if (returnToTitle)
                return new State.TitleScreen(Width, Height);
            return null;
        }

        public IState<StateManager>? OnExit(StateManager obj)
        {
            obj.Children.Remove(this);

            return null;
        }

        public void OnEntityAttack(Actor actor, AttackResults result)
        {
            var attacker = LookupMapActor(actor);
            if (attacker != null)
                Choreographer.AddMotion(new Motions.Wiggle(attacker, true, 30));
            foreach (var a in result.results)
            {
                var target = LookupMapActor(a.Target);
                if (target != null)
                    Choreographer.AddMotion(new Motions.Wiggle(target, false, 30));
                OnAddLogMessage($"Actor attacks Actor!");
                OnAddLogMessage($"{a.DamageDealt} damage!");
            }
        }
    }
}