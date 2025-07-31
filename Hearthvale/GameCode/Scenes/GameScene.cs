using Hearthvale.GameCode.Entities;
using Hearthvale.GameCode.Entities.Players;
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
using SharpDX.Direct2D1;

namespace Hearthvale.Scenes;

public class GameScene : Scene
{
    private Vector2 _playerStart;

    private const float dialogDistance = 40f;

    private bool _isPlayerAttacking;
    public bool IsPlayerAttacking => _isPlayerAttacking;

    private const float MOVEMENT_SPEED = 1.0f; // Increased for better feel
    private SoundEffect _playerAttackSoundEffect; // Add this field

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
    private TextureAtlas _weaponAtlas;
    private Viewport _viewport;
    private Camera2D _camera;
    private InputHandler _inputHandler;

    // handlers
    private NpcManager _npcManager;
    private GameUIManager _uiManager;
    private Player _player;
    // private GameInputHandler _gameInputHandler; // REMOVED
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
        _weaponAtlas = TextureAtlas.FromFile(Core.Content, "images/weapon-atlas.xml");

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
                var npcWeapon = new Weapon("Dagger", baseDamage: 2, _weaponAtlas);
                npcWeapon.Scale = 0.4f;
                npc.EquipWeapon(npcWeapon);
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
        _combatEffectsManager = new CombatEffectsManager(_camera);
        _player = new Player(_heroAtlas, _playerStart, _combatEffectsManager, MOVEMENT_SPEED);
        var sword = new Weapon("Dagger", baseDamage: 5, _weaponAtlas);
        sword.Scale = 0.5f;
        sword.ManualOffset = new Vector2(0, 0); // Move down and to the right
        _player.EquipWeapon(sword);
        _dialogManager = new DialogManager(_uiManager, _player, _npcManager, dialogDistance);
        _playerAttackSoundEffect = Content.Load<SoundEffect>("audio/player_attack"); // Load attack sound
        _combatManager = new CombatManager(
            _npcManager,
            _player,
            _scoreManager,
            Core.SpriteBatch,
            _combatEffectsManager,
            _bounceSoundEffect,
            _collectSoundEffect,
            _playerAttackSoundEffect,
            worldBounds: _mapManager.RoomBounds
        );

        _inputHandler = new InputHandler(
            _camera,
            () => _uiManager.PauseGame(),
            () => _npcManager.SpawnNPC("DefaultNPCType", _player.Position),
            () => Core.ChangeScene(new TitleScene()),
            () => _combatManager.HandlePlayerProjectileAttack(), // Projectile attack
            () => _combatManager.HandlePlayerMeleeAttack()      // Melee attack
        );
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

        _inputHandler.Update(gameTime);

        _player.Update(gameTime, Keyboard.GetState(), _npcManager.Npcs);
        _npcManager.Update(gameTime, _player);

        _combatManager.Update(gameTime);

        _combatEffectsManager.Update(gameTime);

        _dialogManager.Update();

        _mapManager.Update(gameTime);
        UpdateViewport(Core.GraphicsDevice.Viewport);
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
        // End current batch to start a new one with camera transform
        Core.SpriteBatch.End();
        Matrix transform = _camera.GetViewMatrix();
        Core.SpriteBatch.Begin(transformMatrix: transform, samplerState: SamplerState.PointClamp);

        _mapManager.Draw(transform);
        // Determine if sword should be behind player
        bool isMovingUp = _player.LastMovementDirection.Y < 0;
        bool drawWeaponBehind = isMovingUp && !_player.IsAttacking; // Draw behind when moving up, unless attacking

        if (drawWeaponBehind)
        {
            _player.EquippedWeapon?.Draw(Core.SpriteBatch, _player.Position);
            _player.Sprite.Draw(Core.SpriteBatch, _player.Position);
        }
        else
        {
            _player.Sprite.Draw(Core.SpriteBatch, _player.Position);
            _player.EquippedWeapon?.Draw(Core.SpriteBatch, _player.Position);
        }
        _npcManager.Draw(Core.SpriteBatch, _uiManager);
        _combatManager.DrawProjectiles(Core.SpriteBatch);
        _uiManager.DrawCollisionBoxes(Core.SpriteBatch, _player, _npcManager.Npcs);
        _combatEffectsManager.Draw(Core.SpriteBatch, _font);

        // End camera batch and restart for UI
        Core.SpriteBatch.End();
        Core.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);

        _uiManager.DrawPlayerHealthBar(Core.SpriteBatch, _player, new Vector2(20, 20), new Vector2(100, 12));
        _scoreManager.Draw(Core.SpriteBatch);
        _uiManager.DrawDebugInfo(Core.SpriteBatch, gameTime, _player.Position, _camera.Position, _viewport);

        GumService.Default.Draw();
    }
}