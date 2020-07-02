using System;
using System.Collections.Generic;
using System.IO;
using Server.Data;
using Server.Logic;
using Util;
using Util.Functional;

namespace Server
{
    namespace Errors
    {
        public class InvalidAI : IError
        {
            string type;
            public InvalidAI(string type) => this.type = type;
            public string Message => $"Invalid AI type '{type}'.";
        }
    }

    /// <summary>
    /// Contains the results from the server running its simulation.
    /// </summary>
    public class SimResult
    {
        public SimResult(List<IGameMessage> messages, IError? error, Actor? waitingActor)
        {
            Messages = messages;
            Error = error;
            WaitingActor = waitingActor?.ToDataHandle();
        }

        /// <summary>
        /// A list of messages for the client.
        /// </summary>
        public List<IGameMessage> Messages { get; }
        /// <summary>
        /// An optional error, if one occurred during simulation.
        /// </summary>
        public IError? Error { get; }
        /// <summary>
        /// The actor the server is waiting for input for, if one exists.
        /// </summary>
        public DataHandle<Actor>? WaitingActor { get; }
    }

    internal class RWLocked<T>
    {
        private readonly System.Threading.ReaderWriterLockSlim lockObj = new System.Threading.ReaderWriterLockSlim();

        private readonly T resource;

        public RWLocked(T obj)
        {
            resource = obj;
        }

        public U ReadResource<U>(Func<T, U> func)
        {
            lockObj.EnterReadLock();
            var rv = func(resource);
            lockObj.ExitReadLock();
            return rv;
        }

        public void ReadResource(Action<T> action)
        {
            lockObj.EnterReadLock();
            action(resource);
            lockObj.ExitReadLock();
        }

        public U WriteResource<U>(Func<T, U> func)
        {
            lockObj.EnterWriteLock();
            var rv = func(resource);
            lockObj.ExitWriteLock();
            return rv;
        }
        public void WriteResource(Action<T> action)
        {
            lockObj.EnterWriteLock();
            action(resource);
            lockObj.ExitWriteLock();
        }
    }

    public class GameServer
    {
        ClientProxy proxyClient = new ClientProxy();
        RWLocked<GameState> gameStateLock { get; }
        Actor? waitingOn = null;

        static List<IGameMessage> emptyMessageList = new List<IGameMessage>();

        internal GameServer(GameState state)
        {
            gameStateLock = new RWLocked<GameState>(state);
        }

        private static MapData TestMap(int w, int h)
        {
            var map = new MapData { tiles = new byte[w,h] };

            for (int x = 0; x < w; x++)
            {
                map.tiles[x,0] = 1;
                map.tiles[x,h-1] = 1;
            }
            for (int y = 0; y < h; y++)
            {
                map.tiles[0,y] = 1;
            }

            return map;
        }

        public static GameServer NewGame()
            => new GameServer(
                new GameState
                {
                    actors = new List<Actor>
                    {
                        new Actor
                        {
                            aiType = nameof(AIType.PlayerControlled),
                            position = new Vec2i { x=5, y=5 },
                            timeUntilAct = 20
                        },
                        new Actor
                        {
                            aiType = nameof(AIType.MoveRandomly),
                            position = new Vec2i { x=2, y=1 },
                            timeUntilAct = 21
                        },
                        new Actor
                        {
                            aiType = nameof(AIType.MoveRandomly),
                            position = new Vec2i { x=1, y=3 },
                            timeUntilAct = 20
                        },
                        new Actor
                        {
                            aiType = nameof(AIType.Idle),
                            position = new Vec2i { x=4, y=8 },
                            timeUntilAct = 10
                        }
                    },
                    map = TestMap(100, 50),
                });
        
        public static GameServer FromSaveGame(string str)
            => new GameServer(GameStateIO.LoadFromString(str));
        public static GameServer FromSaveGame(TextReader reader)
            => FromSaveGame(reader.ReadToEnd());

        public void ToSaveGame(TextWriter outStream)
            => gameStateLock.ReadResource(gameState => GameStateIO.SaveToStream(gameState, outStream));
        public string ToSaveGame()
        {
            StringWriter sw = new StringWriter();
            ToSaveGame(sw);
            return sw.ToString();
        }

