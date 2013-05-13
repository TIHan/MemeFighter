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
        
        
type ClientEntity = { Active: bool; Position: Vector2; Texture: Texture2D }

type ClientMessage =
    | EntitySpawned of int * Texture2D
    | SetEntityPosition of int * Vector2
    | GetActiveEntities of AsyncReplyChannel<ClientEntity array>
    | None   

module GameClient =

    type Master = { Entities: ClientEntity array }    
            
    let private CreateEntityState () =
        { Active = false; Position = new Vector2 (0.0f, 0.0f); Texture = null }    
    
    let private CreateMasterState () =
        { Entities = [|for i in 1..1024 -> CreateEntityState ()|] }
        
    let Master = new Process<Master, ClientMessage> (CreateMasterState (), (fun state msg ->
            match msg with
            
            | EntitySpawned (id, texture) ->   
                state.Entities.[id] <- { Active = true; Position = state.Entities.[id].Position; Texture = texture }
                state
                
            | SetEntityPosition (id, position) ->
                state.Entities.[id] <- { Active = true; Position = position; Texture = state.Entities.[id].Texture }
                state
                
            | GetActiveEntities channel ->
                channel.Reply (Array.filter (fun x -> x.Active = true) state.Entities)
                state
                
            | _ -> state
            ))

    
type Entity = { Active: bool; Body: Body }

type ServerMessage =
    | SpawnEntity of int
    | GetActiveEntities of AsyncReplyChannel<Entity array>
    | UpdatePhysics
    | None                                 
            
module GameServer =
    
    type Master = { World: World; Entities: Entity array }    
        
    let private CreateEntityState () =
        { Active = false; Body = null }
        
    let private CreateMasterState () =
        { World = new World (new Vector2 (0.0f, 9.82f)); Entities = [|for i in 1..1024 -> CreateEntityState ()|] }
        
    let Master = new Process<Master, ServerMessage> (CreateMasterState (), (fun state msg ->
            match msg with
            
            | SpawnEntity id ->   
                let body = new Body (state.World)
                
                body.BodyType <- BodyType.Dynamic
                let fixture = body.CreateFixture (new Shapes.CircleShape (5.0f, 5.0f) )
                state.World.BodyList.Add body
                state.Entities.[id] <- { Active = true; Body = body }
                state
                
            | GetActiveEntities channel ->
                channel.Reply (Array.filter (fun x -> x.Active = true) state.Entities)
                state
                
            | UpdatePhysics ->
                state.World.Step (0.033333f)
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
        GameClient.Master.Send (EntitySpawned (0,  self.Content.Load<Texture2D>("nyan")))
        GameServer.Master.Send (SpawnEntity (0))
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
        let servers = GameServer.Master.Query (fun x -> ServerMessage.GetActiveEntities x)
        Array.iter (fun (x : Entity) -> 
            GameClient.Master.Send (SetEntityPosition (0, x.Body.Position))
        ) servers
                 
        GameServer.Master.Send (UpdatePhysics)
        
        base.Update gameTime    
    
    ///
    /// Draw
    ///
    override this.Draw gameTime =
        _graphics.GraphicsDevice.Clear Color.Black
        
        _spriteBatch.Begin ()
        
        let entities = GameClient.Master.Query (fun x -> ClientMessage.GetActiveEntities x)
        Array.iter (fun (x : ClientEntity) -> 
            _spriteBatch.Draw (x.Texture, x.Position, Color.White)
        ) entities
         
        _spriteBatch.End ()
        
        //Console.WriteLine ("FPS: {0}", (1000 / gameTime.ElapsedGameTime.Milliseconds))
        base.Draw gameTime        
        