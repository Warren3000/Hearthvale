using Gum.Forms.Controls;
using Hearthvale.GameCode.Managers;
using Hearthvale.GameCode.Utils;
using Hearthvale.Scenes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using MonoGameGum;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;
using MonoGameLibrary.Scenes;
using System;

namespace Hearthvale;

public class Game1 : Core
{
    private Song _themeSong;
    private TextureAtlas _heroAtlas;

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
        //// Add this to initialize the DebugManager
        //DebugManager.Initialize(GameUIManager.Instance.WhitePixel);
        // Load the hero atlas for sprite analysis
        _heroAtlas = TextureAtlas.FromFile(Core.Content, "images/npc-atlas.xml");
        // Preanalyze common sprites to avoid stuttering during gameplay
        PreloadSpriteAnalysis();
    }

    protected override void LoadContent()
    {
        //Log.EnabledAreas = LogArea.Weapon | LogArea.Player;
        //Log.EnabledAreas = LogArea.Scene | LogArea.Dungeon | LogArea.Atlas | LogArea.Weapon | LogArea.UI;
        Log.MinLevel = LogLevel.Warn;

        // Bridge engine logs to our logger without adding engine->game references
        Core.CameraLog = s => Log.VerboseThrottled(LogArea.Camera, s, TimeSpan.FromMilliseconds(250));
        Core.SceneLog = s => Log.Warn(LogArea.Scene, s);

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

    private void PreloadSpriteAnalysis()
    {
        // Analyze commonly used sprites to avoid stuttering during gameplay
        if (_heroAtlas != null && _heroAtlas.Texture != null)
        {
            // Get animation names using the extension method
            foreach (var animName in _heroAtlas.GetAnimationNames())
            {
                try
                {
                    var animation = _heroAtlas.GetAnimation(animName);
                    if (animation?.Frames?.Count > 0)
                    {
                        foreach (var frame in animation.Frames)
                        {
                            SpriteAnalyzer.GetContentBounds(frame.Texture, frame.SourceRectangle);
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error analyzing animation {animName}: {ex.Message}");
                }
            }
        }
    }

    private Texture2D CreateWhitePixelTexture()
    {
        var texture = new Texture2D(GraphicsDevice, 1, 1);
        texture.SetData(new[] { Color.White });
        return texture;
    }
}