module MemeFighter.Main

open System

[<EntryPoint>]
let main args = 
    (new MemeFighter ()).Run ()
    0

