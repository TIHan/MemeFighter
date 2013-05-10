namespace MemeFighter

open System
open System.Linq
open System.Collections.Generic
open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.Graphics
open Microsoft.Xna.Framework.Storage
open Microsoft.Xna.Framework.Input
open Microsoft.Xna.Framework.Audio
open Microsoft.Xna.Framework.Media


type Message<'T> =
    | Command of 'T
    | Event of 'T


type Process<'State> (initial: 'State, execute) =
    let mailbox = new MailboxProcessor<Message<_>> (fun agent ->
        let rec loop (state: 'State)  =
            async {
                let! msg = agent.Receive ()
                return! loop (execute state msg)
            }
        loop initial)
        
    do
        mailbox.Start ()

    member this.Send msg =
        mailbox.Post msg    


module Client =
    type Entity = { Active: bool; Position: Vector2; Texture: Texture2D }
    
    type EntityProcess = { Process: Process<Entity> }
    
    type Master = { EntityCount: int; EntityList: EntityProcess list }
    
    let private CreateEntity () =
        { Active = false; Position = new Vector2 (0.0f, 0.0f); Texture = null }
    
    let private CreateEntityProcess () =
        new Process<Entity> (CreateEntity (),(fun state msg -> state))
    
    let private CreateMaster () =
        { EntityCount = 0; EntityList = [for i in 1..1024 -> { Process = CreateEntityProcess () }] }
        
    let ClientProcess = new Process<Master> (CreateMaster (), (fun state msg ->
            match msg with
            | Command "spawn" ->
                printfn "Spawning Entity %d ..." state.EntityCount
                { EntityCount = state.EntityCount + 1; EntityList = state.EntityList }
            | _ -> state))


type Entity (game : Game, graphicsDevice : GraphicsDevice, spriteBatch : SpriteBatch) =

    let mutable _game : Game = null
    let mutable _graphicsDevice : GraphicsDevice = null
    let mutable _spriteBatch : SpriteBatch = null    
    let mutable _sprite : Texture2D = null
    let mutable _position = new Vector2 ()
    let mutable _runTime : float = 0.0    
    
    do
        _game <- game
        _graphicsDevice <- graphicsDevice
        _spriteBatch <- spriteBatch
        _sprite <- new Texture2D (_graphicsDevice, 0, 0)
        let song = _game.Content.Load<Song>("nyan_song")
        MediaPlayer.Play song
        

    member this.Position with get () = new Vector2 (_position.X, _position.Y)   
   
    member this.Move x y =
        _position.X <- x
        _position.Y <- y      
        
    member this.ChangeScale width height =
        _sprite <- new Texture2D (_graphicsDevice, width, height)
        
    member public this.ChangeSprite name =
        _sprite <- _game.Content.Load<Texture2D>(name)
        
    member this.Update (gameTime : GameTime) =
        let milliseconds = gameTime.TotalGameTime.TotalMilliseconds
        if _runTime = 0.0 then
            _runTime <- milliseconds
            
        if _runTime + 50.0 < milliseconds then
            _runTime <- milliseconds
            _position.X <- float32 (Math.Sin (float milliseconds)) * 25.0f
            
        let state = Keyboard.GetState ()
        if state.IsKeyDown (Keys.Up) then
            _position.Y <- _position.Y + 1.0f

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
    /// Initialize
    ///
    override this.Initialize () =
        base.Initialize ()        
    
    ///
    /// LoadContent   
    ///
    override this.LoadContent () =
        _spriteBatch <- new SpriteBatch (self.GraphicsDevice)
           
    ///
    /// Update 
    ///   
    override this.Update gameTime =
        match (GamePad.GetState PlayerIndex.One).Buttons.Back = ButtonState.Pressed with
        | true -> this.Exit ()
        | _ -> base.Update gameTime    
    
    ///
    /// Draw
    ///
    override this.Draw gameTime =
        _graphics.GraphicsDevice.Clear Color.CornflowerBlue
        
        if _add = 1 then
            self.CreateEntity 0.0f 0.0f 900000 0 "nyan"
            |> ignore
            _add <- 0
        
        _spriteBatch.Begin ()
        
        Seq.iter (fun (x : Entity) -> x.Update gameTime) _entities
        
        _spriteBatch.End ()
        
        base.Draw gameTime        
        
     ///
     /// Creates an entity.
     ///
     member this.CreateEntity (x : float32) y width height content : Entity =
        let entity = new Entity (self, self.GraphicsDevice, _spriteBatch)

        entity.Move x y;
        entity.ChangeScale width height;
        entity.ChangeSprite content;
        _entities.Add entity;
        entity
        