using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using SadConsole;
using SadConsole.Input;
using SadRogue.Primitives;
using Server;
using Server.Data;
using Server.Logic;
using Server.Message;

using C = System.Console;

namespace Client.State
{
    public class DebugStatsConsole : SadConsole.Console
    {
        public long LastSimulationTime { get; set; }
        public long UpdateDelta { get; set; }
        public long RenderDelta { get; set; }

        public DebugStatsConsole() : base(40, 4)
        {
            DefaultBackground = Color.Transparent;
            IsVisible = false;

            Cursor.UseLinuxLineEndings = true;
            Cursor.PrintAppearance = new SadConsole.ColoredGlyph 
            { 
                Foreground=Color.White, 
                Background=Color.Transparent 
            };
        }

        public override void Draw(TimeSpan delta)
        {
            this.Clear();
            Cursor.Position = new Point(0, 0);
            Cursor.Print($"Last simulation time: {LastSimulationTime}ms\n");
            Cursor.Print($"   Last update delta: {UpdateDelta}ms\n");
            Cursor.Print($"   Last render delta: {RenderDelta}ms\n");
            base.Draw(delta);
        }
    }

    public class Gameplay : SadConsole.Console, IGameClient, IState<StateManager>
    {
        private GameServer server;

        private TileMapConsole mapLayer;

        private MessageLogConsole msgLogLayer;

        private List<MapActor> mapActors = new List<MapActor>(64);
        private MapActor? LookupMapActor(Actor actor)
            => mapActors.FirstOrDefault(x => x.Actor == actor);

        private Choreographer Choreographer { get; } = new Choreographer();

        private Util.CoroutineContainer Coroutines { get; } = new Util.CoroutineContainer();

        private bool returnToTitle = false;

        // Contains an actor that the server is waiting on
        private Server.Data.Actor? waitingActor;
        // Contains the next selected action to relay to the server
        private IAction? nextAction;

        // If true, server simulation is performed asynchronously. (beware!)
        private bool isAsyncSim = true;
        // The first simulation tick must be synchronous to avoid entering
        // the draw step with no waiting actor (otherwise camera will "snap"
        // into place on frame 2 and it looks odd)
        private bool isFirstSim = true;
        private bool ShouldAsyncSimulate => !isFirstSim && isAsyncSim;

        private DebugStatsConsole debugStats = new DebugStatsConsole();

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

            Children.Add(debugStats);
        }

        public void HandleMessages(IEnumerable<IGameMessage> messages)
        {
            foreach (var msg in messages)
                msg.Dispatch(this);
        }

        public void ProcessServerResult(SimResult result)
        {
            if (result.Error != null)
                C.WriteLine(result.Error.Message);
            else
                HandleMessages(result.Messages);
            waitingActor = result.WaitingActor;
        }

        private static Task<SimResult> TimedRunSimulation(GameServer server,
                                                          IAction? nextAction,
                                                          DebugStatsConsole? debugStats = null)
            =>  Task.Run(() => {
                    var updateTimer = Stopwatch.StartNew();
                    var result = server.Run(nextAction);
                    if (debugStats != null)
                        debugStats.LastSimulationTime = updateTimer.ElapsedMilliseconds;
                    return result;
                });

        private static IEnumerable RunServerSimulation(bool async,
                                                       GameServer server,
                                                       IAction? nextAction,
                                                       Action<SimResult> continuation,
                                                       DebugStatsConsole? debugStats = null)
        {
            if (async)
            {
                // Contains the eventual server sim results
                Task<SimResult> serverSimulation = TimedRunSimulation(server, nextAction, debugStats);
                yield return serverSimulation;
                continuation(serverSimulation.Result);
            }
            else
            {
                continuation(TimedRunSimulation(server, nextAction, debugStats).Result);
            }
            yield break;
        }

        public override void Update(TimeSpan timeElapsed)
        {
            debugStats.UpdateDelta = timeElapsed.Milliseconds;
            // Only run the server simulation if we've selected an action
            // or the server's not waiting on any actors
            if (nextAction != null || waitingActor == null)
            {
                Coroutines.Start(RunServerSimulation(ShouldAsyncSimulate,
                                                     server,
                                                     nextAction,
                                                     ProcessServerResult,
                                                     debugStats));
                
                // If we're simulating, we're no longer waiting on a unit
                waitingActor = null;
                nextAction = null;
            }
            isFirstSim = false;

            Coroutines.Update();

            base.Update(timeElapsed);
        }

        public override bool ProcessKeyboard(SadConsole.Input.Keyboard info)
        {
            if (info.IsKeyPressed(Keys.F1))
                debugStats.IsVisible = !debugStats.IsVisible;

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
                    isAsyncSim = !isAsyncSim;
                    msgLogLayer.AddMessage($"Async update: {isAsyncSim}");
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
            debugStats.RenderDelta = timeElapsed.Milliseconds;
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
            HandleMessages(server.GetClientInitMessages());

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