using System;
using Hearthvale.UI;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameGum;
using MonoGameGum.Forms.Controls;
using MonoGameGum.GueDeriving;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;
using MonoGameLibrary.Input;
using MonoGameLibrary.Scenes;
using MonoGame.Extended.Tiled;
using MonoGame.Extended.Tiled.Renderers;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using System.Threading;

namespace Hearthvale.Scenes;

public class GameScene : Scene
{
    // Defines the slime animated sprite.
    //private AnimatedSprite _slime;

    // Defines the bat animated sprite.
    private AnimatedSprite _bat;

    // Tracks the position of the slime.
    //private Vector2 _slimePosition;
    private Vector2 _heroPosition;

    private Vector2 _playerStart;

    AnimatedSprite _hero;

    private NpcManager _npcManager;

    private bool _facingRight = true;
    private string _currentAnimationName = "Mage_Idle";

    // Speed multiplier when moving.
    private const float MOVEMENT_SPEED = 3.0f;
    public float MovementSpeed => MOVEMENT_SPEED;

    // Tracks the position of the bat.
    private Vector2 _batPosition;

    // Tracks the velocity of the bat.
    private Vector2 _batVelocity;

    // Defines the tilemap to draw.
    private Tilemap _tilemap;

    // Defines the bounds of the room that the slime and bat are contained within.
    private Rectangle _roomBounds;

    // The sound effect to play when the bat bounces off the edge of the screen.
    private SoundEffect _bounceSoundEffect;

    // The sound effect to play when the slime eats a bat.
    private SoundEffect _collectSoundEffect;

    // The SpriteFont Description used to draw text
    private SpriteFont _font;

    SpriteFont _debugFont;

    // Tracks the players score.
    private int _score;

    // Defines the position to draw the score text at.
    private Vector2 _scoreTextPosition;

    // Defines the origin used when drawing the score text.
    private Vector2 _scoreTextOrigin;

    // A reference to the pause panel UI element so we can set its visibility
    // when the game is paused.
    private Panel _pausePanel;

    // A reference to the resume button UI element so we can focus it
    // when the game is paused.
    private AnimatedButton _resumeButton;

    // The UI sound effect to play when a UI event is triggered.
    private SoundEffect _uiSoundEffect;

    // Reference to the texture atlas that we can pass to UI elements when they
    // are created.
    private TextureAtlas _atlas;

    private TextureAtlas _heroAtlas;

    private TiledMap _map;
    private TiledMapRenderer _mapRenderer;
    int _mapWidthInPixels;
    int _mapHeightInPixels;

    Viewport _viewport;
    Camera2D _camera;
    private InputHandler _inputHandler;

    private Texture2D _whitePixel;
    



    public override void Initialize()
    {
        // LoadContent is called during base.Initialize().
        base.Initialize();

        // During the game scene, we want to disable exit on escape. Instead,
        // the escape key will be used to return back to the title screen
        Core.ExitOnEscape = false;

        Rectangle screenBounds = Core.GraphicsDevice.PresentationParameters.Bounds;

        _roomBounds = new Rectangle(0, 0, _map.Width * 16, _map.Height * 16);
        
        // Initial slime position will be the center tile of the tile map.
        int centerRow = _map.Height / 2;
        int centerColumn = _map.Width / 2;

        _heroPosition = new Vector2(centerColumn * _map.TileWidth, centerRow * _map.TileHeight);

        // Initial bat position will the in the top left corner of the room.
        _batPosition = new Vector2(_roomBounds.Left, _roomBounds.Top);

        // Set the position of the score text to align to the left edge of the
        // room bounds, and to vertically be at the center of the first tile.
        _scoreTextPosition = new Vector2(_roomBounds.Left, _map.TileHeight * 0.5f);

        // Set the origin of the text so it is left-centered.
        float scoreTextYOrigin = _font.MeasureString("Score").Y * 0.5f;
        _scoreTextOrigin = new Vector2(0, scoreTextYOrigin);

        InitializeUI();

        _camera = new Camera2D(Core.GraphicsDevice.Viewport);
        _camera.Zoom = 3.0f; // Scale the whole world by 4x

        _viewport = Core.GraphicsDevice.Viewport;

        Debug.WriteLine($"Viewport size: {_viewport.Width} x {_viewport.Height}");

        Debug.WriteLine($"Map size: {_map.Width}x{_map.Height} tiles");
        Debug.WriteLine($"Map pixel size: {_map.Width * _map.TileWidth}x{_map.Height * _map.TileHeight}");
        Debug.WriteLine($"Camera position: {_camera.Position}");
        Debug.WriteLine($"Zoom: {_camera.Zoom}");

        // Initialize InputHandler with callbacks
        _inputHandler = new InputHandler(
            _camera,
            movementSpeed: MOVEMENT_SPEED,
            pauseGameCallback: PauseGame,
            moveHeroCallback: MoveHero,
            spawnNpcCallback: () => _npcManager.SpawnNPC("merchant", _heroPosition + new Vector2(32, 0)),
            quitCallback: () => Core.ChangeScene(new TitleScene())
        );

    }

