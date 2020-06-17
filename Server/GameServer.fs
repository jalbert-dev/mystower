namespace Server.FSharp

/// Accepts clients and performs game logic, informing clients of any changes
/// in game state that may need to be reflected.
type GameServer = {
    Clients: Server.IGameClient list;
    State: GameState;
    WaitingForInput: Entity option;
    }

module GameServer =
    // executes given action for given actor on given server, emitting
    // signals to clients as necessary
    // returns the new game state and actor being waited on, if exists
    let private executeAction server (actor, action) =
        let waitForInputFrom actor = server.State, Some actor
        let continueSignal newState = (newState, None)

        match action with
        | None -> waitForInputFrom actor
        | Some act ->
            match act with
            | Idle -> continueSignal server.State
            | Move {X=dx; Y=dy} -> 
                let newState = actor |> GameState.mutateEntity server.State (Entity.moveBy dx dy)
                for client in server.Clients do
                    client.OnEntityMove actor dx dy
                continueSignal newState

    let addEntity server entity =
        for client in server.Clients do
            client.OnEntityAppear entity
        { server with State=GameState.addEntity server.State entity }

    /// Performs a single step of game logic on given server, firing callbacks
    /// to clients when appropriate.
    /// 
    /// On success, returns the new state and the entity being waited on,
    /// if exists.
    /// On failure, returns the last state and the error type.
    let step server = 
        // our "step" consists of accelerating time to the next unit needing to act
        server.State
        |> GameState.advanceTimeToNextAction
        |> Result.bind GameState.runAi
        |> Result.map (executeAction server)

    /// Runs game world and fires IGameClient callbacks to all attached clients
    /// until user input is required.
    let rec run server =
        match server |> step with
        | Ok (newState, None) ->
            run { server with State=newState }
        | Ok (newState, entity) ->
            Ok { server with State=newState; WaitingForInput=entity }
        | Error errType ->
            Error (server.State, errType)
    