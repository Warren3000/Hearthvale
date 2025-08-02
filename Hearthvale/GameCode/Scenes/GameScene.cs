using Hearthvale.GameCode.Data;
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
using System.Collections.Generic;

namespace Hearthvale.Scenes
{
    public class GameScene : Scene
    {
        private Vector2 _playerStart;

        private const float dialogDistance = 40f;
        private const float MOVEMENT_SPEED = 2.0f;

        private SoundEffect _playerAttackSoundEffect;
        private SoundEffect _bounceSoundEffect;
        private SoundEffect _collectSoundEffect;
        private SoundEffect _uiSoundEffect;

        private SpriteFont _font;
        private SpriteFont _debugFont;

        private TextureAtlas _atlas;
        private TextureAtlas _heroAtlas;
        private TextureAtlas _weaponAtlas;
        private TextureAtlas _arrowAtlas;

        private Viewport _viewport;
        private Camera2D _camera;
        private InputHandler _inputHandler;

        // Managers
        private NpcManager _npcManager;
        private GameUIManager _uiManager;
        private ScoreManager _scoreManager;
        private MapManager _mapManager;
        private CombatEffectsManager _combatEffectsManager;
        private CombatManager _combatManager;
        private DialogManager _dialogManager;
        private CameraManager _cameraManager;
        private WeaponManager _weaponManager;

        private Player _player;
        private List<Weapon> _playerWeapons;
        private int _currentPlayerWeaponIndex = 0;

        public override void Initialize()
        {
            base.Initialize();
            Core.ExitOnEscape = false;
        }

        public override void LoadContent()
        {
            // Load all data from JSON files first
            DataManager.LoadContent();

            // Initialize camera and managers
            _camera = new Camera2D(Core.GraphicsDevice.Viewport) { Zoom = 3.0f };
            _cameraManager = new CameraManager(_camera);
            _weaponManager = new WeaponManager();

            // Load assets
            _atlas = TextureAtlas.FromFile(Core.Content, "images/atlas-definition.xml");
            _heroAtlas = TextureAtlas.FromFile(Core.Content, "images/npc-atlas.xml");
            _weaponAtlas = TextureAtlas.FromFile(Core.Content, "images/weapon-atlas.xml");
            _arrowAtlas = TextureAtlas.FromFile(Core.Content, "images/arrow-atlas.xml");
            _bounceSoundEffect = Content.Load<SoundEffect>("audio/bounce");
            _collectSoundEffect = Content.Load<SoundEffect>("audio/collect");
            _uiSoundEffect = Core.Content.Load<SoundEffect>("audio/ui");
            _playerAttackSoundEffect = Content.Load<SoundEffect>("audio/player_attack");
            _font = Core.Content.Load<SpriteFont>("fonts/04B_30");
            _debugFont = Content.Load<SpriteFont>("fonts/DebugFont");

            // Initialize map and NPC manager
            _mapManager = new MapManager(Core.GraphicsDevice, Core.Content, "Tilesets/DampDungeons/Tiles/DungeonMap");
            _npcManager = new NpcManager(_heroAtlas, _mapManager.RoomBounds);
            _npcManager.SpawnAllNpcTypesTest();

            // Load NPCs from the map and configure them using data-driven stats
            var entityLayer = _mapManager.GetObjectLayer("Entities");
            if (entityLayer != null)
            {
                _npcManager.LoadNPCs(entityLayer.Objects);
                foreach (var npc in _npcManager.Npcs)
                {
                    npc.DialogText = "I am a friendly NPC!";
                    var npcWeaponStats = DataManager.GetWeaponStats("NpcDagger");
                    var npcWeapon = new Weapon("Dagger", npcWeaponStats, _weaponAtlas, _arrowAtlas);
                    _weaponManager.EquipWeapon(npc, npcWeapon);
                }
            }

            // Initialize score and UI managers
            var scoreTextPosition = new Vector2(_mapManager.RoomBounds.Left, _mapManager.TileHeight * 1f);
            var scoreTextOrigin = new Vector2(0, _font.MeasureString("Score").Y * 0.5f);
            _scoreManager = new ScoreManager(_font, scoreTextPosition, scoreTextOrigin);
            _uiManager = new GameUIManager(
                _atlas, _font, _debugFont,
                () => _uiManager.ResumeGame(_uiSoundEffect),
                () => _uiManager.QuitGame(_uiSoundEffect, () => Core.ChangeScene(new TitleScene()))
            );

            // Initialize combat systems
            _combatEffectsManager = new CombatEffectsManager(_camera);
            _combatManager = new CombatManager(
                _npcManager, null, _scoreManager, Core.SpriteBatch, _combatEffectsManager,
                _bounceSoundEffect, _collectSoundEffect, _mapManager.RoomBounds
            );

            // Initialize Player
            _playerStart = _mapManager.GetPlayerSpawnPoint();
            _player = new Player(
                _heroAtlas, _playerStart, _combatManager, _combatEffectsManager, _scoreManager,
                _bounceSoundEffect, _collectSoundEffect, _playerAttackSoundEffect, MOVEMENT_SPEED
            );
            _combatManager.SetPlayer(_player);

            // Create and equip player weapons from data
            _playerWeapons = new List<Weapon>
            {
                new Weapon("Dagger", DataManager.GetWeaponStats("Dagger"), _weaponAtlas, _arrowAtlas),
                new Weapon("Dagger-Copper", DataManager.GetWeaponStats("Dagger-Copper"), _weaponAtlas, _arrowAtlas),
                new Weapon("Dagger-Cold", DataManager.GetWeaponStats("Dagger-Cold"), _weaponAtlas, _arrowAtlas)
            };
            _weaponManager.EquipWeapon(_player, _playerWeapons[_currentPlayerWeaponIndex]);

            // Initialize remaining managers
            _dialogManager = new DialogManager(_uiManager, _player, _npcManager.Characters, dialogDistance);
            _inputHandler = new InputHandler(
                _camera, MOVEMENT_SPEED,
                () => _uiManager.PauseGame(),
                (movement) => _player.Move(movement, _mapManager.RoomBounds, _player.Sprite.Width, _player.Sprite.Height, _npcManager.Npcs),
                () => _npcManager.SpawnNPC("DefaultNPCType", _player.Position),
                () => Core.ChangeScene(new TitleScene()),
                () => _player.CombatController.StartProjectileAttack(),
                () => _player.CombatController.StartMeleeAttack(),
                RotatePlayerWeaponLeft,
                RotatePlayerWeaponRight
            );
        }

