using System;
using System.Collections.Generic;
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
        public Data.Actor? WaitingOn { get; private set; }

        public GameServer()
        {
            gameState = new GameState
            {
                actors = new List<Actor>
                {
                    new Actor
                    {
                        aiType = nameof(AIType.PlayerControlled),
                        position = new Vec2i { x=5, y=5 },
                        timeUntilAct = 20
                    }
                },
                map = new MapData { tiles = new byte[10,10] },
            };
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
                    action.Execute(Clients, gameState, actor);
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
            return null;
        }
    }
}