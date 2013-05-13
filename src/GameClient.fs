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
    PositionLerp: LerpVector2;
    Texture: Texture2D; }

type ClientMessage =
    | EntitySpawned of int * Texture2D
    | SetEntityPosition of int * Vector2
    | Interpolate of float
    | Draw of float * SpriteBatch * AsyncReplyChannel<bool>
    | None   

module GameClient =

    type Master = { Entities: ClientEntity array }    
            
    let private CreateEntityState () =
        {
            Active = false;
            Position = new Vector2 (0.0f, 0.0f);
            PositionLerp = { X1 = 0.0f; Y1 = 0.0f; X2 = 0.0f; Y2 = 0.0f; Lerp = Vector2 (0.0f, 0.0f); Time = 0.0 }
            Texture = null;
        }
        
    let private LerpDuration = (1.0f / 30.0f * 1000.0f)
    
    let inline private UpdateEntityLerp entity x y time =
        { entity with PositionLerp = {
                                        entity.PositionLerp with
                                            X1 = match time with | 0.0 -> entity.PositionLerp.X2 | _ -> entity.PositionLerp.X1;
                                            Y1 = match time with | 0.0 -> entity.PositionLerp.Y2 | _ -> entity.PositionLerp.Y1;
                                            X2 = match time with | 0.0 -> entity.Position.X | _ -> entity.PositionLerp.X2;
                                            Y2 = match time with | 0.0 -> entity.Position.Y | _ -> entity.PositionLerp.Y2;
                                            Lerp = new Vector2 (x, y);
                                            Time = time
            }
        }
    
    let private CreateMasterState () =
        { Entities = [|for i in 1..1024 -> CreateEntityState ()|] }      
        
    let Master = new Process<Master, ClientMessage> (CreateMasterState (), (fun state msg ->
            match msg with
            
            | EntitySpawned (id, texture) ->
                let entity = state.Entities.[id]                  
                state.Entities.[id] <- { entity with Active = true; Texture = texture }                
                state
                
            | SetEntityPosition (id, position) ->
                let entity = state.Entities.[id]                
                state.Entities.[id] <- { entity with Position = position }               
                state
                
            | Interpolate milliseconds ->             
                Array.iteri (fun i x ->
                    match x.Active with
                    | false -> ()
                    | _ ->       
                           
                    let amount = match x.PositionLerp.Time with
                                    | 0.0 -> 0.0f
                                    | (m) -> MathHelper.Clamp (float32 (milliseconds - m) / LerpDuration, 0.0f, 1.0f)
                    
                    let lerpX = MathHelper.Lerp (x.PositionLerp.X1, x.PositionLerp.X2, amount)
                    let lerpY = MathHelper.Lerp (x.PositionLerp.Y1, x.PositionLerp.Y2, amount)
                    let time = match (x.PositionLerp.Time, amount) with
                                | (0.0, _) -> milliseconds 
                                | (m, 1.0f) -> 0.0
                                | (m, _) -> m                                        

                    state.Entities.[i] <- UpdateEntityLerp x lerpX lerpY time                
                ) state.Entities
                    
                state
                
            | Draw (milliseconds, spriteBatch, channel) ->                
                Array.iter (fun x ->
                    match x.Active with
                    | false -> ()
                    | _ ->
                     
                    spriteBatch.Draw (x.Texture, x.PositionLerp.Lerp, Color.White)
                ) state.Entities
                
                channel.Reply true
                state                
                
            | _ -> state
            ))