        private void RotatePlayerWeaponLeft()
        {
            if (_playerWeapons.Count == 0) return;
            _currentPlayerWeaponIndex = (_currentPlayerWeaponIndex - 1 + _playerWeapons.Count) % _playerWeapons.Count;
            _weaponManager.EquipWeapon(_player, _playerWeapons[_currentPlayerWeaponIndex]);
        }

        private void RotatePlayerWeaponRight()
        {
            if (_playerWeapons.Count == 0) return;
            _currentPlayerWeaponIndex = (_currentPlayerWeaponIndex + 1) % _playerWeapons.Count;
            _weaponManager.EquipWeapon(_player, _playerWeapons[_currentPlayerWeaponIndex]);
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
            _player.Update(gameTime, _npcManager.Npcs);
            _npcManager.Update(gameTime, _player);
            _combatManager.Update(gameTime);
            _combatEffectsManager.Update(gameTime);
            _dialogManager.Update();
            _mapManager.Update(gameTime);
            _player.ClampToBounds(_mapManager.RoomBounds);
            _uiManager.UpdateWeaponUI(_player.EquippedWeapon);

            UpdateViewport(Core.GraphicsDevice.Viewport);
            Rectangle margin = new Rectangle(
                (int)(100 / _cameraManager.Zoom), (int)(80 / _cameraManager.Zoom),
                (int)(_viewport.Width / _cameraManager.Zoom) - (int)(200 / _cameraManager.Zoom),
                (int)(_viewport.Height / _cameraManager.Zoom) - (int)(160 / _cameraManager.Zoom)
            );

            _cameraManager.UpdateCamera(
               _player.Position, new Point((int)_player.Sprite.Width, (int)_player.Sprite.Height),
               margin, _mapManager, _combatEffectsManager, gameTime
           );
        }

        public void UpdateViewport(Viewport viewport)
        {
            _viewport = viewport;
        }

        public override void Draw(GameTime gameTime)
        {
            Core.SpriteBatch.End();
            Matrix transform = _camera.GetViewMatrix();
            Core.SpriteBatch.Begin(transformMatrix: transform, samplerState: SamplerState.PointClamp);

            _mapManager.Draw(transform);
            _player.Draw(Core.SpriteBatch);
            _npcManager.Draw(Core.SpriteBatch, _uiManager);
            _combatManager.DrawProjectiles(Core.SpriteBatch);
            _uiManager.DrawCollisionBoxes(Core.SpriteBatch, _player, _npcManager.Npcs);
            _combatEffectsManager.Draw(Core.SpriteBatch, _font);

            Core.SpriteBatch.End();
            Core.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);

            _uiManager.DrawPlayerHealthBar(Core.SpriteBatch, _player, new Vector2(20, 20), new Vector2(100, 12));
            _scoreManager.Draw(Core.SpriteBatch);
            _uiManager.DrawDebugInfo(Core.SpriteBatch, gameTime, _player.Position, _camera.Position, _viewport);

            GumService.Default.Draw();
        }
    }
}