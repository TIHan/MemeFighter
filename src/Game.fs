namespace MemeFighter

open System
open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.Graphics
open Microsoft.Xna.Framework.Storage
open Microsoft.Xna.Framework.Input


type MemeFighter () as self =
    inherit Game ()
    
    let mutable spriteBatch : SpriteBatch = null
    let mutable graphics : GraphicsDeviceManager = null

        
    do
        graphics <- new GraphicsDeviceManager (self)
        self.Content.RootDirectory <- "Content"
        graphics.IsFullScreen <- false

                
    override this.Initialize () =
        base.Initialize ()
        
        
    override this.LoadContent () =
        spriteBatch <- new SpriteBatch (self.GraphicsDevice)

                
     override this.Update gameTime =
        match (GamePad.GetState PlayerIndex.One).Buttons.Back = ButtonState.Pressed with
        | true -> this.Exit ()
        | _ -> base.Update gameTime
    
    
    override this.Draw gameTime =
        graphics.GraphicsDevice.Clear Color.CornflowerBlue
        
        base.Draw gameTime