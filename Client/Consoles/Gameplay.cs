using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using SadConsole.Input;
using Server;
using Server.Data;
using Server.Logic;

using C = System.Console;

namespace Client
{
    public static partial class Consoles
    {
        public class Gameplay : SadConsole.Console, IResizeHandler
        {
            private readonly Consoles.DebugStats debugStats;
            private readonly Consoles.TileMap tilemap;
            private readonly Consoles.MessageLog messageLogConsole;
            private readonly Consoles.MiniMap minimap;

            public IClientContext ClientContext { get; }
            public GameServer Server { get; }
            public Choreographer<MapActor> Choreographer { get; } = new Choreographer<MapActor>();
            public Util.CoroutineContainer Coroutines { get; } = new Util.CoroutineContainer();

            public SadRogue.Primitives.Point MapTileSize => tilemap.TileSize;

            public ActorSet MapActors { get; } = new ActorSet();
            public GameMessageLog MessageLog { get; } = new GameMessageLog();

            public bool ShouldReturnToTitle { get; private set; } = false;

            private MapActor? CurrentPC => waitingActor.HasValue ? MapActors.Lookup(waitingActor.Value) : null;
            
            private readonly GameplayMessageHandler msgHandler;
            
            // The last valid key state. Consumed on use.
            private SadConsole.Input.Keyboard? keyState = null;

            // The actor the server is waiting on, if one exists.
            private DataHandle<Actor>? waitingActor;
            private MapActor? fallbackCameraFocusActor;

            protected override void Dispose(bool disposing)
            {
                minimap.Dispose();
                base.Dispose(disposing);
            }

            public Gameplay(IClientContext ctx, GameServer s) : base(1, 1)
            {
                ClientContext = ctx;
                Server = s;
                msgHandler = new GameplayMessageHandler(this);

                tilemap = new TileMap(this);

                messageLogConsole = new MessageLog(this, MessageLog);

                minimap = new MiniMap(this);

                debugStats = new DebugStats(this);

                Coroutines.Add(SimulationLoop(false));

                MapActors.OnAddActor += (a) => 
                {
                    tilemap.EntityLayer.Children.Add(a);
                };
                MapActors.OnRemoveActor += (a) =>
                {
                    if (fallbackCameraFocusActor == a)
                        fallbackCameraFocusActor = null;
                    tilemap.EntityLayer.Children.Remove(a);
                };

                Choreographer.OnMotionCompletion += UpdateActorsOnMinimap;

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
                    
                    var simTask = Task.Run(() => TimedRunSimulation(Server, userAction, debugStats));

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
                debugStats.UpdateDelta = timeElapsed.Milliseconds;

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
                if (info.IsKeyPressed(Keys.Z))
                {
                    return new Actions.TryAttack();
                }

                if (info.IsKeyPressed(Keys.Space))
                {
                    return new Actions.Idle();
                }

                int dx = 0, dy = 0;
                if (info.IsKeyDown(Keys.Left)) dx -= 1;
                if (info.IsKeyDown(Keys.Right)) dx += 1;
                if (info.IsKeyDown(Keys.Up)) dy -= 1;
                if (info.IsKeyDown(Keys.Down)) dy += 1;

                if (dx != 0 || dy != 0)
                {
                    if (ManualFacing(info))
                        return new Actions.Face(dx, dy);
                    else
                        return new Actions.Move(dx, dy);
                }

                return null;
            }

            private void CheckNonGameplayHotkeys(SadConsole.Input.Keyboard info)
            {
                if (info.IsKeyPressed(Keys.F1))
                    debugStats.IsVisible = !debugStats.IsVisible;

                if (info.IsKeyPressed(Keys.Escape))
                    ShouldReturnToTitle = true;

                if (info.IsKeyPressed(Keys.L))
                    messageLogConsole.ToggleVisible();

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
                tilemap.IsGridVisible = value;
            }

            public override bool ProcessKeyboard(SadConsole.Input.Keyboard info)
            {
                CheckNonGameplayHotkeys(info);
                keyState = info;

                SetShowGrid(ManualFacing(info));

                return false;
            }

            public override void Render(TimeSpan timeElapsed)
            {
                debugStats.RenderDelta = timeElapsed.Milliseconds;

                foreach (var actor in MapActors.Actors)
                {
                    // Actors have their position offsets reset each frame to allow
                    // multiple motions to sum their individual offsets.
                    actor.PositionOffset = default;
                }

                Choreographer.Update(timeElapsed);

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
                    tilemap.CenterViewOn(cameraFocus);
                
                base.Render(timeElapsed);
            }

            public void OnWindowResize(int width, int height)
            {
                tilemap.ResizeViewportPx(width, height);
                messageLogConsole.Reposition(width, height, MessageLog);
                minimap.Reposition(width, height);
            }

            public void UpdateMapTerrain(Server.Data.TileMap terrainData)
            {
                tilemap.RebuildTileMap(terrainData);
                minimap.RebuildTerrain(terrainData);
            }

            public void UpdateActorsOnMinimap()
            {
                minimap.RebuildLocalActorDisplay(MapActors.Actors, tilemap.TileSize);
            }
        }
    }
}