    public override void LoadContent()
    {
        // Create the texture atlas from the XML configuration file.
        _atlas = TextureAtlas.FromFile(Core.Content, "images/atlas-definition.xml");

        // Load the player slime animation from the atlas

        _heroAtlas = TextureAtlas.FromFile(Core.Content, "images/npc-atlas.xml");
        _hero = _heroAtlas.CreateAnimatedSprite("Mage_Idle");

        // Optionally set scale if needed (4x for example)
        _hero.Scale = new Vector2(1f, 1f);

        // Load the map using MonoGame.Extended's TiledMap loader
        _map = Game1.Content.Load<TiledMap>("Tilesets/DampDungeons/Tiles/DungeonMap");
        _mapRenderer = new TiledMapRenderer(Game1.GraphicsDevice, _map);

        _roomBounds = new Rectangle(0, 0, _map.Width * 16, _map.Height * 16);

        // Initialize NpcManager once _roomBounds and _heroAtlas are ready
        _npcManager = new NpcManager(_heroAtlas, _roomBounds);

        // Load NPCs using NpcManager
        var entityLayer = _map.ObjectLayers.FirstOrDefault(layer => layer.Name == "Entities");
        if (entityLayer != null)
        {
            _npcManager.LoadNPCs(entityLayer.Objects);
        }


        _mapWidthInPixels = _map.WidthInPixels;
        _mapHeightInPixels = _map.HeightInPixels;

        // Load sounds
        _bounceSoundEffect = Content.Load<SoundEffect>("audio/bounce");
        _collectSoundEffect = Content.Load<SoundEffect>("audio/collect");
        _uiSoundEffect = Core.Content.Load<SoundEffect>("audio/ui");

        // Load the font.
        _font = Core.Content.Load<SpriteFont>("fonts/04B_30");
        _debugFont = Content.Load<SpriteFont>("fonts/DebugFont");

        // White pixel texture used for debug drawing
        _whitePixel = new Texture2D(Core.GraphicsDevice, 1, 1);
        _whitePixel.SetData(new[] { Color.White });

        // Find the player spawn point from Tiled object layer (uses Class == "Player")
        FindPlayerSpawnPoint();
        _heroPosition = _playerStart;
    }

