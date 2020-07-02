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
    public class Gameplay : SadConsole.Console, IState<StateManager>
    {
        public Consoles.TileMap TileMap { get; }
        public Consoles.MessageLog MessageLog { get; }
        public Consoles.DebugStats DebugStatsDisplay = new Consoles.DebugStats();

        public Choreographer Choreographer { get; } = new Choreographer();
        public Util.CoroutineContainer Coroutines { get; } = new Util.CoroutineContainer();

        public List<MapActor> MapActors { get; } = new List<MapActor>(64);
        public MapActor? LookupMapActor(Actor actor)
            => MapActors.FirstOrDefault(x => x.Actor == actor);
        
        private GameServer server;
        private GameplayMessageHandler msgHandler;
        
        // The last valid key state. Consumed on use.
        private SadConsole.Input.Keyboard? keyState = null;


        private bool returnToTitle = false;

        // The actor the server is waiting on, if one exists.
        private Server.Data.Actor? waitingActor;

        public Gameplay(int w, int h, GameServer s) : base(w, h)
        {
            server = s;
            msgHandler = new GameplayMessageHandler(this);

            TileMap = new Consoles.TileMap(w / 4, h / 4);
            Children.Add(TileMap);

            MessageLog = new Consoles.MessageLog(
                Program.GameSizeW * 3 / 4, 
                Program.GameSizeH * 1 / 5);
            MessageLog.Position = new Point(
                Program.GameSizeW / 8, 
                Program.GameSizeH * 4 / 5);
            MessageLog.DefaultBackground = Color.Gray;
            Children.Add(MessageLog);

            Children.Add(DebugStatsDisplay);

            Coroutines.Add(SimulationLoop(false));
        }

        public void HandleMessages(IEnumerable<IGameMessage> messages)
        {
            foreach (var msg in messages)
                msg.Dispatch(msgHandler);
        }

        private void ProcessSimulationResult(SimResult result)
        {
            if (result.Error != null)
                C.WriteLine(result.Error.Message);
            else
                HandleMessages(result.Messages);
            waitingActor = result.WaitingActor;
        }

        private static SimResult TimedRunSimulation(GameServer server,
                                                    IAction? nextAction,
                                                    Consoles.DebugStats? debugStats = null)
        {
            var updateTimer = Stopwatch.StartNew();
            var result = server.Run(nextAction);
            if (debugStats != null)
                debugStats.LastSimulationTime = updateTimer.ElapsedMilliseconds;
            return result;
        }

        private IEnumerable SimulationLoop(bool async)
        {
            while (true)
            {
                IAction? userAction = null;
                if (!Choreographer.IsBusy && keyState != null)
                {
                    userAction = TrySelectAction(keyState, waitingActor);
                    keyState = null;
                }
                
                var simTask = Task.Run(() => TimedRunSimulation(server, userAction, DebugStatsDisplay));

                // TODO: async could work via two-bit prediction or something.
                //       if the last few significant updates have been >16ms,
                //       switch to async? or the other way around, keep a running
                //       average of the update delta, and if it's around
                //       <8ms, Sleep for 4ms to give the task time to complete?

                // As written here, async incurs 1 frame of latency, which will
                // cause slight but noticeable hitching when the player holds
                // down a movement key! It does work, however, so with some
                // clever prediction this could help with long updates.
                if (async)
                    yield return simTask;

                ProcessSimulationResult(simTask.Result);
                yield return null;
            }
        }

        public override void Update(TimeSpan timeElapsed)
        {
            DebugStatsDisplay.UpdateDelta = timeElapsed.Milliseconds;

            Coroutines.Update();

            base.Update(timeElapsed);
        }

        /// <summary>
        /// Attempts to parse the user's input into a valid game action for the
        /// given actor.
        /// 
        /// If the given actor is null or the player's input does not correspond
        /// to any game action, returns null.
        /// </summary>
        private IAction? TrySelectAction(SadConsole.Input.Keyboard info, Actor? waitingActor)
        {
            if (waitingActor == null)
                return null;

            int dx = 0, dy = 0;
            if (info.IsKeyDown(Keys.Left)) dx -= 1;
            if (info.IsKeyDown(Keys.Right)) dx += 1;
            if (info.IsKeyDown(Keys.Up)) dy -= 1;
            if (info.IsKeyDown(Keys.Down)) dy += 1;

            if (dx != 0 || dy != 0)
            {
                // try movement
                return new Actions.Move(dx, dy);
            }

            if (info.IsKeyPressed(Keys.Space))
            {
                return new Actions.Idle();
            }

            if (info.IsKeyPressed(Keys.Z))
            {
                return new Actions.TryAttack();
            }

            return null;
        }

        private void CheckNonGameplayHotkeys(SadConsole.Input.Keyboard info)
        {
            if (info.IsKeyPressed(Keys.F1))
                DebugStatsDisplay.IsVisible = !DebugStatsDisplay.IsVisible;

            if (info.IsKeyPressed(Keys.Escape))
                returnToTitle = true;

            if (info.IsKeyPressed(Keys.L))
                MessageLog.ToggleVisible();

            if (info.IsKeyPressed(Keys.P))
            {
                foreach (var x in MapActors.Select((x, i) => (x, i)))
                    Choreographer.AddMotion(new Motions.Wiggle(x.x, x.i % 3 == 0));
            }

            if (info.IsKeyPressed(Keys.F5))
            {
                string save = server.ToSaveGame();
                System.IO.Directory.CreateDirectory("Saves");
                System.IO.File.WriteAllText("Saves/save.sav", save);
            }
        }

        public override bool ProcessKeyboard(SadConsole.Input.Keyboard info)
        {
            CheckNonGameplayHotkeys(info);
            keyState = info;
            return false;
        }

        public override void Draw(TimeSpan timeElapsed)
        {
            DebugStatsDisplay.RenderDelta = timeElapsed.Milliseconds;
            Choreographer.PrepareDraw(MapActors, timeElapsed);
            
            if (waitingActor != null)
            {
                var entity = LookupMapActor(waitingActor);
                if (entity != null)
                    TileMap.CenterViewOn(entity);
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
    }
}