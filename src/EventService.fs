namespace MemeFighter

open System
open System.Linq
open System.Collections.Generic
open System.Diagnostics
open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.Net

type ServiceState = { NetworkSession: NetworkSession }

module EventService =

    let inline private CreateServiceState () =
        { NetworkSession = null }

    let private ServiceState = new Process<ServiceState, _> (CreateServiceState (), fun state msg ->
            match msg with          
            | _ ->
                GameClient.Send msg
                state
    )
    
    let Init () =
        ServiceState.Start ()
            
    let Throw event =
        ServiceState.Send event
        
