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

type Process<'State, 'Msg> (initial: 'State, execute) =
    let mailbox = new MailboxProcessor<'Msg> (fun agent ->
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
        
    member this.Query query =
        mailbox.PostAndReply query
        
        
type Entity = { Active: bool; Position: Vector2; Texture: Texture2D }   
    
type Master = { Entities: Entity array }    
type MasterMessage =
    | EntitySpawned of int * Texture2D
    | GetActiveEntities of AsyncReplyChannel<Entity array>
    | None


module Client =    
    let private CreateEntityState () =
        { Active = false; Position = new Vector2 (0.0f, 0.0f); Texture = null }    
    
    let private CreateMasterState () =
        { Entities = [|for i in 1..1024 -> CreateEntityState ()|] }
        
    let Master = new Process<Master, MasterMessage> (CreateMasterState (), (fun state msg ->
            match msg with
            
            | EntitySpawned (id, texture) ->   
                state.Entities.[id] <- { Active = true; Position = state.Entities.[id].Position; Texture = texture }
                state
                
            | GetActiveEntities channel ->
                channel.Reply (Array.filter (fun x -> x.Active = true) state.Entities)
                state
                
            | _ -> state
            ))


(*type Entity (game : Game, graphicsDevice : GraphicsDevice, spriteBatch : SpriteBatch) =

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
            
        if _runTime + 25.0 < milliseconds then
            _runTime <- milliseconds
            _position.X <- _position.X + 1.0f
            _position.Y <- float32 (Math.Sin (float milliseconds)) * 20.0f
            
        let state = Keyboard.GetState ()
        if state.IsKeyDown (Keys.Up) then
            _position.Y <- _position.Y + 1.0f

        _spriteBatch.Draw (_sprite, _position, Color.White)*)
        

type MemeFighter () as self =
    inherit Game ()

    let mutable _spriteBatch : SpriteBatch = null
    let mutable _graphics : GraphicsDeviceManager = null
        
    do
        _graphics <- new GraphicsDeviceManager (self)
        self.Content.RootDirectory <- "Content"
        _graphics.IsFullScreen <- false
    
    ///
    /// Initialize
    ///
    override this.Initialize () =
        Client.Master.Send (EntitySpawned (0,  self.Content.Load<Texture2D>("nyan")))
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
        base.Update gameTime    
    
    ///
    /// Draw
    ///
    override this.Draw gameTime =
        _graphics.GraphicsDevice.Clear Color.Black
        
        _spriteBatch.Begin ()
        
        let entities = Client.Master.Query (fun x -> GetActiveEntities x)
        Array.iter (fun (x : Entity) -> 
            _spriteBatch.Draw (x.Texture, x.Position, Color.White)
        ) entities
         
        _spriteBatch.End ()
        
        Console.WriteLine ("FPS: {0}", (1000 / gameTime.ElapsedGameTime.Milliseconds))
        base.Draw gameTime        
        