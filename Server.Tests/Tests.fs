module Tests

open Xunit
open Swensen.Unquote
open Server.FSharp
open System.IO

[<Fact>]
let ``game state must be equal after serialize and deserialize cycle`` () =
    let state = GameState.create()
    let sw = new StringWriter()
    state |> GameState.saveToStream sw
    let serialized = sw.ToString()
    let deserialized = GameState.loadFromStream serialized

    test <@ state = deserialized @>
