using System;
using System.Collections.Generic;
using Server.Data;
using Server.Logic;
using Server.Util.Functional;

namespace Server
{
    public class InvalidAI : IError
    {
        string type;
        public InvalidAI(string type) => this.type = type;
        public string Message => $"Invalid AI type '{type}'.";
    }

    public class GameServer
    {
        List<IGameClient> clients = new List<IGameClient>();
        Data.GameState gameState = new Data.GameState();
        Data.Actor? waitingOn;

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
                .ErrorIfNone(() => new InvalidAI(actor.aiType))
                .Map(aiFunc => aiFunc(gameState, actor))
                .Map(ActionExecutor(actor));
        }

        public void RegisterClient(IGameClient client) => clients.Add(client);
        public void UnregisterClient(IGameClient client) => clients.Remove(client);

        /// Runs game world and fires IGameClient callbacks to all attached clients
        /// until user input is required.
        /// 
        /// Returns an IError instance if an error was encountered, else null.
        public IError? Run()
        {
            while (waitingOn == null)
            {
                var stepResult = Step();
                if (!stepResult.IsSuccess)
                    return stepResult.Err;
                
                var maybeActor = stepResult.Value;
                if (!maybeActor.IsNone)
                    waitingOn = maybeActor.Value;
            }
            return null;
        }
    }
}