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
    public class Gameplay : SadConsole.Console, IGameClient, IState<StateManager>
    {
        private GameServer server;
        private TileMapConsole mapLayer;
        private MessageLogConsole msgLogLayer;
        private Choreographer Choreographer { get; } = new Choreographer();
        private Util.CoroutineContainer Coroutines { get; } = new Util.CoroutineContainer();
        private DebugStatsConsole debugStats = new DebugStatsConsole();
        
        // The last valid key state. Consumed on use.
        private SadConsole.Input.Keyboard? keyState = null;

        private List<MapActor> mapActors = new List<MapActor>(64);
        private MapActor? LookupMapActor(Actor actor)
            => mapActors.FirstOrDefault(x => x.Actor == actor);

        private bool returnToTitle = false;

        // The actor the server is waiting on, if one exists.
        private Server.Data.Actor? waitingActor;

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

            Coroutines.Add(SimulationLoop(false));
        }

        public void HandleMessages(IEnumerable<IGameMessage> messages)
        {
            foreach (var msg in messages)
                msg.Dispatch(this);
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
                                                    DebugStatsConsole? debugStats = null)
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
                
                var simTask = Task.Run(() => TimedRunSimulation(server, userAction, debugStats));

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
                debugStats.IsVisible = !debugStats.IsVisible;

            if (info.IsKeyPressed(Keys.Escape))
                returnToTitle = true;

            if (info.IsKeyPressed(Keys.L))
                msgLogLayer.ToggleVisible();

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
        }

        public override bool ProcessKeyboard(SadConsole.Input.Keyboard info)
        {
            CheckNonGameplayHotkeys(info);
            keyState = info;
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