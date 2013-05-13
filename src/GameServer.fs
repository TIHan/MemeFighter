namespace MemeFighter

open System
open System.Linq
open System.Collections.Generic
open System.Diagnostics
open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.Graphics
open Microsoft.Xna.Framework.Storage
open Microsoft.Xna.Framework.Input
open Microsoft.Xna.Framework.Audio
open Microsoft.Xna.Framework.Media
open FarseerPhysics.Common
open FarseerPhysics.Factories
open FarseerPhysics.Dynamics
open FarseerPhysics
open FarseerPhysics.Collision
open FarseerPhysics.Controllers

type Entity = { Active: bool; Body: Body }

type ServerMessage =
    | SpawnEntity of int
    | Update of float32
    | None                                 
            
module GameServer =
    
    type Master = { World: World; Entities: Entity array }    
        
    let private CreateEntityState () =
        { Active = false; Body = null }
        
    let private CreateMasterState () =
        { World = new World (new Vector2 (0.0f, 9.82f)); Entities = [|for i in 1..1024 -> CreateEntityState ()|] }
        
    let Master = new Process<Master, ServerMessage> (CreateMasterState (), (fun state msg ->
            match msg with
            
            | SpawnEntity id ->   
                let body = new Body (state.World)
                
                body.BodyType <- BodyType.Dynamic
                let fixture = body.CreateFixture (new Shapes.CircleShape (5.0f, 5.0f) )
                state.World.BodyList.Add body
                state.Entities.[id] <- { Active = true; Body = body }
                state
                
            | Update timeStep ->
                state.World.Step timeStep
                let entities = Array.filter (fun x -> x.Active = true) state.Entities
                Array.iter (fun (x : Entity) -> 
                    GameClient.Master.Send (SetEntityPosition (0, x.Body.Position))
                ) entities
                state
                
            | _ -> state
            ))    
