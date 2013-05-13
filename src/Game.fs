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
        

type MemeFighter () as this =
    inherit Game ()

    let mutable _spriteBatch : SpriteBatch = null
    let mutable _graphics : GraphicsDeviceManager = null
    let mutable _updateCount = 1
        
    do
        _graphics <- new GraphicsDeviceManager (this)
        this.Content.RootDirectory <- "Content"
        _graphics.IsFullScreen <- false
       
    
    ///
    /// Initialize
    ///
    override this.Initialize () =
        GameServer.Init ()
        GameClient.Master.Send (EntitySpawned (0,  this.Content.Load<Texture2D>("nyan")))
        GameServer.Master.Send (SpawnEntity (0))
        base.Initialize ()        
    
    ///
    /// LoadContent   
    ///
    override this.LoadContent () =
        _spriteBatch <- new SpriteBatch (this.GraphicsDevice)
           
    ///
    /// Update 
    ///   
    override this.Update gameTime =         
        match _updateCount >= 1 with
        | true ->
            GameServer.Master.Send (Update (1.0f / 30.0f))
            _updateCount <- 0
        | _ ->
            _updateCount <- _updateCount + 1
        base.Update gameTime    
    
    ///
    /// Draw
    ///
    override this.Draw gameTime =
        _graphics.GraphicsDevice.Clear Color.Black
        
        let milliseconds = gameTime.TotalGameTime.TotalMilliseconds
        
        GameClient.Master.Send (Interpolate milliseconds)
        
        _spriteBatch.Begin ()
        
        GameClient.Master.SendAndReply (fun x -> Draw (milliseconds, _spriteBatch, x))
        |> ignore
         
        _spriteBatch.End ()
        Console.WriteLine ("FPS: {0}", (1000 / gameTime.ElapsedGameTime.Milliseconds))
        base.Draw gameTime        
        