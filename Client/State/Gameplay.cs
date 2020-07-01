using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SadConsole.Input;
using SadRogue.Primitives;
using Server;
using Server.Data;
using Server.Logic;
using Server.Message;

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

        // Contains an actor that the server is waiting on
        private Server.Data.Actor? waitingActor;
        // Contains the next selected action to relay to the server
        private IAction? nextAction;

        // If true, server simulation is performed asynchronously. (beware!)
        private bool isAsyncUpdate = true;
        // Contains the eventual server sim results
        private Task<SimResult>? serverSimulation = null;

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

        public void HandleMessages(IEnumerable<IGameMessage> messages)
        {
            foreach (var msg in messages)
                msg.Dispatch(this);
        }

        public void Init()
        {
            HandleMessages(server.GetClientInitMessages());
        }

        public void ProcessServerResult(SimResult result)
        {
            if (result.Error != null)
                Console.WriteLine(result.Error.Message);
            else
                HandleMessages(result.Messages);
            waitingActor = result.WaitingActor;
        }

        public override void Update(TimeSpan timeElapsed)
        {
            // Only run the server simulation if we've selected an action
            // or if we're not waiting for any actors
            if (nextAction != null || waitingActor == null)
            {
                // If we're simulating, we're no longer waiting on a unit
                waitingActor = null;
                if (isAsyncUpdate)
                {
                    if (serverSimulation == null)
                    {
                        // beware the thunk! copy the nextAction reference!
                        var next = nextAction;
                        serverSimulation = Task.Run(() => server.Run(next));
                    }
                    else if (serverSimulation.IsCompleted)
                    {
                        var result = serverSimulation.Result;
                        ProcessServerResult(result);
                        serverSimulation = null;
                    }
                }
                else
                {
                    ProcessServerResult(server.Run(nextAction));
                }
                nextAction = null;
            }

            base.Update(timeElapsed);
        }

        public override bool ProcessKeyboard(SadConsole.Input.Keyboard info)
        {
            // need to handle input for any actor the server needs an action for
            if (!Choreographer.Busy && waitingActor != null)
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

                if (info.IsKeyPressed(Keys.A))
                {
                    isAsyncUpdate = !isAsyncUpdate;
                    msgLogLayer.AddMessage($"Async update: {isAsyncUpdate}");
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
                    nextAction = selectedAction;
                    // TODO: think this's technically wrong but I'm still trying to figure
                    //       out what's going on with SadConsole's keyboard input system
                    return true;
                }
            }
            return false;
        }

        public override void Draw(TimeSpan timeElapsed)
        {
            Choreographer.PrepareDraw(mapActors, timeElapsed);
            
            if (waitingActor != null)
            {
                var entity = LookupMapActor(waitingActor);
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

        public void HandleMessage(EntityAppeared msg)
        {
            mapActors.Add(new MapActor(mapLayer, msg.Actor));
        }

        public void HandleMessage(EntityVanished msg)
        {
            mapActors.RemoveAll(x => x.Actor == msg.Actor);
        }

        public void HandleMessage(EntityMoved msg)
        {
            var vis = LookupMapActor(msg.Actor);
            if (vis != null)
                Choreographer.AddMotion(new Motions.LerpMove(
                    msg.SourceTile.x, msg.SourceTile.y, 
                    msg.DestTile.x, msg.DestTile.y, 
                    10, vis));
        }

        public void HandleMessage(MapChanged msg)
        {
            mapLayer.RebuildTileMap(msg.NewMapData);
        }

        public void HandleMessage(AddedToLog msg)
        {
            msgLogLayer.AddMessage(msg.MessageId);
        }

        public void HandleMessage(EntityAttacked msg)
        {
            var attacker = LookupMapActor(msg.Actor);
            if (attacker != null)
                Choreographer.AddMotion(new Motions.Wiggle(attacker, true, 30));
            foreach (var a in msg.Results)
            {
                var target = LookupMapActor(a.Target);
                if (target != null)
                    Choreographer.AddMotion(new Motions.Wiggle(target, false, 30));
                msgLogLayer.AddMessage($"Actor attacks Actor!");
                msgLogLayer.AddMessage($"{a.DamageDealt} damage!");
            }
        }
    }
}