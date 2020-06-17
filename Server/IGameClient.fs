namespace Server

open Server.FSharp

type IGameClient = 
    /// Is called when a previously not visible entity becomes visible.
    abstract member OnEntityAppear: Entity -> unit
    /// Is called when a entity disappears (whether due to death or some other reason).
    abstract member OnEntityVanish: Entity -> unit
    /// Is called when an entity moves by some amount (dx, dy).
    abstract member OnEntityMove: Entity -> dx: int -> dy: int -> unit
