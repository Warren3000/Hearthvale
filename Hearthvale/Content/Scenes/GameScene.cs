using Hearthvale.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameGum;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;
using MonoGameLibrary.Scenes;
using System;
using System.Diagnostics;
using System.Linq;

namespace Hearthvale.Scenes;

public class GameScene : Scene
{
    private Vector2 _playerStart;

    private const float dialogDistance = 40f;
    
    private bool _isPlayerAttacking;
    public bool IsPlayerAttacking => _isPlayerAttacking;

    private const float MOVEMENT_SPEED = 1.0f;
    public float MovementSpeed => MOVEMENT_SPEED;

    private SoundEffect _bounceSoundEffect;
    private SoundEffect _collectSoundEffect;

    private SpriteFont _font;
    private SpriteFont _debugFont;
    private Vector2 _scoreTextPosition;
    private Vector2 _scoreTextOrigin;

    private SoundEffect _uiSoundEffect;
    private TextureAtlas _atlas;
    private TextureAtlas _heroAtlas;
    private Viewport _viewport;
    private Camera2D _camera;
    private InputHandler _inputHandler;
    private Texture2D _whitePixel;

    // handlers
    private NpcManager _npcManager;
    private GameUIManager _uiManager;
    private Player _player;
    private GameInputHandler _gameInputHandler;
    private ScoreManager _scoreManager;
    private MapManager _mapManager;

    public override void Initialize()
    {
        base.Initialize();
        Core.ExitOnEscape = false;

        _camera = new Camera2D(Core.GraphicsDevice.Viewport);
        _camera.Zoom = 3.0f;
        _viewport = Core.GraphicsDevice.Viewport;

        Debug.WriteLine($"Viewport size: {_viewport.Width} x {_viewport.Height}");
        Debug.WriteLine($"Camera position: {_camera.Position}");
        Debug.WriteLine($"Zoom: {_camera.Zoom}");

        // MapManager must be initialized in LoadContent after content is loaded
    }

    public override void LoadContent()
    {
        _atlas = TextureAtlas.FromFile(Core.Content, "images/atlas-definition.xml");
        _heroAtlas = TextureAtlas.FromFile(Core.Content, "images/npc-atlas.xml");

        // Initialize MapManager
        _mapManager = new MapManager(Game1.GraphicsDevice, Game1.Content, "Tilesets/DampDungeons/Tiles/DungeonMap");

        // Use MapManager for bounds and NPCs
        _npcManager = new NpcManager(_heroAtlas, _mapManager.RoomBounds);

        var entityLayer = _mapManager.GetObjectLayer("Entities");
        if (entityLayer != null)
        {
            _npcManager.LoadNPCs(entityLayer.Objects);
        }

        _bounceSoundEffect = Content.Load<SoundEffect>("audio/bounce");
        _collectSoundEffect = Content.Load<SoundEffect>("audio/collect");
        _uiSoundEffect = Core.Content.Load<SoundEffect>("audio/ui");
        _font = Core.Content.Load<SpriteFont>("fonts/04B_30");
        _debugFont = Content.Load<SpriteFont>("fonts/DebugFont");

        // Score display
        _scoreTextPosition = new Vector2(_mapManager.RoomBounds.Left, _mapManager.TileHeight * 0.5f);
        float scoreTextYOrigin = _font.MeasureString("Score").Y * 0.5f;
        _scoreTextOrigin = new Vector2(0, scoreTextYOrigin);
        _scoreManager = new ScoreManager(_font, _scoreTextPosition, _scoreTextOrigin);

        _whitePixel = new Texture2D(Core.GraphicsDevice, 1, 1);
        _whitePixel.SetData(new[] { Color.White });

        FindPlayerSpawnPoint();

        // Initialize UI manager
        _uiManager = new GameUIManager(_atlas, _font, _debugFont, HandleResumeButtonClicked, HandleQuitButtonClicked);
        _player = new Player(_heroAtlas, _playerStart, MOVEMENT_SPEED);

        _inputHandler = new InputHandler(
            _camera,
            MOVEMENT_SPEED,
            PauseGame,
            MoveHero,
            () => _npcManager.SpawnNPC("DefaultNPCType", _player.Position),
            () => Core.ChangeScene(new TitleScene())
        );

        _gameInputHandler = new GameInputHandler(_player, _npcManager, _uiManager, PauseGame, () => Core.ChangeScene(new TitleScene()));
    }

