module MemeFighter.Main

open System
open OpenTK

[<EntryPoint>]
let main args =
    Console.WriteLine (OpenTK.Configuration.RunningOnMono.ToString ())
    let game = new MemeFighter ()
    game.Run ()
    0

