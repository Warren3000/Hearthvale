using Hearthvale.GameCode.Entities;
using Hearthvale.GameCode.Input;
using Hearthvale.GameCode.Managers;
using Hearthvale.GameCode.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameGum;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;
using MonoGameLibrary.Scenes;

namespace Hearthvale.Scenes;

public class GameScene : Scene
{
    private Vector2 _playerStart;

    private const float dialogDistance = 40f;

    private bool _isPlayerAttacking;
    public bool IsPlayerAttacking => _isPlayerAttacking;

    private const float MOVEMENT_SPEED = 1.0f;
    private const int PlayerAttackPower = 1;

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


    // handlers
    private NpcManager _npcManager;
    private GameUIManager _uiManager;
    private Player _player;
    private GameInputHandler _gameInputHandler;
    private ScoreManager _scoreManager;
    private MapManager _mapManager;
    private CombatEffectsManager _combatEffectsManager;
    private CombatManager _combatManager;
    private DialogManager _dialogManager;
    private CameraManager _cameraManager;

    public override void Initialize()
    {
        base.Initialize();
        Core.ExitOnEscape = false;

    }

    public override void LoadContent()
    {
        _camera = new Camera2D(Core.GraphicsDevice.Viewport);
        _camera.Zoom = 3.0f;
        _cameraManager = new CameraManager(_camera);

        _atlas = TextureAtlas.FromFile(Core.Content, "images/atlas-definition.xml");
        _heroAtlas = TextureAtlas.FromFile(Core.Content, "images/npc-atlas.xml");

        // Initialize MapManager
        _mapManager = new MapManager(Game1.GraphicsDevice, Game1.Content, "Tilesets/DampDungeons/Tiles/DungeonMap");

        // Use MapManager for bounds and NPCs
        _npcManager = new NpcManager(_heroAtlas, _mapManager.RoomBounds);
        _npcManager.SpawnAllNpcTypesTest();

        var entityLayer = _mapManager.GetObjectLayer("Entities");
        if (entityLayer != null)
        {
            _npcManager.LoadNPCs(entityLayer.Objects);

            foreach (var npc in _npcManager.Npcs)
            {
                npc.DialogText = "I am a friendly NPC!";
                // You can customize per NPC type or from map data
            }
        }
        _bounceSoundEffect = Content.Load<SoundEffect>("audio/bounce");
        _collectSoundEffect = Content.Load<SoundEffect>("audio/collect");
        _uiSoundEffect = Core.Content.Load<SoundEffect>("audio/ui");
        _font = Core.Content.Load<SpriteFont>("fonts/04B_30");
        _debugFont = Content.Load<SpriteFont>("fonts/DebugFont");

        // Score display
        _scoreTextPosition = new Vector2(_mapManager.RoomBounds.Left, _mapManager.TileHeight * 1f);
        float scoreTextYOrigin = _font.MeasureString("Score").Y * 0.5f;
        _scoreTextOrigin = new Vector2(0, scoreTextYOrigin);
        _scoreManager = new ScoreManager(_font, _scoreTextPosition, _scoreTextOrigin);



        _playerStart = _mapManager.GetPlayerSpawnPoint();

        // Initialize UI manager
        _uiManager = new GameUIManager(
            _atlas,
            _font,
            _debugFont,
            () => _uiManager.ResumeGame(_uiSoundEffect),
            () => _uiManager.QuitGame(_uiSoundEffect, () => Core.ChangeScene(new TitleScene()))
        );
        _player = new Player(_heroAtlas, _playerStart, MOVEMENT_SPEED);
        _combatEffectsManager = new CombatEffectsManager(_camera);
        _dialogManager = new DialogManager(_uiManager, _player, _npcManager, dialogDistance);
        _combatManager = new CombatManager(
            _npcManager,
            _player,
            _scoreManager,
            _combatEffectsManager,
            _bounceSoundEffect,
            _collectSoundEffect
        );

        _inputHandler = new InputHandler(
            _camera,
            MOVEMENT_SPEED,
            () => _uiManager.PauseGame(),
            movement => _player.Move(movement, _mapManager.RoomBounds, _player.Sprite.Width, _player.Sprite.Height, _npcManager.Npcs),
            () => _npcManager.SpawnNPC("DefaultNPCType", _player.Position),
            () => Core.ChangeScene(new TitleScene())
        );
        _gameInputHandler = new GameInputHandler(
            _player,
            _npcManager,
            _uiManager,
            () => _uiManager.PauseGame(),
            () => Core.ChangeScene(new TitleScene()));
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
        _npcManager.Update(gameTime, _player);

        KeyboardState keyboardState = Keyboard.GetState();
        _isPlayerAttacking = keyboardState.IsKeyDown(Keys.Space);

        _combatManager.Update(gameTime);

        if (IsPlayerAttacking && _combatManager.CanAttack)
        {
            _combatManager.HandlePlayerAttack(PlayerAttackPower);
        }
        _combatEffectsManager.Update(gameTime);

        _dialogManager.Update();

        _mapManager.Update(gameTime);
        UpdateViewport(Core.GraphicsDevice.Viewport);
        _inputHandler.Update(gameTime);

        _player.ClampToBounds(_mapManager.RoomBounds);

        // Calculate the margin rectangle as before
        Rectangle margin = new Rectangle(
            (int)(100 / _cameraManager.Zoom),
            (int)(80 / _cameraManager.Zoom),
            (int)(_viewport.Width / _cameraManager.Zoom) - (int)(200 / _cameraManager.Zoom),
            (int)(_viewport.Height / _cameraManager.Zoom) - (int)(160 / _cameraManager.Zoom)
        );

        // Call CameraManager to update the camera
        _cameraManager.UpdateCamera(
           _player.Position,
           new Point((int)_player.Sprite.Width, (int)_player.Sprite.Height),
           margin,
           _mapManager,
           gameTime
       );

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
        _uiManager.DrawCollisionBoxes(Core.SpriteBatch, _player, _npcManager.Npcs);
        _combatEffectsManager.Draw(Core.SpriteBatch, _font);
        Core.SpriteBatch.End();

        Core.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);

        _uiManager.DrawPlayerHealthBar(Core.SpriteBatch, _player, new Vector2(20, 20), new Vector2(100, 12));
        _scoreManager.Draw(Core.SpriteBatch);
        _uiManager.DrawDebugInfo(Core.SpriteBatch, gameTime, _player.Position, _camera.Position, _viewport);

        Core.SpriteBatch.End();

        GumService.Default.Draw();
    }
}
