namespace MemeFighter

open System
open System.Linq
open System.Collections.Generic
open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.Graphics
open Microsoft.Xna.Framework.Storage
open Microsoft.Xna.Framework.Input


type Entity (game : Game, graphicsDevice : GraphicsDevice, spriteBatch : SpriteBatch) =

    let mutable _game : Game = null
    let mutable _graphicsDevice : GraphicsDevice = null
    let mutable _spriteBatch : SpriteBatch = null    
    let mutable _sprite : Texture2D = null
    let mutable _position = new Vector2 ()    
    
    do
        _game <- game
        _graphicsDevice <- graphicsDevice
        _spriteBatch <- spriteBatch
        _sprite <- new Texture2D (_graphicsDevice, 0, 0)

    member this.Position with get () = new Vector2 (_position.X, _position.Y)   
   
    member this.Move x y =
        _position.X <- x
        _position.Y <- y      
        
    member this.ChangeScale width height =
        _sprite <- new Texture2D (_graphicsDevice, width, height)
        
    member public this.ChangeSprite name =
        _sprite <- _game.Content.Load<Texture2D>(name)
        
    member this.Update (gameTime : GameTime) =
        _spriteBatch.Draw (_sprite, _position, Color.White)
        

type MemeFighter () as self =
    inherit Game ()
    
    let mutable _entities = new List<Entity> ()
    let mutable _spriteBatch : SpriteBatch = null
    let mutable _graphics : GraphicsDeviceManager = null
    let mutable _add = 1
        
    do
        _graphics <- new GraphicsDeviceManager (self)
        self.Content.RootDirectory <- "Content"
        _graphics.IsFullScreen <- false
    
    ///
    ///
    /// Initialize
    override this.Initialize () =
        base.Initialize ()        
    
    ///
    ///
    /// LoadContent   
    override this.LoadContent () =
        _spriteBatch <- new SpriteBatch (self.GraphicsDevice)
           
    ///
    ///
    /// Update    
    override this.Update gameTime =
        match (GamePad.GetState PlayerIndex.One).Buttons.Back = ButtonState.Pressed with
        | true -> this.Exit ()
        | _ -> base.Update gameTime    
    
    ///
    ///
    /// Draw
    override this.Draw gameTime =
        _graphics.GraphicsDevice.Clear Color.CornflowerBlue
        
        if _add = 1 then
            self.CreateEntity (1.0f, 1.0f, 15, 15, "nyan")
            _add <- 0
        
        _spriteBatch.Begin ()
        
        Seq.iter (fun (x : Entity) -> x.Update gameTime) _entities
        
        _spriteBatch.End ()
        
        base.Draw gameTime        
        
     ///
     ///
     /// Creates an entity.
     member this.CreateEntity (x, y, width, height, content) =
        let entity = new Entity (self, self.GraphicsDevice, _spriteBatch)

        entity.Move x y;
        entity.ChangeScale width height;
        entity.ChangeSprite content;
        _entities.Add entity;
        ()
        