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
        

type MemeFighter () as this =
    inherit Game ()

    let mutable _spriteBatch : SpriteBatch = null
    let mutable _graphics : GraphicsDeviceManager = null
    let mutable _updateTime: float = 0.0
        
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
        let keyboardState = Keyboard.GetState () 

        match _updateTime + (1.0 / 30.0 * 1000.0) - 0.1 <= gameTime.TotalGameTime.TotalMilliseconds with
        | true ->
            GameServer.Master.Send (Update (1.0f / 30.0f))
            |> ignore
            _updateTime <- gameTime.TotalGameTime.TotalMilliseconds
        | _ -> ()
        base.Update gameTime    
        
        // Placeholder for exiting.
        if keyboardState.IsKeyDown Keys.Escape then
            this.Exit ()
    
    ///
    /// Draw
    ///
    override this.Draw gameTime =
        _graphics.GraphicsDevice.Clear Color.Black
        
        let milliseconds = gameTime.TotalGameTime.TotalMilliseconds
        
        _spriteBatch.Begin ()
        
        GameClient.Master.SendAndAwait<unit> (fun x -> Draw (milliseconds, _spriteBatch, x))
         
        _spriteBatch.End ()
        //Console.WriteLine ("FPS: {0}", (1000 / gameTime.ElapsedGameTime.Milliseconds))
        base.Draw gameTime        
        