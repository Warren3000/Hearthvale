using Gum.Forms.Controls;
using Hearthvale.GameCode.Managers;
using Hearthvale.Scenes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using MonoGameGum;
using MonoGameLibrary;
using MonoGameLibrary.Scenes;

namespace Hearthvale;

public class Game1 : Core
{
    private Song _themeSong;

    public Game1() : base("Hearthvale", 1920, 1080, false) { }

    protected override void Initialize()
    {
        base.Initialize();

        // Initialize singleton managers
        ConfigurationManager.Initialize();
        DataManager.Initialize();

        // Start playing the background music
        Audio.PlaySong(_themeSong);

        // Initialize the Gum UI service
        InitializeGum();

        // Start the game with the title scene
        SceneManager.ChangeScene(new TitleScene());
    }

    protected override void LoadContent()
    {
        _themeSong = Content.Load<Song>("audio/theme");
    }

    protected override void Update(GameTime gameTime)
    {
        Input.Update(gameTime);
        Audio.Update();

        if (ExitOnEscape && Input.Keyboard.IsKeyDown(Keys.Escape))
        {
            Exit();
        }

        SceneManager.Update(gameTime);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        SceneManager.Draw(gameTime);
        base.Draw(gameTime);
    }

    private void InitializeGum()
    {
        GumService.Default.Initialize(this);
        GumService.Default.ContentLoader.XnaContentManager = Core.Content;
        FrameworkElement.KeyboardsForUiControl.Add(GumService.Default.Keyboard);
        FrameworkElement.GamePadsForUiControl.AddRange(GumService.Default.Gamepads);
        FrameworkElement.TabReverseKeyCombos.Add(new KeyCombo() { PushedKey = Microsoft.Xna.Framework.Input.Keys.Up });
        FrameworkElement.TabKeyCombos.Add(new KeyCombo() { PushedKey = Microsoft.Xna.Framework.Input.Keys.Down });
        GumService.Default.CanvasWidth = Core.GraphicsDevice.PresentationParameters.BackBufferWidth;
        GumService.Default.CanvasHeight = Core.GraphicsDevice.PresentationParameters.BackBufferHeight;
        GumService.Default.Renderer.Camera.Zoom = 1.0f;
    }
}