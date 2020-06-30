using System;
using System.Collections.Generic;
using System.IO;
using Server.Data;
using Server.Logic;
using Server.Util.Functional;

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

        public class NoWaitingActor : IError
        {
            public string Message => "Attempted to assign action when there's no actor waiting.";
        }

        public class CantAssignNullAction : IError
        {
            public string Message => "Attempted to assign null action to waiting unit.";
        }
    }

    public class GameServer
    {
        List<IGameClient> clients = new List<IGameClient>();
        Data.GameState gameState { get; }
        public Data.Actor? WaitingOn { get; private set; } = null;

        internal GameServer(GameState state)
        {
            gameState = state;
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
            => GameStateIO.SaveToStream(gameState, outStream);
        public string ToSaveGame()
        {
            StringWriter sw = new StringWriter();
            GameStateIO.SaveToStream(gameState, sw);
            return sw.ToString();
        }

        public IEnumerable<IGameClient> Clients => clients;

        // given an actor, spawns a function that has that actor execute
        // an action, or return the actor if no action was performed.
        Func<IAction?, Option<Actor>> ActionExecutor(Actor actor)
        {
            return (action) => {
                if (action == null)
                {
                    return Option.Some(actor);
                }
                else
                {
                    actor.timeUntilAct = action.Execute(Clients, gameState, actor);
                    return Option.None;
                }
            };
        }

        /// Performs a single step of game logic on given server, firing callbacks
        /// to clients when appropriate.
        /// 
        /// If no entities exist, does nothing.
        /// 
        /// Returns a Result containing an optional actor that must receive orders
        /// before processing can continue.
        Result<Option<Actor>> Step()
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
                .Map(ActionExecutor(actor));
        }

        public void RegisterClient(IGameClient client) 
        {
            clients.Add(client);
            client.OnMapChange(gameState.map);
            foreach (var actor in gameState.actors)
                client.OnEntityAppear(actor);
        }
        public void UnregisterClient(IGameClient client) => clients.Remove(client);

        /// Runs game world and fires IGameClient callbacks to all attached clients
        /// until user input is required.
        /// 
        /// Returns an error object if an error was encountered, else null.
        public IError? Run()
        {
            while (WaitingOn == null)
            {
                var stepResult = Step();
                if (!stepResult.IsSuccess)
                    return stepResult.Err;
                
                var maybeActor = stepResult.Value;
                if (!maybeActor.IsNone)
                    WaitingOn = maybeActor.Value;
            }
            return null;
        }

        /// <summary>
        /// If an actor is waiting for input from the client, attempts to
        /// have them execute the given action.
        /// 
        /// If no actor is waiting for input or given action is null, 
        /// returns an appropriate error and does nothing.
        /// 
        /// Returns any error that occurs.
        /// </summary>
        /// <param name="action">An IAction to execute.</param>
        /// <returns>
        /// Returns an error object if an error was encountered while executing
        /// the action, otherwise null.
        /// </returns>
        public IError? AssignActionForWaitingActor(IAction action)
        {
            if (WaitingOn == null)
                return new Errors.NoWaitingActor();
            if (action == null)
                return new Errors.CantAssignNullAction();;
            ActionExecutor(WaitingOn)(action);
            WaitingOn = null;
            return null;
        }
    }
}