module MemeFighter.Main

open System

[<EntryPoint>]
let main args =
    let game = new MemeFighter ()
    game.Run ()
    0

