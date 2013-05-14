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

type LerpVector2 =
    { X1: float32;
    Y1: float32;
    X2: float32;
    Y2: float32;
    Lerp: Vector2;
    Time: float; }

type ClientEntity =
    { Active: bool;
    Position: Vector2;
    Texture: Texture2D; }

type ClientMessage =
    | EntitySpawned of int * Texture2D
    | SetEntityPosition of int * Vector2
    | Draw of float * SpriteBatch * AsyncReplyChannel<bool>
    | None   

module GameClient =

    type Master = { Entities: ClientEntity array }    
            
    let private CreateEntityState () =
        {
            Active = false;
            Position = new Vector2 (0.0f, 0.0f);
            Texture = null;
        }
        
    let private LerpDuration = (1.0f / 30.0f * 1000.0f)
    
    let inline private SpawnEntity entity texture =
        { entity with Active = true; Texture = texture }

    let inline private UpdateEntityPosition entity position =
        { entity with Position = position; }
    
    let private CreateMasterState () =
        { Entities = [|for i in 1..1024 -> CreateEntityState ()|] }      
        
    let Master = new Process<Master, ClientMessage> (CreateMasterState (), (fun state msg ->
            match msg with
            
            | EntitySpawned (id, texture) ->
                let entity = state.Entities.[id]                  
                state.Entities.[id] <- SpawnEntity entity texture                
                state
                
            | SetEntityPosition (id, position) ->
                let entity = state.Entities.[id]                
                state.Entities.[id] <- UpdateEntityPosition entity position             
                state
                
            | Draw (milliseconds, spriteBatch, channel) ->                
                Array.iter (fun x ->
                    match x.Active with
                    | false -> ()
                    | _ ->
                     
                    spriteBatch.Draw (x.Texture, x.Position, Color.White)
                ) state.Entities
                
                channel.Reply true
                state                
                
            | _ -> state
            ))
