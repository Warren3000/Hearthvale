using Hearthvale.GameCode.Data;
using Hearthvale.GameCode.Entities;
using Hearthvale.GameCode.Entities.Interfaces;
using Hearthvale.GameCode.Entities.NPCs;
using Hearthvale.GameCode.Entities.Players;
using Hearthvale.GameCode.Input;
using Hearthvale.GameCode.Managers;
using Hearthvale.GameCode.UI;
using Hearthvale.GameCode.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameGum;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;
using MonoGameLibrary.Scenes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Hearthvale.Scenes
{
    public class GameScene : Scene, ICameraProvider
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

        private DungeonManager _dungeonManager;
        private MonoGameLibrary.Graphics.Tilemap _tilemap; // <-- Change type here

        // Managers
        private NpcManager _npcManager;
        private DialogManager _dialogManager;
        private WeaponManager _weaponManager;

        private Player _player;
        private List<Weapon> _playerWeapons;
        private int _currentPlayerWeaponIndex = 0;
        public Player Player => _player;

        private List<Rectangle> allObstacles = new List<Rectangle>();

        private int _wallTileId; // Store wall tile ID for easy access

        public override void Initialize()
        {
            base.Initialize();
            Core.ExitOnEscape = false;
        }

        public override void LoadContent()
        {
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

            // Initialize camera FIRST before using it
            _camera = new Camera2D(Core.GraphicsDevice.Viewport) { Zoom = 3.0f };

            _dungeonManager = new ProceduralDungeonManager();
            _wallTileId = ProceduralDungeonManager.DefaultWallTileId;
            _tilemap = ((ProceduralDungeonManager)_dungeonManager).GenerateBasicDungeon(Content);
            _playerStart = ((ProceduralDungeonManager)_dungeonManager).GetPlayerStart(_tilemap);
            Rectangle dungeonBounds = new Rectangle(0, 0, _tilemap.Columns * (int)_tilemap.TileWidth, _tilemap.Rows * (int)_tilemap.TileHeight);

            // Gather dungeon element rectangles
            var dungeonRects = _dungeonManager.GetAllElements()
                .Select(e => {
                    var boundsProp = e.GetType().GetProperty("Bounds");
                    return boundsProp != null ? (Rectangle)boundsProp.GetValue(e) : Rectangle.Empty;
                })
                .Where(r => r != Rectangle.Empty)
                .ToList();

            // Gather wall rectangles using MapUtils
            var wallRects = MapUtils.GetWallRectangles(_tilemap, _wallTileId);
            allObstacles = wallRects.Concat(dungeonRects).ToList();

            // 1. Create NpcManager with a temporary empty WeaponManager (will be replaced)
            var tempNpcList = new List<NPC>();
            _weaponManager = new WeaponManager(_heroAtlas, _weaponAtlas, dungeonBounds, tempNpcList);

            // 2. Now create NpcManager with the real WeaponManager
            _npcManager = new NpcManager(_heroAtlas, dungeonBounds, _tilemap, _wallTileId, _weaponManager, _weaponAtlas, _arrowAtlas);
            _npcManager.SpawnAllNpcTypesTest();
            _weaponManager = new WeaponManager(_heroAtlas, _weaponAtlas, dungeonBounds, _npcManager.Npcs as List<NPC>);

            // Create player BEFORE initializing singletons (so we can use it in callbacks)
            _player = new Player(
                _heroAtlas, _playerStart, _bounceSoundEffect, _collectSoundEffect, _playerAttackSoundEffect, MOVEMENT_SPEED
            );

            // Initialize singletons ONCE with the now-initialized camera and player
            SingletonManager.InitializeForGameScene(
                _atlas, _font, _debugFont, _uiSoundEffect, _camera, MOVEMENT_SPEED,
                (movement) => _player.Move(movement, dungeonBounds, _player.Sprite.Width, _player.Sprite.Height, _npcManager.Npcs, _tilemap, _wallTileId, allObstacles ?? new List<Rectangle>()),
                () => {
                    // NPC spawn logic
                    float swingRadius = _player.EquippedWeapon?.Length ?? 32f;
                    float minDistance = swingRadius + 16f;
                    var random = new Random();
                    double angle = random.NextDouble() * Math.PI * 2;
                    Vector2 direction = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
                    Vector2 spawnPos = _player.Position + direction * minDistance;
                    spawnPos.X = MathHelper.Clamp(spawnPos.X, dungeonBounds.Left, dungeonBounds.Right - _player.Sprite.Width);
                    spawnPos.Y = MathHelper.Clamp(spawnPos.Y, dungeonBounds.Top, dungeonBounds.Bottom - _player.Sprite.Height);
                    _npcManager.SpawnNPC("DefaultNPCType", spawnPos);
                },
                () => _player.CombatController.StartProjectileAttack(),
                () => _player.CombatController.StartMeleeAttack(),
                RotatePlayerWeaponLeft,
                RotatePlayerWeaponRight,
                HandleInteraction
            );

            CombatManager.Initialize(
                _npcManager, _player, ScoreManager.Instance, Core.SpriteBatch, CombatEffectsManager.Instance,
                _bounceSoundEffect, _collectSoundEffect, dungeonBounds
            );

            _playerWeapons = new List<Weapon>
            {
                new Weapon("Dagger", DataManager.Instance.GetWeaponStats("Dagger"), _weaponAtlas, _arrowAtlas),
                new Weapon("Dagger-Copper", DataManager.Instance.GetWeaponStats("Dagger-Copper"), _weaponAtlas, _arrowAtlas),
                new Weapon("Dagger-Cold", DataManager.Instance.GetWeaponStats("Dagger-Cold"), _weaponAtlas, _arrowAtlas)
            };
            _weaponManager.EquipWeapon(_player, _playerWeapons[_currentPlayerWeaponIndex]);

            _dialogManager = new DialogManager(GameUIManager.Instance, _player, _npcManager.Characters, dialogDistance);
        }

        private void HandleInteraction()
        {
            var aSwitch = _dungeonManager.GetElement<DungeonSwitch>("switch_1");
            if (aSwitch != null && _player.IsNearTile(aSwitch.Column, aSwitch.Row, _tilemap.TileWidth, _tilemap.TileHeight))
            {
                aSwitch.Activate();
            }
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

            if (InputHandler.Instance.WasPausePressed())
            {
                DebugManager.Instance.ShowUIDebugGrid = !DebugManager.Instance.ShowUIDebugGrid; // Use singleton
            }

            // Handle Pause/Unpause
            if (Core.Input.Keyboard.WasKeyJustPressed(Keys.Escape))
            {
                if (GameUIManager.Instance.IsPausePanelVisible)
                    GameUIManager.Instance.ResumeGame(_uiSoundEffect);
                else
                    GameUIManager.Instance.PauseGame();
            }

            // Dialog close with Enter
            if (GameUIManager.Instance.IsDialogOpen && Core.Input.Keyboard.WasKeyJustPressed(Keys.Enter))
            {
                GameUIManager.Instance.HideDialog();
                return;
            }

            // If pause panel is visible, let Gum handle the input.
            if (GameUIManager.Instance.IsPausePanelVisible)
            {
                return;
            }

            InputHandler.Instance.Update(gameTime);
            _player.Update(gameTime, _npcManager.Npcs);
            _npcManager.Update(gameTime, _player, allObstacles);
            CombatManager.Instance.Update(gameTime);
            CombatEffectsManager.Instance.Update(gameTime);
            _dialogManager.Update();
            _player.ClampToBounds(new Rectangle(0, 0, _tilemap.Columns * (int)_tilemap.TileWidth, _tilemap.Rows * (int)_tilemap.TileHeight));
            GameUIManager.Instance.UpdateWeaponUI(_player.EquippedWeapon);

            UpdateViewport(Core.GraphicsDevice.Viewport);

            // Camera follow logic - now using the properly initialized _camera
            Rectangle dungeonBounds = new Rectangle(0, 0, _tilemap.Columns * (int)_tilemap.TileWidth, _tilemap.Rows * (int)_tilemap.TileHeight);
            Rectangle margin = new Rectangle(
                (int)(100 / _camera.Zoom), (int)(80 / _camera.Zoom),
                (int)(_viewport.Width / _camera.Zoom) - (int)(200 / _camera.Zoom),
                (int)(_viewport.Height / _camera.Zoom) - (int)(160 / _camera.Zoom)
            );
            
            // Follow player with margin and clamp to dungeon bounds
            _camera.FollowWithMargin(_player.Position, margin, 0.1f);
            _camera.ClampToMap(_tilemap.Columns, _tilemap.Rows, (int)_tilemap.TileWidth);
            _camera.Update(gameTime);

            // Update player movement
            _player.Move(
                InputHandler.Instance.GetMovement(), dungeonBounds, _player.Sprite.Width, _player.Sprite.Height,
                _npcManager.Npcs, _tilemap, _wallTileId, allObstacles ?? new List<Rectangle>());

            // Update NPCs
            foreach (var npc in _npcManager.Npcs)
                npc.Update(gameTime, _npcManager.Npcs, _player, allObstacles);
        }

        public void UpdateViewport(Viewport viewport)
        {
            _viewport = viewport;
        }

        public override void DrawWorld(GameTime gameTime)
        {
            // Draw procedural tilemap
            _tilemap.Draw(Core.SpriteBatch);

            // Draw entities
            _player.Draw(Core.SpriteBatch);
            foreach (var npc in _npcManager.Npcs)
                npc.Draw(Core.SpriteBatch);

            // Draw debug overlays that should follow the camera
            GameUIManager.Instance.DrawDungeonElementCollisionBoxes(Core.SpriteBatch, _dungeonManager.GetAllElements(), _camera.GetViewMatrix());
            DebugManager.Instance.Draw(Core.SpriteBatch, _player, _npcManager.Npcs, _tilemap, ProceduralDungeonManager.DefaultWallTileId, _dungeonManager.GetAllElements(), _camera.GetViewMatrix());
        }

        public override void DrawUI(GameTime gameTime)
        {
            GumService.Default.Draw();
            // Draw UI in screen space (no camera transform)
            GameUIManager.Instance.DrawPlayerHealthBar(Core.SpriteBatch, _player, new Vector2(20, 20), new Vector2(100, 12));
            ScoreManager.Instance.Draw(Core.SpriteBatch);
            GameUIManager.Instance.DrawDebugInfo(Core.SpriteBatch, gameTime, _player.Position, _camera.Position, _viewport);

            // Draw the UI debug grid if enabled
            if (DebugManager.Instance?.ShowUIDebugGrid == true)
            {
                DebugManager.Instance.DrawUIDebugGrid(Core.SpriteBatch, Core.GraphicsDevice.Viewport, 40, 40, Color.Black * 0.25f, _debugFont);
            }
        }

        // Optionally, override Draw to do nothing or throw NotImplementedException
        public override void Draw(GameTime gameTime)
        {
            // Intentionally left blank; use DrawWorld and DrawUI instead.
        }

        public Matrix GetViewMatrix()
        {
            return CameraManager.Instance.GetViewMatrix();
        }

        protected override void Dispose(bool disposing)
        {
            if (IsDisposed) return;

            if (disposing)
            {
                // Remove all UI elements from the global Gum root
                // to prevent them from capturing input in other scenes.
                GumService.Default.Root.Children.Clear();
            }

            base.Dispose(disposing);
        }
    }
}