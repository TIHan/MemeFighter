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
open FarseerPhysics
        

type MemeFighter () as this =
    inherit Game ()

    let DrawRate = 60.0 // FPS
    let LogicRate = 20.0 // FPS
    let LogicCheckRate = (1.0 / LogicRate * 1000.0) - 0.1
    let LogicUpdateRate = float32 (1.0 / LogicRate)
    
    let mutable _spriteBatch : SpriteBatch = null
    let mutable _graphics : GraphicsDeviceManager = null
    let mutable _updateTime = 0.0
    let mutable _textureBlock : Texture2D = null
        
    do
        _graphics <- new GraphicsDeviceManager (this)
        _graphics.IsFullScreen <- false
        this.TargetElapsedTime <- TimeSpan.FromSeconds (1.0 / DrawRate)
        this.Content.RootDirectory <- "Content"
  
    
    ///
    /// Initialize
    ///
    override this.Initialize () =
        GameClient.Init ()
        GameServer.Init ()
        //let texture = this.Content.Load<Texture2D> ("yellow_block_16x16")
        _textureBlock <- this.Content.Load<Texture2D> ("yellow_block_16x16")
            
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
        let milliseconds = gameTime.TotalGameTime.TotalMilliseconds

        match _updateTime + LogicCheckRate <= milliseconds with
        | true ->
            GameServer.Send (Update LogicUpdateRate)
            _updateTime <- milliseconds
            //GameServer.Send (SpawnEntity (_textureBlock, 20.0f, 2.0f))
        | _ -> ()
        
        base.Update gameTime    
       
        // Placeholder for exiting.
        let keyboardState = Keyboard.GetState ()
        if keyboardState.IsKeyDown Keys.Escape then
            this.Exit ()
            
        let mouseState = Mouse.GetState ()
        if mouseState.LeftButton = ButtonState.Pressed then
            GameServer.Send (SpawnEntity (_textureBlock, ConvertUnits.ToSimUnits(mouseState.X), ConvertUnits.ToSimUnits(mouseState.Y)))
    
    ///
    /// Draw
    ///
    override this.Draw gameTime =
        let milliseconds = gameTime.TotalGameTime.TotalMilliseconds
        
        // Clear the screen.
        _graphics.GraphicsDevice.Clear Color.Black
        
        // Draw a new screen.
        _spriteBatch.Begin ()     
        GameClient.SendAndAwait<unit> (fun x -> Draw (milliseconds, _spriteBatch, x))         
        _spriteBatch.End ()

        Console.WriteLine ("Render FPS: {0}", (1000 / gameTime.ElapsedGameTime.Milliseconds))
        base.Draw gameTime        
        