        /// <summary>
        /// Queries the server for data by the given handle, and passes it to the
        /// given action if found.
        /// 
        /// This method read-locks the server during execution.
        /// 
        /// Returns whether the query was successful.
        /// </summary>
        public bool QueryData<T>(DataHandle<T> handle, Action<T> action) where T : class
            => gameStateLock.ReadResource(gs => handle.Query(gs, action));

        // given an actor, spawns a function that has that actor execute
        // an action, or return the actor if no action was performed.
        Func<IAction?, Option<Actor>> ActionExecutor(GameState state, Actor actor)
            => (action) => {
                if (action == null)
                {
                    return Option.Some(actor);
                }
                else
                {
                    actor.timeUntilAct = action.Execute(proxyClient, state, actor);
                    return Option.None;
                }
            };

        /// Performs a single step of game logic on given server, firing callbacks
        /// to clients when appropriate.
        /// 
        /// If no entities exist, does nothing.
        /// 
        /// Returns a Result containing an optional actor that must receive orders
        /// before processing can continue.
        Result<Option<Actor>> Step(GameState gameState)
        {
            // for now, a step is however long it takes for the next unit to move
            var maybeActor = TurnController.GetNextToAct(gameState.actors);
            if (maybeActor.IsNone)
                return Result.Ok<Option<Actor>>(Option.None);
            
            var actor = maybeActor.Value;
            TurnController.AdvanceTime(gameState, actor.timeUntilAct);
            
            // try to lookup AI type and execute
            return Logic.AIType.Lookup(actor.aiType)
                .ErrorIfNone(() => new Errors.InvalidAI(actor.aiType))
                .Map(aiFunc => aiFunc(gameState, actor))
                .Map(ActionExecutor(gameState, actor));
        }

        private IEnumerable<IGameMessage> clientInitMessages(GameState gameState)
        {
            yield return new Message.MapChanged(gameState.map);
            foreach (var actor in gameState.actors)
                yield return new Message.EntityAppeared(actor);
            yield break;
        }

        /// <summary>
        /// Returns an enumerable of any messages required to initialize
        /// a client to the current world state.
        /// </summary>
        public IEnumerable<IGameMessage> GetClientInitMessages() 
            => gameStateLock.ReadResource(clientInitMessages);

        /// <summary>
        /// Runs world simulation, and returns a SimResult object containing
        /// the results of the simulation and any errors that occurred.
        /// 
        /// If an actor's AI doesn't select any action, they are assumed to be
        /// controlled by the client. At this point, simulation will stop and
        /// that actor will be designated as the "waiting" actor in the SimResult.
        /// 
        /// At this point, the client must call Run() and pass in a valid action
        /// for that actor to take before simulation can continue.
        /// 
        /// If an actor is waiting and a null action is given, the actor will
        /// continue to wait, and simulation will not progress.
        /// </summary>
        /// <param name="pendingAction">An action for the waiting unit to take, or null if no action.</param>
        /// <param name="maxSteps">The maximum number of actor turns to simulate before returning, or 0 for no limit.</param>
        public SimResult Run(IAction? pendingAction, int maxSteps = 0)
        {
            // if we're waiting for an input from the user but they haven't
            // given us one, there's no need to continue
            if (waitingOn != null && pendingAction == null)
                return new SimResult(emptyMessageList, null, waitingOn);

            // otherwise lock the game state for writing and do game logic
            return gameStateLock.WriteResource(gameState => {
                // first try to execute the pending action for the waiting actor, if any
                if (waitingOn != null && pendingAction != null)
                {
                    ActionExecutor(gameState, waitingOn)(pendingAction);
                    pendingAction = null;
                    waitingOn = null;
                }

                // simulate until an actor needs user input
                int steps = 0;
                while (waitingOn == null)
                {
                    if (maxSteps > 0 && steps++ >= maxSteps)
                        break;

                    var stepResult = Step(gameState);
                    if (!stepResult.IsSuccess)
                        return new SimResult(proxyClient.PopMessages(), stepResult.Err, waitingOn);
                    
                    var maybeActor = stepResult.Value;
                    if (!maybeActor.IsNone)
                        waitingOn = maybeActor.Value;
                }

                // execution for this tick is finished, so collect messages from the
                // proxy client and return them
                return new SimResult(proxyClient.PopMessages(), null, waitingOn);
            });
        }
    }
}