using Gum.Forms.Controls;
using Hearthvale.GameCode.Managers;
using Hearthvale.GameCode.Managers.Dungeon;
using Hearthvale.GameCode.Rendering;
using Hearthvale.GameCode.Utils;
using Hearthvale.Scenes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using MonoGameGum;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;
using System;

namespace Hearthvale;

public class Game1 : Core
{
    private Song _themeSong;
    private TextureAtlas _heroAtlas;
    private TextureAtlas _dungeonAtlas;

    public Game1() : base("Hearthvale", 1920, 1080, false) { }

    protected override void Initialize()
    {
        base.Initialize();

        // Initialize singleton managers
        ConfigurationManager.Initialize();
        DataManager.Initialize();

        // Initialize dungeon manager with auto-loot placement
        DungeonManager.Initialize(new AutoLootDungeonManager(
            lootTableIds: new[] { "default" },
            roomLootChance: 0.6f,
            trapChance: 0.0f
        ));

        // Start playing the background music
        Audio.PlaySong(_themeSong);

        // Initialize the Gum UI service
        InitializeGum();

        // Start the game with the title scene
        SceneManager.ChangeScene(new TitleScene());

        // Load the hero atlas for sprite analysis
        _heroAtlas = TextureAtlas.FromFile(Core.Content, "images/npc-atlas.xml");
        PreloadSpriteAnalysis();
    }

    protected override void LoadContent()
    {
        // Configure logging
        Log.EnabledAreas = LogArea.Camera;
        Log.MinLevel = LogLevel.Warn;

        Core.CameraLog = s => Log.VerboseThrottled(LogArea.Camera, s, TimeSpan.FromMilliseconds(250));
        Core.SceneLog = s => Log.Warn(LogArea.Scene, s);

        _themeSong = Content.Load<Song>("audio/theme");

        _dungeonAtlas = TextureAtlas.FromFile(Content, "images/chest-definition.xml");

        // Animation names must exist in chest-definition.xml
        DungeonLootRenderer.Initialize(
            _dungeonAtlas,
            closedIdleAnimation: "chest-wood_idle0",
            openingAnimation: "chest-wood_open",
            openedIdleAnimation: "chest-wood_idle1" // optional, can be null
        );
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
        if (_heroAtlas != null && _heroAtlas.Texture != null)
        {
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
}