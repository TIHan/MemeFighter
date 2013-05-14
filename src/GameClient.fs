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
type ClientEntity =
    {
        Id: int;
        Position: Vector2;
        Texture: Texture2D;
    }
    
    override this.Equals x =
        match x with
        | :? ClientEntity as entity -> this.Id = entity.Id
        | _ -> false
        
    override this.GetHashCode () = hash this.Id
    
    interface IComparable with
        member this.CompareTo x = 
            match x with
            | :? ClientEntity as entity -> compare this.Id entity.Id
            | _ -> invalidArg "x" "Invalid comparison"  
    

type ClientMessage =
    | EntitySpawned of int * Texture2D * float32 * float32
    | EntityPositionsUpdated of (int * int * int) list
    | SetEntityPosition of int * Vector2
    | Draw of float * SpriteBatch * AsyncReplyChannel<unit>
    | None   
    
type ClientState = { Entities: Set<ClientEntity> }  

module GameClient =                  
    let inline private SpawnEntity id x y texture =
        {
            Id = id;
            Position = new Vector2 (x, y);
            Texture = texture;
        }

    let inline private UpdateEntityPosition entity position =
        { entity with Position = position; }
    
    let inline private CreateClientState () =
        { Entities = Set.empty }                               
        
    let private ClientState = new Process<ClientState, ClientMessage> (CreateClientState (), (fun state msg ->
            match msg with
            
            | EntitySpawned (id, texture, x, y) ->              
                { Entities = Set.add (SpawnEntity id x y texture) state.Entities }
                
            | SetEntityPosition (id, position) ->
                let entity = Set.filter (fun x -> x.Id = id) state.Entities |> Set.minElement
                match entity = Unchecked.defaultof<_> with
                | false -> { Entities = Set.remove entity state.Entities |> Set.add (UpdateEntityPosition entity position) }   
                | _ -> state         
                
            | Draw (milliseconds, spriteBatch, channel) ->
                Set.iter (fun x ->
                    spriteBatch.Draw (x.Texture, x.Position, Color.Yellow)
                ) state.Entities
                           
                channel.Reply ()
                state                
                
            | _ -> state
            ))
            
    let Init () =
        ConvertUnits.SetDisplayUnitToSimUnitRatio(16.0f)
        ClientState.Start ()
            
    let Send msg =
        ClientState.Send msg
        
    let SendAndAwait<'Reply> msg : 'Reply =
        ClientState.SendAndAwait msg
