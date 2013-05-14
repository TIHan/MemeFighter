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

type Entity = { Active: bool; Fixture: Fixture }

type ServerMessage =
    | SpawnEntity of Texture2D
    | Update of float32
    | None
    
type ServerState = { World: World; Entities: Entity array; NextEntityId: int }                                   
            
module GameServer =        
    let inline private CreateEntityState () =
        { Active = false; Fixture = null }
        
    let inline private CreateServerState () =
        let entities = [|for i in 1..1024 -> CreateEntityState ()|]
        { World = new World (new Vector2 (0.0f, 9.82f)); Entities = entities; NextEntityId = 1 }        
        
    let mutable private CanSpawnFloor = true       
        
    let private ServerState = new Process<ServerState, ServerMessage> (CreateServerState (), (fun state msg ->
            match CanSpawnFloor with
            | true ->
                let body = BodyFactory.CreateBody (state.World, new Vector2 (0.0f, 20.0f))
                let shape = new Shapes.PolygonShape (PolygonTools.CreateRectangle(1000.0f, 4.0f), 0.0f)
                let fixture = body.CreateFixture shape
                CanSpawnFloor <- false
            | _ -> ()
            match msg with
            
            | SpawnEntity content ->   
                let id = state.NextEntityId
                let body = BodyFactory.CreateBody (state.World, new Vector2(0.0f, 0.0f))
                body.BodyType <- BodyType.Dynamic
                let shape = new Shapes.CircleShape (0.5f, 0.5f)
                let fixture = body.CreateFixture (shape, 5)
                fixture.Restitution <- 1.0f
                state.Entities.[1] <- { Active = true; Fixture = fixture }
                GameClient.Send (EntitySpawned (id, content))
                state
                
            | Update timeStep ->
                state.World.Step timeStep
                let entities = Array.filter (fun x -> x.Active = true) state.Entities
                Array.iter (fun (x : Entity) -> 
                    let position = new Vector2(ConvertUnits.ToDisplayUnits (x.Fixture.Body.Position.X), ConvertUnits.ToDisplayUnits (x.Fixture.Body.Position.Y))
                    GameClient.Send (SetEntityPosition (1, position))
                ) entities
                state
                
            | _ -> state
            ))    

    let Init () =
        ConvertUnits.SetDisplayUnitToSimUnitRatio(16.0f)
        ServerState.Start ()
        
    let Send msg =
        ServerState.Send msg
        
    let SendAndAwait<'Reply> msg : 'Reply =
        ServerState.SendAndAwait msg