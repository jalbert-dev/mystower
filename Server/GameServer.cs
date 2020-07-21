using System;
using System.Collections.Generic;
using System.IO;
using Server.Data;
using Server.Database;
using Server.Logic;
using Util;
using Util.Functional;

namespace Server
{
    namespace Errors
    {
        public class InvalidAI : IError
        {
            private readonly string type;
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
        private readonly ServerContext proxyClient;
        private readonly RWLocked<GameState> gameStateLock;
        private Actor? waitingOn = null;
        public readonly Util.Database Database;
        private static readonly List<IGameMessage> emptyMessageList = new List<IGameMessage>();

        internal GameServer(GameState state, Util.Database db)
        {
            gameStateLock = new RWLocked<GameState>(state);
            Database = db;

            proxyClient = new ServerContext(Database);
        }

        private static TileMap TestMap(int w, int h)
        {
            var map = new TileMap(w, h);

            for (int x = 0; x < w; x++)
            {
                map[x,0] = 1;
                map[x,h-1] = 1;
            }
            for (int y = 0; y < h; y++)
            {
                map[0,y] = 1;
            }

            return map;
        }

        public static Result<GameServer> NewGame(Util.Database gamedb)
             => from player in Actor.FromArchetype(5, 5, 20, 1, "player", gamedb.Lookup<ActorArchetype>)
                from a in Actor.FromArchetype(2, 1, 21, 1, "squablin", gamedb.Lookup<ActorArchetype>)
                from b in Actor.FromArchetype(1, 3, 20, 1, "squablin", gamedb.Lookup<ActorArchetype>)
                from c in Actor.FromArchetype(4, 8, 10, 1, "squablin", gamedb.Lookup<ActorArchetype>)
                select 
                    new GameServer(new GameState(
                        new ValueList<Actor> { player, a, b, c },
                        TestMap(100, 50)),
                    gamedb);
        
        public static Result<GameServer> FromSaveGame(string str, Util.Database gamedb)
             => GameStateIO.LoadFromString(str, gamedb)
                    .Map(state => new GameServer(state, gamedb));
        public static Result<GameServer> FromSaveGame(TextReader reader, Util.Database gamedb)
            => FromSaveGame(reader.ReadToEnd(), gamedb);

        public void ToSaveGame(TextWriter outStream)
            => gameStateLock.ReadResource(gameState => {
                var err = GameStateIO.SaveToStream(gameState, outStream, Database);
                if (err != null)
                    Console.WriteLine(err.Message);
            });
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
                    actor.TimeUntilAct = action.Execute(proxyClient, state, actor);
                    return Option.None;
                }
            };

        Result<Option<Actor>> PerformActorAI(GameState gameState, Actor actor)
             => Result.Ok(() => TurnController.AdvanceTime(gameState, actor.TimeUntilAct))
                .Bind(_ => Logic.AIType.Lookup(actor.AiType)
                    .ErrorIfNone(() => new Errors.InvalidAI(actor.AiType))
                .Map(aiFunc => aiFunc(gameState, actor))
                .Map(ActionExecutor(gameState, actor)));

        /// Performs a single step of game logic on given server, firing callbacks
        /// to clients when appropriate.
        /// 
        /// If no actors exist, does nothing.
        /// 
        /// Returns a Result containing an optional actor that must receive orders
        /// before processing can continue.
        Result<Option<Actor>> Step(GameState gameState)
             => TurnController.GetNextToAct(gameState.Actors).Match(
                    none: () => Result.Ok<Option<Actor>>(Option.None),
                    some: actor => PerformActorAI(gameState, actor));

        private IEnumerable<IGameMessage> ClientInitMessages(GameState gameState)
        {
            yield return new Message.MapChanged(gameState.Map);
            foreach (var actor in gameState.Actors)
                yield return new Message.ActorAppeared(actor);
        }

        /// <summary>
        /// Returns an enumerable of any messages required to initialize
        /// a client to the current world state.
        /// </summary>
        public IEnumerable<IGameMessage> GetClientInitMessages() 
            => gameStateLock.ReadResource(ClientInitMessages);

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