    public override void Update(GameTime gameTime)
    {
        
        // Ensure the UI is always updated
        GumService.Default.Update(gameTime);

        // If the game is paused, do not continue
        if (_pausePanel.IsVisible)
        {
            return;
        }

        _hero.Update(gameTime);

        _npcManager.Update(gameTime);

        _mapRenderer.Update(gameTime);

        // Ensure _viewport is valid and updated
        UpdateViewport(Core.GraphicsDevice.Viewport);

        _inputHandler.Update(gameTime);

        Vector2 heroCenter = _heroPosition + new Vector2(_hero.Width / 2f, _hero.Height / 2f);

        int marginSizeX = (int)(100 / _camera.Zoom);
        int marginSizeY = (int)(80 / _camera.Zoom);
        // Calculate margin scaled by zoom to world units
        Rectangle margin = new Rectangle(
            (int)(100 / _camera.Zoom),
            (int)(80 / _camera.Zoom),
            (int)(_viewport.Width / _camera.Zoom) - (int)(200 / _camera.Zoom),
            (int)(_viewport.Height / _camera.Zoom) - (int)(160 / _camera.Zoom)
        );

        //_camera.FollowWithMargin(slimeCenter, margin, 0.1f);
        _camera.FollowWithMargin(heroCenter, margin, 0.1f);
        _camera.ClampToMap(_map.Width, _map.Height, _map.TileWidth);

        // Direct follow without margin or clamp
        _camera.Update(gameTime);

        UpdateViewport(Core.GraphicsDevice.Viewport);

        UpdateHeroAnimation();

        Circle slimeBounds = new Circle(
            (int)(_heroPosition.X + (_hero.Width * 0.5f)),
            (int)(_heroPosition.Y + (_hero.Height * 0.5f)),
            (int)(_hero.Width * 0.5f)
        );

        // Use distance based checks to determine if the slime is within the
        // bounds of the game screen, and if it is outside that screen edge,
        // move it back inside.
        if (slimeBounds.Left < _roomBounds.Left)
        {
            _heroPosition.X = _roomBounds.Left;
        }
        else if (slimeBounds.Right > _roomBounds.Right)
        {
            _heroPosition.X = _roomBounds.Right - _hero.Width;
        }

        if (slimeBounds.Top < _roomBounds.Top)
        {
            _heroPosition.Y = _roomBounds.Top;
        }
        else if (slimeBounds.Bottom > _roomBounds.Bottom)
        {
            _heroPosition.Y = _roomBounds.Bottom - _hero.Height;
        }
        _heroPosition.X = MathHelper.Clamp(_heroPosition.X, _roomBounds.Left, _roomBounds.Right - _hero.Width);
        _heroPosition.Y = MathHelper.Clamp(_heroPosition.Y, _roomBounds.Top, _roomBounds.Bottom - _hero.Height);
    }

    public void UpdateViewport(Viewport viewport)
    {
        _viewport = viewport;
    }

    public override void Draw(GameTime gameTime)
    {
        // Clear the back buffer.
        Core.GraphicsDevice.Clear(Color.CornflowerBlue);



        // Begin the sprite batch to prepare for rendering.
        //Moves with camera

        Matrix transform = _camera.GetViewMatrix();
        Core.SpriteBatch.Begin(transformMatrix: transform, samplerState: SamplerState.PointClamp);

        _mapRenderer.Draw(transform);

        // Draw the slime sprite.
        _hero.Draw(Core.SpriteBatch, _heroPosition);

        _npcManager.Draw(Core.SpriteBatch);

        Core.SpriteBatch.End();
        //End moves with camera

        // Draw the bat sprite.
        //_bat.Draw(Core.SpriteBatch, _batPosition);

        //Begin draw without camera
        Core.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);

        // Draw the score.
        Core.SpriteBatch.DrawString(
            _font,              // spriteFont
            $"Score: {_score}", // text
            new Vector2(_viewport.X + 20, _viewport.Y + 20), // position
            Color.White,        // color
            0.0f,               // rotation
            _scoreTextOrigin,   // origin
            1.0f,               // scale
            SpriteEffects.None, // effects
            0.0f                // layerDepth
        );

        DrawDebugInfo(Core.SpriteBatch, gameTime);

        // Always end the sprite batch when finished.
        Core.SpriteBatch.End();

