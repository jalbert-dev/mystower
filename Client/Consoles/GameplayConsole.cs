using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using SadConsole.Input;
using SadRogue.Primitives;
using Server;
using Server.Data;
using Server.Logic;

using C = System.Console;

namespace Client.Consoles
{
    public class Gameplay : SadConsole.Console, IResizeHandler
    {
        public class ActorSet
        {
            List<MapActor> mapActors { get; } = new List<MapActor>(64);
            public IEnumerable<MapActor> Actors => mapActors;

            public void Remove(DataHandle<Actor> actor)
            {
                mapActors.RemoveAll(x => {
                    var shouldRemove = x.Actor.HandleEquals(actor);
                    if (shouldRemove)
                        OnRemoveActor?.Invoke(x);
                    return shouldRemove;
                });
            }

            public MapActor? Lookup(DataHandle<Actor> actor)
                => mapActors.FirstOrDefault(x => x.Actor.HandleEquals(actor));

            public void Add(MapActor mapActor)
            {
                if (mapActors.Any(x => x.Actor.HandleEquals(mapActor.Actor)))
                    return;
                mapActors.Add(mapActor);
            }

            public MapActor? this[DataHandle<Actor> index] => Lookup(index);

            public delegate void ActorRemovalHandler(MapActor actor);
            public event ActorRemovalHandler? OnRemoveActor;
        }

        public Consoles.TileMap TileMap { get; }
        public Consoles.MessageLog MessageLog { get; }
        public Consoles.DebugStats DebugStatsDisplay = new Consoles.DebugStats();

        public GameServer Server { get; }
        public Choreographer Choreographer { get; } = new Choreographer();
        public Util.CoroutineContainer Coroutines { get; } = new Util.CoroutineContainer();

        public ActorSet MapActors { get; } = new ActorSet();

        public bool ShouldReturnToTitle { get; private set; } = false;

        private MapActor? CurrentPC => waitingActor.HasValue ? MapActors.Lookup(waitingActor.Value) : null;
        
        private GameplayMessageHandler msgHandler;
        
        // The last valid key state. Consumed on use.
        private SadConsole.Input.Keyboard? keyState = null;

        // The actor the server is waiting on, if one exists.
        private DataHandle<Actor>? waitingActor;
        private MapActor? fallbackCameraFocusActor;

        public Gameplay(GameServer s) : base(1, 1)
        {
            Server = s;
            msgHandler = new GameplayMessageHandler(this);

            TileMap = new Consoles.TileMap();
            Children.Add(TileMap);

            MessageLog = new Consoles.MessageLog();
            Children.Add(MessageLog);

            Children.Add(DebugStatsDisplay);

            Coroutines.Add(SimulationLoop(false));

            MapActors.OnRemoveActor += (a) =>
            {
                if (fallbackCameraFocusActor == a)
                    fallbackCameraFocusActor = null;
            };

            HandleMessages(Server.GetClientInitMessages());

            OnWindowResize(SadConsole.Settings.Rendering.RenderWidth, SadConsole.Settings.Rendering.RenderHeight);
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
            var result = server.Run(nextAction, 64);
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
                    userAction = TrySelectAction(keyState, CurrentPC, MapActors);
                    keyState = null;
                }
                
                var simTask = Task.Run(() => TimedRunSimulation(Server, userAction, DebugStatsDisplay));

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

        private static bool ManualFacing(SadConsole.Input.Keyboard keyboard)
            => keyboard.IsKeyDown(Keys.LeftControl) || keyboard.IsKeyDown(Keys.RightControl);

        /// <summary>
        /// Attempts to parse the user's input into a valid game action for the
        /// given actor.
        /// 
        /// If the given actor is null or the player's input does not correspond
        /// to any game action, returns null.
        /// </summary>
        private static IAction? TrySelectAction(SadConsole.Input.Keyboard info, MapActor? pc, ActorSet actors)
        {
            int dx = 0, dy = 0;
            if (info.IsKeyDown(Keys.Left)) dx -= 1;
            if (info.IsKeyDown(Keys.Right)) dx += 1;
            if (info.IsKeyDown(Keys.Up)) dy -= 1;
            if (info.IsKeyDown(Keys.Down)) dy += 1;

            if (dx != 0 || dy != 0)
            {
                // we shouldn't move in a direction we aren't facing, so if the PC
                // isn't already facing the input direction, emit a set-facing
                // action instead of a move
                if (ManualFacing(info) || (pc != null && pc.Facing != new Vec2i(dx, dy)))
                    return new Actions.Face(dx, dy);
                else
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
                ShouldReturnToTitle = true;

            if (info.IsKeyPressed(Keys.L))
                MessageLog.ToggleVisible();

            if (info.IsKeyPressed(Keys.F5))
            {
                string save = Server.ToSaveGame();
                System.IO.Directory.CreateDirectory("Saves");
                System.IO.File.WriteAllText("Saves/save.sav", save);
            }
        }

        public void SetShowGrid(bool value)
        {
            foreach (var actor in MapActors.Actors)
                actor.ShowFacingMarker = value;
        }

        public override bool ProcessKeyboard(SadConsole.Input.Keyboard info)
        {
            CheckNonGameplayHotkeys(info);
            keyState = info;

            SetShowGrid(ManualFacing(info));

            return false;
        }

        public override void Draw(TimeSpan timeElapsed)
        {
            DebugStatsDisplay.RenderDelta = timeElapsed.Milliseconds;

            foreach (var actor in MapActors.Actors)
            {
                // Actors have their position offsets reset each frame to allow
                // multiple motions to sum their individual offsets.
                actor.PositionOffset = default(Point);
            }

            Choreographer.PrepareDraw(timeElapsed);

            // capture the first player-controlled actor as a fallback
            // in the event that we don't have a waitingActor to use as a 
            // camera target (most of the time these'll be the same)
            fallbackCameraFocusActor ??= 
                waitingActor.HasValue ? 
                    MapActors[waitingActor.Value] : 
                    null;
            
            // prioritize focusing the camera on the waiting actor over the fallback
            var cameraFocus = 
                waitingActor.HasValue ?
                    MapActors[waitingActor.Value] :
                    fallbackCameraFocusActor;
                    
            if (cameraFocus != null)
                TileMap.CenterViewOn(cameraFocus);
            
            base.Draw(timeElapsed);
        }

        public void OnWindowResize(int width, int height)
        {
            TileMap.ResizePx(width, height);

            var msgW = width / MessageLog.FontSize.X * 3 / 5;
            var msgH = height / MessageLog.FontSize.Y * 1 / 5;
            int msgX = width / 2;
            int msgY = height - 40;
            MessageLog.Reposition(msgX, msgY, msgW, msgH);
        }
    }
}