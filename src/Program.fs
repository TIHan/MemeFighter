module MemeFighter.Main

open System

[<EntryPoint>]
let main args = 
    (new MemeFighter ()).Run ()
    //Console.ReadLine () |> ignore
    0

