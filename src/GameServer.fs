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

[<CustomEquality>]
[<CustomComparison>]
type Entity = 
    {
        Id: int;
        Fixture: Fixture
    }
    
    override this.Equals x =
        match x with
        | :? Entity as entity -> this.Id = entity.Id
        | _ -> false
        
    override this.GetHashCode () = hash this.Id
    
    interface IComparable with
        member this.CompareTo x = 
            match x with
            | :? Entity as entity -> compare this.Id entity.Id
            | _ -> invalidArg "x" "Invalid comparison"
              

type ServerMessage =
    | SpawnEntity of Texture2D * float32 * float32
    | Update of float32
    | None
    
type ServerState = { World: World; Entities: Set<Entity>; NextEntityId: int }                                   
            
module GameServer = 

    ///
    ///
    ///
    let inline private CreateDynamicFixture world x y =
        let position = Nullable<Vector2> (new Vector2(x, y))
        let body = new Body (world, position)
        let width = ConvertUnits.ToSimUnits(8)
        let height = ConvertUnits.ToSimUnits(8)
        let shape = new Shapes.PolygonShape (PolygonTools.CreateRectangle(width, height), 1.0f)
        let fixture = body.CreateFixture shape
        
        fixture.Restitution <- 0.00f
        fixture.Body.BodyType <- BodyType.Dynamic
        fixture.Body.Mass <- 1.0f
        fixture.Body.SleepingAllowed <- true
        fixture        

    ///
    ///
    ///
    let inline private SpawnEntity state x y =
        let id = state.NextEntityId
        let fixture = CreateDynamicFixture state.World x y
        
        { Id = id; Fixture = fixture }

    ///
    ///
    ///        
    let inline private CreateServerState () =
        { World = new World (new Vector2 (0.0f, 9.82f)); Entities = Set.empty; NextEntityId = 1 }        

    ///
    ///
    ///        
    let mutable private CanSpawnFloor = true   
       
    ///
    ///
    /// 
    let private ServerState = new Process<ServerState, ServerMessage> (CreateServerState (), fun state msg ->
            match CanSpawnFloor with
            | true ->
                let body = BodyFactory.CreateBody (state.World, new Vector2 (0.0f, 30.0f))
                let shape = new Shapes.PolygonShape (PolygonTools.CreateRectangle(1000.0f, 4.0f), 0.0f)
                let fixture = body.CreateFixture shape
                CanSpawnFloor <- false
            | _ -> ()
            
            match msg with
            
            | SpawnEntity (content, x, y) ->   
                let entity = SpawnEntity state x y
                EventService.Throw (EntitySpawned (entity.Id, content, ConvertUnits.ToDisplayUnits (x), ConvertUnits.ToDisplayUnits (y)))
                { state with Entities = Set.add entity state.Entities; NextEntityId = state.NextEntityId + 1 }
                
            | Update timeStep ->
                state.World.Step timeStep
                Set.iter (fun x -> 
                    let position = new Vector2 (ConvertUnits.ToDisplayUnits (x.Fixture.Body.Position.X), ConvertUnits.ToDisplayUnits (x.Fixture.Body.Position.Y))
                    let rotation = x.Fixture.Body.Rotation
                    EventService.Throw (SetEntityPosition (x.Id, position.X, position.Y, rotation))
                ) state.Entities
                state
                
            | _ -> state
    )    

    ///
    ///
    ///
    let Init () =
        ServerState.Start ()

    ///
    ///
    ///                
    let Send msg =
        ServerState.Send msg
       
    ///
    ///
    ///         
    let SendAndAwait<'Reply> msg : 'Reply =
        ServerState.SendAndAwait msg