        // Draw the Gum UI
        GumService.Default.Draw();
    }

    private void PauseGame()
    {
        // Make the pause panel UI element visible.
        _pausePanel.IsVisible = true;

        // Set the resume button to have focus
        _resumeButton.IsFocused = true;
    }

    private void CreatePausePanel()
    {
        _pausePanel = new Panel();
        _pausePanel.Anchor(Anchor.Center);
        _pausePanel.Visual.WidthUnits = DimensionUnitType.Absolute;
        _pausePanel.Visual.HeightUnits = DimensionUnitType.Absolute;
        _pausePanel.Visual.Height = 70;
        _pausePanel.Visual.Width = 264;
        _pausePanel.IsVisible = false;
        _pausePanel.AddToRoot();

        TextureRegion backgroundRegion = _atlas.GetRegion("panel-background");

        NineSliceRuntime background = new NineSliceRuntime();
        background.Dock(Dock.Fill);
        background.Texture = backgroundRegion.Texture;
        background.TextureAddress = TextureAddress.Custom;
        background.TextureHeight = backgroundRegion.Height;
        background.TextureLeft = backgroundRegion.SourceRectangle.Left;
        background.TextureTop = backgroundRegion.SourceRectangle.Top;
        background.TextureWidth = backgroundRegion.Width;
        _pausePanel.AddChild(background);

        var textInstance = new TextRuntime();
        textInstance.Text = "PAUSED";
        textInstance.CustomFontFile = @"fonts/04b_30.fnt";
        textInstance.UseCustomFont = true;
        textInstance.FontScale = 0.5f;
        textInstance.X = 10f;
        textInstance.Y = 10f;
        _pausePanel.AddChild(textInstance);

        _resumeButton = new AnimatedButton(_atlas);
        _resumeButton.Text = "RESUME";
        _resumeButton.Anchor(Anchor.BottomLeft);
        _resumeButton.Visual.X = 9f;
        _resumeButton.Visual.Y = -9f;
        _resumeButton.Visual.Width = 80;
        _resumeButton.Click += HandleResumeButtonClicked;
        _pausePanel.AddChild(_resumeButton);

        AnimatedButton quitButton = new AnimatedButton(_atlas);
        quitButton.Text = "QUIT";
        quitButton.Anchor(Anchor.BottomRight);
        quitButton.Visual.X = -9f;
        quitButton.Visual.Y = -9f;
        quitButton.Width = 80;
        quitButton.Click += HandleQuitButtonClicked;

        _pausePanel.AddChild(quitButton);
    }

    private void HandleResumeButtonClicked(object sender, EventArgs e)
    {
        // A UI interaction occurred, play the sound effect
        Core.Audio.PlaySoundEffect(_uiSoundEffect);

        // Make the pause panel invisible to resume the game.
        _pausePanel.IsVisible = false;
    }

    private void HandleQuitButtonClicked(object sender, EventArgs e)
    {
        // A UI interaction occurred, play the sound effect
        Core.Audio.PlaySoundEffect(_uiSoundEffect);

        // Go back to the title scene.
        Core.ChangeScene(new TitleScene());
    }

    private void InitializeUI()
    {
        GumService.Default.Root.Children.Clear();

        CreatePausePanel();
    }

    private void UpdateHeroAnimation()
    {
        KeyboardInfo keyboard = Core.Input.Keyboard;
        bool moving = keyboard.IsKeyDown(Keys.A) || keyboard.IsKeyDown(Keys.D) ||
                      keyboard.IsKeyDown(Keys.Left) || keyboard.IsKeyDown(Keys.Right);

        // Determine facing direction
        if (keyboard.IsKeyDown(Keys.A) || keyboard.IsKeyDown(Keys.Left))
            _facingRight = false;
        else if (keyboard.IsKeyDown(Keys.D) || keyboard.IsKeyDown(Keys.Right))
            _facingRight = true;

        // Set animation state
        string desiredAnimation = moving ? "Mage_Walk" : "Mage_Idle";
        if (_currentAnimationName != desiredAnimation)
        {
            _hero.Animation = _heroAtlas.GetAnimation(desiredAnimation);
            _currentAnimationName = desiredAnimation;
        }

        // Flip sprite based on direction
        _hero.Effects = _facingRight ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
    }

    private void MoveHero(Vector2 movement)
    {
        _heroPosition += movement;
    }

    private void FindPlayerSpawnPoint()
    {
        // Find the object layer named "Entities"
        var entityLayer = _map.ObjectLayers.FirstOrDefault(layer => layer.Name == "Entities");

        if (entityLayer == null)
        {
            throw new Exception("Entities layer not found in the map.");
        }

        // Find the first object with Class == "Player"
        var playerObject = entityLayer.Objects.FirstOrDefault(obj => obj.Type == "Player");

        if (playerObject == null)
        {
            throw new Exception("Player spawn point not found in Entities layer.");
        }

        // Set the player start position from the object position
        _playerStart = new Vector2(playerObject.Position.X, playerObject.Position.Y);
    }

    private void DrawDebugInfo(SpriteBatch spriteBatch, GameTime gameTime)
    {
        Vector2 position = new Vector2(20, 40);

        float fps = 1f / (float)gameTime.ElapsedGameTime.TotalSeconds;

        string[] debugLines = new string[]
        {
        $"Player Position: {_heroPosition}",
        $"Camera Position: {_camera.Position}",
        $"Viewport Position: {_viewport.X} ,{_viewport.Y}",
        $"FPS: {fps:0.0}"
        };

        foreach (string line in debugLines)
        {
            spriteBatch.DrawString(_debugFont, line, position, Color.Yellow);
            position.Y += _debugFont.LineSpacing;
        }
    }

}
