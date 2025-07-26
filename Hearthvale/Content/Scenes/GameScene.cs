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
using Hearthvale.Content.Sprites;

namespace Hearthvale.Scenes;

public class GameScene : Scene
{
    private AnimatedSprite _bat;
    private Vector2 _heroPosition;
    private Vector2 _playerStart;
    private AnimatedSprite _hero;

    private const float dialogDistance = 40f;
    private NpcManager _npcManager;

    private bool _isPlayerAttacking;
    public bool IsPlayerAttacking => _isPlayerAttacking;
    private bool _facingRight = true;
    private string _currentAnimationName = "Mage_Idle";

    private const float MOVEMENT_SPEED = 3.0f;
    public float MovementSpeed => MOVEMENT_SPEED;

    private Vector2 _batPosition;
    private Vector2 _batVelocity;
    private Tilemap _tilemap;
    private Rectangle _roomBounds;
    private SoundEffect _bounceSoundEffect;
    private SoundEffect _collectSoundEffect;
    private SpriteFont _font;
    private SpriteFont _debugFont;
    private int _score;
    private Vector2 _scoreTextPosition;
    private Vector2 _scoreTextOrigin;

    private SoundEffect _uiSoundEffect;
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

    // New UI manager
    private GameUIManager _uiManager;

    public override void Initialize()
    {
        base.Initialize();
        Core.ExitOnEscape = false;

        Rectangle screenBounds = Core.GraphicsDevice.PresentationParameters.Bounds;
        _roomBounds = new Rectangle(0, 0, _map.Width * 16, _map.Height * 16);

        int centerRow = _map.Height / 2;
        int centerColumn = _map.Width / 2;
        _heroPosition = new Vector2(centerColumn * _map.TileWidth, centerRow * _map.TileHeight);
        _batPosition = new Vector2(_roomBounds.Left, _roomBounds.Top);
        _scoreTextPosition = new Vector2(_roomBounds.Left, _map.TileHeight * 0.5f);
        float scoreTextYOrigin = _font.MeasureString("Score").Y * 0.5f;
        _scoreTextOrigin = new Vector2(0, scoreTextYOrigin);

        _camera = new Camera2D(Core.GraphicsDevice.Viewport);
        _camera.Zoom = 3.0f;
        _viewport = Core.GraphicsDevice.Viewport;

        Debug.WriteLine($"Viewport size: {_viewport.Width} x {_viewport.Height}");
        Debug.WriteLine($"Map size: {_map.Width}x{_map.Height} tiles");
        Debug.WriteLine($"Map pixel size: {_map.Width * _map.TileWidth}x{_map.Height * _map.TileHeight}");
        Debug.WriteLine($"Camera position: {_camera.Position}");
        Debug.WriteLine($"Zoom: {_camera.Zoom}");

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
        _atlas = TextureAtlas.FromFile(Core.Content, "images/atlas-definition.xml");
        _heroAtlas = TextureAtlas.FromFile(Core.Content, "images/npc-atlas.xml");
        _hero = _heroAtlas.CreateAnimatedSprite("Mage_Idle");
        _hero.Scale = new Vector2(1f, 1f);

        _map = Game1.Content.Load<TiledMap>("Tilesets/DampDungeons/Tiles/DungeonMap");
        _mapRenderer = new TiledMapRenderer(Game1.GraphicsDevice, _map);

        _roomBounds = new Rectangle(0, 0, _map.Width * 16, _map.Height * 16);
        _npcManager = new NpcManager(_heroAtlas, _roomBounds);

        var entityLayer = _map.ObjectLayers.FirstOrDefault(layer => layer.Name == "Entities");
        if (entityLayer != null)
        {
            _npcManager.LoadNPCs(entityLayer.Objects);
        }

        _mapWidthInPixels = _map.WidthInPixels;
        _mapHeightInPixels = _map.HeightInPixels;

        _bounceSoundEffect = Content.Load<SoundEffect>("audio/bounce");
        _collectSoundEffect = Content.Load<SoundEffect>("audio/collect");
        _uiSoundEffect = Core.Content.Load<SoundEffect>("audio/ui");
        _font = Core.Content.Load<SpriteFont>("fonts/04B_30");
        _debugFont = Content.Load<SpriteFont>("fonts/DebugFont");

        _whitePixel = new Texture2D(Core.GraphicsDevice, 1, 1);
        _whitePixel.SetData(new[] { Color.White });

        FindPlayerSpawnPoint();
        _heroPosition = _playerStart;
        _hero.Position = _heroPosition;

        // Initialize UI manager
        _uiManager = new GameUIManager(_atlas, _font, _debugFont, HandleResumeButtonClicked, HandleQuitButtonClicked);
    }

