namespace Server.FSharp

type ServerError =
    | NoEntities
    | InvalidAI of id:string