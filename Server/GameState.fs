namespace Server.FSharp

open System.IO
open Newtonsoft.Json
open System.Collections.Generic

[<Struct>]
type Vec2i = { X: int; Y: int }

type Action =
    | Idle
    | Move of delta:Vec2i

type TileType = TileType of uint8

type Map = {
    Tiles: TileType array
    }
/// A serializable structure representing a living entity in the game world.
type Entity = {
    Position: Vec2i;
    AiType: string;
    TimeUntilAct: int;
    }
/// AI consists of a stateless function that takes a game state and entity,
/// and returns an optional action to take. (If no action taken, delegates to client.)
and EntityAI = EntityAI of (GameState -> Entity -> Action option)
/// A serializable structure representing the game state, including world, entities, etc.
and GameState = {
    Entities: Entity list;
    Map: Map;
    }

module AIType =
    // this allows for reflection over AIType
    type internal Dummy = | Dummy

    let PlayerControlled = EntityAI (fun _ _ -> None)
    let DoNothing = EntityAI (fun _ _ -> Some Idle)

    let GetByName str =
        typeof<Dummy>.DeclaringType.GetProperties() 
        |> Array.filter (fun prop -> prop.Name = str)
        |> Array.tryExactlyOne
        |> Option.map (fun prop -> prop.GetValue(null, null) :?> EntityAI)

module Map =
    let create w h =
        { Tiles = Array.create (w * h) (TileType 0uy) }

module Entity =
    let create x y =
        { Position={ X=x; Y=y }; AiType=nameof AIType.DoNothing; TimeUntilAct=0 }
    let advanceTime dt entity =
        { entity with TimeUntilAct=max 0 (entity.TimeUntilAct-dt) }
    let moveBy dx dy entity =
        { entity with Position={ X=entity.Position.X + dx; Y=entity.Position.Y + dy } }
    let setTimeToAct t entity =
        { entity with TimeUntilAct=t }

module GameState =
    /// Serializes the world as JSON to the given output stream.
    let saveToStream (outStream: TextWriter) (state: GameState) =
        state |> JsonConvert.SerializeObject |> outStream.Write

    /// Deserializes a world from the given input JSON string.
    let loadFromStream =
        JsonConvert.DeserializeObject<GameState>
    
    let create () = 
        { Entities=[]; Map=Map.create 3 3 }

    let addEntity state entity =
        { state with Entities=entity :: state.Entities }

    let mutateEntity state f entity =
        let mutIfEq actor =
            if LanguagePrimitives.PhysicalEquality actor entity then
                f actor
            else
                actor
        let newEntities = state.Entities |> List.map mutIfEq
        { state with Entities=newEntities }

    let private nextToAct state = 
        if state.Entities.IsEmpty then
            Error NoEntities
        else
            state.Entities |> List.minBy (fun entity -> entity.TimeUntilAct) |> Ok
    let private advanceTime state dt =
        { state with Entities=state.Entities |> List.map (Entity.advanceTime dt) }
    let advanceTimeToNextAction state =
        state 
        |> nextToAct
        |> Result.map (fun actor -> advanceTime state actor.TimeUntilAct, actor)

    // find and execute AI to get next action. returns next actor and action
    let runAi (state, actor) =
        AIType.GetByName actor.AiType
        |> (function | Some x -> Ok x | None -> Error (InvalidAI actor.AiType))
        |> Result.map (fun (EntityAI aiFun) -> aiFun state actor)
        |> Result.map (fun action -> (actor, action))