    public override void Update(GameTime gameTime)
    {
        GumService.Default.Update(gameTime);

        if (_uiManager.IsDialogOpen && InputHandler.IsKeyPressed(Keys.Enter))
        {
            _uiManager.HideDialog();
        }

        if (_uiManager.IsPausePanelVisible)
            return;

        _player.Update(gameTime, Keyboard.GetState());
        _npcManager.Update(gameTime);

        KeyboardState keyboardState = Keyboard.GetState();
        _isPlayerAttacking = keyboardState.IsKeyDown(Keys.Space);

        if (IsPlayerAttacking)
        {
            Rectangle attackArea = _player.GetAttackArea();
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
            if (Vector2.Distance(_player.Position, npc.Position) < dialogDistance)
            {
                if (InputHandler.IsKeyPressed(Keys.E))
                {
                    _uiManager.ShowDialog($"Hello, I am {npc.GetType().Name}!");
                }
            }
        }

        _mapManager.Update(gameTime);
        UpdateViewport(Core.GraphicsDevice.Viewport);
        _inputHandler.Update(gameTime);

        // Clamp player position to room bounds
        float clampedX = MathHelper.Clamp(
            _player.Position.X,
            _mapManager.RoomBounds.Left,
            _mapManager.RoomBounds.Right - _player.Sprite.Width
        );
        float clampedY = MathHelper.Clamp(
            _player.Position.Y,
            _mapManager.RoomBounds.Top,
            _mapManager.RoomBounds.Bottom - _player.Sprite.Height
        );
        _player.SetPosition(new Vector2(clampedX, clampedY));

        // Camera follow (use player center)
        Vector2 playerCenter = _player.Position + new Vector2(_player.Sprite.Width / 2f, _player.Sprite.Height / 2f);

        Rectangle margin = new Rectangle(
            (int)(100 / _camera.Zoom),
            (int)(80 / _camera.Zoom),
            (int)(_viewport.Width / _camera.Zoom) - (int)(200 / _camera.Zoom),
            (int)(_viewport.Height / _camera.Zoom) - (int)(160 / _camera.Zoom)
        );

        _camera.FollowWithMargin(playerCenter, margin, 0.1f);
        _camera.ClampToMap(_mapManager.Map.Width, _mapManager.Map.Height, _mapManager.TileWidth);
        _camera.Update(gameTime);

        UpdateViewport(Core.GraphicsDevice.Viewport);
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

        _mapManager.Draw(transform);
        _player.Sprite.Draw(Core.SpriteBatch, _player.Position);
        _npcManager.Draw(Core.SpriteBatch);

        Core.SpriteBatch.End();

        Core.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);

        _scoreManager.Draw(Core.SpriteBatch);

        _uiManager.DrawDebugInfo(Core.SpriteBatch, gameTime, _player.Position, _camera.Position, _viewport);

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

    private void MoveHero(Vector2 movement)
    {
        Vector2 newPosition = _player.Position + movement;

        // Clamp to room bounds
        float clampedX = MathHelper.Clamp(
            newPosition.X,
            _mapManager.RoomBounds.Left,
            _mapManager.RoomBounds.Right - _player.Sprite.Width
        );
        float clampedY = MathHelper.Clamp(
            newPosition.Y,
            _mapManager.RoomBounds.Top,
            _mapManager.RoomBounds.Bottom - _player.Sprite.Height
        );

        _player.SetPosition(new Vector2(clampedX, clampedY));
    }

    private void FindPlayerSpawnPoint()
    {
        var entityLayer = _mapManager.GetObjectLayer("Entities");
        if (entityLayer == null)
            throw new Exception("Entities layer not found in the map.");

        var playerObject = entityLayer.Objects.FirstOrDefault(obj => obj.Type == "Player");
        if (playerObject == null)
            throw new Exception("Player spawn point not found in Entities layer.");

        _playerStart = new Vector2(playerObject.Position.X, playerObject.Position.Y);
    }
}