    public override void Update(GameTime gameTime)
    {
        GumService.Default.Update(gameTime);

        // Dialog close logic
        if (_uiManager.IsDialogOpen && InputHandler.IsKeyPressed(Keys.Enter))
        {
            _uiManager.HideDialog();
        }

        // Pause logic
        if (_uiManager.IsPausePanelVisible)
            return;

        _hero.Update(gameTime);
        _npcManager.Update(gameTime);

        KeyboardState keyboardState = Keyboard.GetState();
        _isPlayerAttacking = keyboardState.IsKeyDown(Keys.Space);

        if (IsPlayerAttacking)
        {
            Rectangle attackArea = GetAttackArea();
            foreach (var npc in _npcManager.Npcs)
            {
                if (npc.Bounds.Intersects(attackArea))
                {
                    //npc.TakeDamage(PlayerAttackPower);
                }
            }
        }

        foreach (var npc in _npcManager.Npcs)
        {
            if (Vector2.Distance(_hero.Position, npc.Position) < dialogDistance)
            {
                if (InputHandler.IsKeyPressed(Keys.E))
                {
                    _uiManager.ShowDialog($"Hello, I am {npc.GetType().Name}!"); // Or use npc dialog property
                }
            }
        }

        _mapRenderer.Update(gameTime);
        UpdateViewport(Core.GraphicsDevice.Viewport);
        _inputHandler.Update(gameTime);

        Vector2 heroCenter = _heroPosition + new Vector2(_hero.Width / 2f, _hero.Height / 2f);

        Rectangle margin = new Rectangle(
            (int)(100 / _camera.Zoom),
            (int)(80 / _camera.Zoom),
            (int)(_viewport.Width / _camera.Zoom) - (int)(200 / _camera.Zoom),
            (int)(_viewport.Height / _camera.Zoom) - (int)(160 / _camera.Zoom)
        );

        _camera.FollowWithMargin(heroCenter, margin, 0.1f);
        _camera.ClampToMap(_map.Width, _map.Height, _map.TileWidth);
        _camera.Update(gameTime);

        UpdateViewport(Core.GraphicsDevice.Viewport);
        UpdateHeroAnimation();

        Circle slimeBounds = new Circle(
            (int)(_heroPosition.X + (_hero.Width * 0.5f)),
            (int)(_heroPosition.Y + (_hero.Height * 0.5f)),
            (int)(_hero.Width * 0.5f)
        );

        if (slimeBounds.Left < _roomBounds.Left)
            _heroPosition.X = _roomBounds.Left;
        else if (slimeBounds.Right > _roomBounds.Right)
            _heroPosition.X = _roomBounds.Right - _hero.Width;

        if (slimeBounds.Top < _roomBounds.Top)
            _heroPosition.Y = _roomBounds.Top;
        else if (slimeBounds.Bottom > _roomBounds.Bottom)
            _heroPosition.Y = _roomBounds.Bottom - _hero.Height;

        _heroPosition.X = MathHelper.Clamp(_heroPosition.X, _roomBounds.Left, _roomBounds.Right - _hero.Width);
        _heroPosition.Y = MathHelper.Clamp(_heroPosition.Y, _roomBounds.Top, _roomBounds.Bottom - _hero.Height);
    }

    public void UpdateViewport(Viewport viewport)
    {
        _viewport = viewport;
    }

    public override void Draw(GameTime gameTime)
    {
        Core.GraphicsDevice.Clear(Color.CornflowerBlue);

        Matrix transform = _camera.GetViewMatrix();
        Core.SpriteBatch.Begin(transformMatrix: transform, samplerState: SamplerState.PointClamp);

        _mapRenderer.Draw(transform);
        _hero.Draw(Core.SpriteBatch, _heroPosition);
        _npcManager.Draw(Core.SpriteBatch);

        Core.SpriteBatch.End();

        Core.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);

        Core.SpriteBatch.DrawString(
            _font,
            $"Score: {_score}",
            new Vector2(_viewport.X + 20, _viewport.Y + 20),
            Color.White,
            0.0f,
            _scoreTextOrigin,
            1.0f,
            SpriteEffects.None,
            0.0f
        );

        _uiManager.DrawDebugInfo(Core.SpriteBatch, gameTime, _heroPosition, _camera.Position, _viewport);

        Core.SpriteBatch.End();

        GumService.Default.Draw();
    }

    private void PauseGame()
    {
        _uiManager.ShowPausePanel();
    }

    private void HandleResumeButtonClicked()
    {
        Core.Audio.PlaySoundEffect(_uiSoundEffect);
        _uiManager.HidePausePanel();
    }

    private void HandleQuitButtonClicked()
    {
        Core.Audio.PlaySoundEffect(_uiSoundEffect);
        Core.ChangeScene(new TitleScene());
    }

    private void UpdateHeroAnimation()
    {
        KeyboardInfo keyboard = Core.Input.Keyboard;
        bool moving = keyboard.IsKeyDown(Keys.A) || keyboard.IsKeyDown(Keys.D) ||
                      keyboard.IsKeyDown(Keys.Left) || keyboard.IsKeyDown(Keys.Right);

        if (keyboard.IsKeyDown(Keys.A) || keyboard.IsKeyDown(Keys.Left))
            _facingRight = false;
        else if (keyboard.IsKeyDown(Keys.D) || keyboard.IsKeyDown(Keys.Right))
            _facingRight = true;

        string desiredAnimation = moving ? "Mage_Walk" : "Mage_Idle";
        if (_currentAnimationName != desiredAnimation)
        {
            _hero.Animation = _heroAtlas.GetAnimation(desiredAnimation);
            _currentAnimationName = desiredAnimation;
        }

        _hero.Effects = _facingRight ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
    }

    private void MoveHero(Vector2 movement)
    {
        _heroPosition += movement;
        _hero.Position = _heroPosition;
    }

    private Rectangle GetAttackArea()
    {
        int width = 32;
        int height = (int)_hero.Height;
        int offsetX = _facingRight ? (int)_hero.Width : -width;
        int x = (int)_heroPosition.X + offsetX;
        int y = (int)_heroPosition.Y;
        return new Rectangle(x, y, width, height);
    }

    private void FindPlayerSpawnPoint()
    {
        var entityLayer = _map.ObjectLayers.FirstOrDefault(layer => layer.Name == "Entities");
        if (entityLayer == null)
            throw new Exception("Entities layer not found in the map.");

        var playerObject = entityLayer.Objects.FirstOrDefault(obj => obj.Type == "Player");
        if (playerObject == null)
            throw new Exception("Player spawn point not found in Entities layer.");

        _playerStart = new Vector2(playerObject.Position.X, playerObject.Position.Y);
    }
}
