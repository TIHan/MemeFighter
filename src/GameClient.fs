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

type ClientEntity = {
    Active: bool;
    Position: Vector2;
    PositionPrevious: Vector2;
    PositionLerp: Vector2;
    PositionLerpTime: float;
    Texture: Texture2D
}

type ClientMessage =
    | EntitySpawned of int * Texture2D
    | SetEntityPosition of int * Vector2
    | Interpolate of float
    | Draw of float * SpriteBatch * AsyncReplyChannel<bool>
    | None   

module GameClient =

    type Master = { Entities: ClientEntity array }    
            
    let private CreateEntityState () =
        { Active = false;
        Position = new Vector2 (0.0f, 0.0f);
        PositionPrevious = new Vector2 (0.0f, 0.0f);
        PositionLerp = new Vector2 (0.0f, 0.0f);
        PositionLerpTime = 0.0;
        Texture = null }
        
    let private LerpDuration = (1.0f / 30.0f * 1000.0f)
    
    let private CreateMasterState () =
        { Entities = [|for i in 1..1024 -> CreateEntityState ()|] }      
        
    let Master = new Process<Master, ClientMessage> (CreateMasterState (), (fun state msg ->
            match msg with
            
            | EntitySpawned (id, texture) ->
                let entity = state.Entities.[id]   
                
                state.Entities.[id] <- {
                    Active = true;
                    Position = entity.Position;
                    PositionPrevious = entity.PositionPrevious;
                    PositionLerp = entity.PositionLerp;
                    PositionLerpTime = entity.PositionLerpTime;
                    Texture = texture
                }
                
                state
                
            | SetEntityPosition (id, position) ->
                let entity = state.Entities.[id]
                let mutable previousPosition = entity.PositionPrevious
                let mutable currentPosition = entity.Position
                
                previousPosition.X <- currentPosition.X
                previousPosition.Y <- currentPosition.Y
                currentPosition.X <- position.X
                currentPosition.Y <- position.Y
                
                state.Entities.[id] <- {
                    Active = true;
                    Position = currentPosition;
                    PositionPrevious = previousPosition
                    PositionLerp = entity.PositionLerp;
                    PositionLerpTime = 0.0
                    Texture = entity.Texture
                }
                
                state
                
            | Interpolate milliseconds ->             
                Array.iteri (fun i x ->
                    match x.Active = true with
                    | false -> ()
                    | _ ->       
                           
                    let amount = match x.PositionLerpTime with
                                    | 0.0 -> 0.0f
                                    | (m) -> MathHelper.Clamp (float32 (milliseconds - m) / LerpDuration, 0.0f, 1.0f)
                    
                    let lerpX = MathHelper.Lerp (x.PositionPrevious.X, x.Position.X, amount)
                    let lerpY = MathHelper.Lerp (x.PositionPrevious.Y, x.Position.Y, amount)
                    let mutable currentPositionLerp = x.PositionLerp

                    currentPositionLerp.X <- lerpX
                    currentPositionLerp.Y <- lerpY
                    
                    state.Entities.[i] <- {
                        Active = x.Active;
                        Position = x.Position;
                        PositionPrevious = x.PositionPrevious;
                        PositionLerp = currentPositionLerp;
                        PositionLerpTime = match x.PositionLerpTime with | 0.0 -> milliseconds | (m) -> m;
                        Texture = x.Texture;
                    }
                    
                ) state.Entities
                    
                state
                
            | Draw (milliseconds, spriteBatch, channel) ->
                let entities = Array.filter (fun x -> x.Active = true) state.Entities
                
                Array.iter (fun x -> 
                    spriteBatch.Draw (x.Texture, x.PositionLerp, Color.White)
                ) entities
                
                channel.Reply true
                state                
                
            | _ -> state
            ))
