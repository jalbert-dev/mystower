using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Server.Data;
using Server.Database;
using Server.Logic;
using Server.Random;
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

        private static TileMap TestMap(int w, int h, IRandomSource rng)
        {
            var map = new TileMap(w, h, 255);

            var rooms = new List<(Vec2i pos, Vec2i size)>();
            bool vacant(int px, int py, int sx, int sy)
            {
                px = px < 0 ? 0 : px;
                py = py < 0 ? 0 : py;

                return !rooms.Any(room => {
                    return (px <= room.pos.x + room.size.x && px + sx >= room.pos.x &&
                            py <= room.pos.y + room.size.y && py + sy >= room.pos.y);
                });
            }
            static Vec2i randomPointOnPerimeter(IRandomSource rng, (Vec2i pos, Vec2i size) room)
                => room.pos + rng.Next(0, 3) switch
                {
                    0 => (0, rng.Next(1, room.size.y-2)),
                    1 => (room.size.x-1, rng.Next(1, room.size.y-2)),
                    2 => (rng.Next(1, room.size.x-2), 0),
                    3 => (rng.Next(1, room.size.x-2), room.size.y-1),
                    _ => (0, 0),
                };

            for (int i = 0; i < 7; i++)
            {
                for (int _ = 0; _ < 100; _++)
                {
                    int sizeX = rng.Next(6, w / 4);
                    int sizeY = rng.Next(6, h / 4);
                    
                    int posX = rng.Next(1, w-1 - sizeX);
                    int posY = rng.Next(1, h-1 - sizeY);

                    if (vacant(posX-1, posY-1, sizeX+2, sizeY+2))
                    {
                        rooms.Add(((posX, posY), (sizeX, sizeY)));
                        break;
                    }
                }
            }

            // carve out allocated rooms
            foreach (var (pos, size) in rooms)
            {
                // first fill with wall
                for (int i = 0; i < size.x; i++)
                    for (int j = 0; j < size.y; j++)
                        map[pos.x+i, pos.y+j] = 1;

                // then dig the floor out of the center
                for (int i = 1; i < size.x-1; i++)
                    for (int j = 1; j < size.y-1; j++)
                        map[i+pos.x, j+pos.y] = 0;
            }

            var nodes = rng.Shuffle(Enumerable.Range(0, rooms.Count)).ToList();
            List<(int from, int to)> edges = nodes.Zip(nodes.Skip(1), ValueTuple.Create).ToList();

            static IEnumerable<Vec2i>? bfs(TileMap map, Vec2i src, Vec2i dst)
            {
                var toVisit = new Queue<Vec2i>();
                var parents = new Dictionary<Vec2i, Vec2i>();

                toVisit.Enqueue(src);

                while (toVisit.Count > 0)
                {
                    var current = toVisit.Dequeue();

                    foreach (var child in map
                            .SurroundingTiles(current.x, current.y, false)
                            .Where(x => !parents.ContainsKey(x.pos))
                            .Where(x => map[x.pos] > 1))
                    {
                        toVisit.Enqueue(child.pos);
                        parents.Add(child.pos, current);

                        if (child.pos == dst)
                        {
                            IList<Vec2i> path = new List<Vec2i>();
                            Vec2i ptr = dst;
                            while (ptr != src)
                            {
                                path.Add(ptr);
                                ptr = parents[ptr];
                            }
                            path.Add(src);
                            return path.Reverse();
                        }
                    }
                }

                return null;
            }

            foreach (var (srcRoom, dstRoom) in edges.Select(x => (rooms[x.from], rooms[x.to])))
            {
                for (int _ = 0; _ < 100; _++)
                {
                    // get random tiles on perimeters of src and dst
                    var srcTile = randomPointOnPerimeter(rng, srcRoom);
                    var dstTile = randomPointOnPerimeter(rng, dstRoom);

                    map[srcTile] = 2;
                    map[dstTile] = 2;

                    // use BFS to find path from srcEntrance to dstEntrance
                    var path = bfs(map, srcTile, dstTile);

                    if (path == null)
                        continue;
                    
                    // set all tiles on path to road
                    foreach (var p in path)
                        if (map[p] != 0)
                            map[p] = 2;
                    break;
                }
            }

            // finally replace all placeholder tiles with wall
            for (int i = 0; i < map.Width; i++)
                for (int j = 0; j < map.Height; j++)
                    if (map[i,j] == 255)
                        map[i,j] = 1;

            foreach (var (roomPos, roomSize) in rooms)
                map.DefineRoom(roomPos, roomSize);

            return map;
        }

        private static Result<ValueList<Actor>> PopulateMap(Func<string, Result<ActorArchetype>> lookupArchetype, TileMap map, IRandomSource rng)
        {
            var actors = new ValueList<Actor>();

            var playerRoom = rng.PickFrom(map.Rooms);

            var playerStart = rng.PickFrom(Map.GetUnoccupiedTilesInRoom(playerRoom, map, actors));
            var player = Actor.FromArchetype(playerStart.x, playerStart.y, 0, 1, "player", lookupArchetype);
            if (!player.IsSuccess)
                return Result.Error(player.Err);

            actors.Add(player.Value);

            foreach (var r in map.Rooms.Where(x => !x.Equals(playerRoom)))
            {
                var enemyCount = rng.Next(1, 2);
                for (int i = 0; i < enemyCount; i++)
                {
                    var tile = rng.PickFrom(Map.GetUnoccupiedTilesInRoom(r, map, actors));
                    var actor = Actor.FromArchetype(tile.x, tile.y, 0, 1, "squablin", lookupArchetype);
                    
                    if (!actor.IsSuccess)
                        return Result.Error(actor.Err);

                    actors.Add(actor.Value);
                }
            }

            return Result.Ok(actors);
        }

        public static Result<GameServer> NewGame(Util.Database gamedb)
             => from rng in Result.Ok(new LCG64RandomSource())
                let map = TestMap(100, 50, rng)
                from actorList in PopulateMap(gamedb.Lookup<ActorArchetype>, map, rng)
                select 
                    new GameServer(new GameState(actorList, map, rng),
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