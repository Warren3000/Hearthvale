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
using MonoGame.Extended.Tiled;
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
        private bool _f5WasDown = false;
        private DebugKeysBar _debugKeysBar;
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
        // Remove _camera field - we'll use CameraManager instead

        private DungeonManager _dungeonManager;
        private MonoGameLibrary.Graphics.Tilemap _tilemap;

        // Managers
        private NpcManager _npcManager;
        private DialogManager _dialogManager;
        private WeaponManager _weaponManager;
        private MapManager _mapManager; // Add MapManager for CameraManager integration

        private Player _player;
        private List<Weapon> _playerWeapons;
        private int _currentPlayerWeaponIndex = 0;
        public Player Player => _player;

        private List<Rectangle> allObstacles = new List<Rectangle>();


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

            // Initialize camera and CameraManager
            var camera = new Camera2D(Core.GraphicsDevice.Viewport) { Zoom = 3.0f };
            CameraManager.Initialize(camera);

            // Create the procedural dungeon manager and initialize it as the singleton
            _dungeonManager = new ProceduralDungeonManager();
            DungeonManager.Initialize(_dungeonManager);

            // Define dungeon assets and dimensions
            int dungeonColumns = 100;
            int dungeonRows = 80;
            string wallTilesetPath = "Tilesets/DampDungeons/Tiles/dungeon-autotiles-walls";
            Rectangle wallTilesetRect = new Rectangle(0, 0, 256, 192);
            var wallAutotileXml = ConfigurationManager.Instance.LoadConfiguration("Content/Tilesets/DampDungeons/Tiles/Autotiles-Dungeon.xml");
            string floorTilesetPath = "Tilesets/DampDungeons/Tiles/Dungeon_WallsAndFloors";
            Rectangle floorTilesetRect = new Rectangle(0, 0, 96, 512);
            var floorAutotileXml = ConfigurationManager.Instance.LoadConfiguration("Content/Tilesets/DampDungeons/Tiles/Autotiles.xml");

            
            // Create tilesets here and assign the wall tileset to the class field
            var wallTileset = new Tileset(new TextureRegion(Content.Load<Texture2D>(wallTilesetPath), wallTilesetRect.X, wallTilesetRect.Y, wallTilesetRect.Width, wallTilesetRect.Height), ProceduralDungeonManager.AutotileSize, ProceduralDungeonManager.AutotileSize);

            var floorTileset = new Tileset(new TextureRegion(Content.Load<Texture2D>(floorTilesetPath), floorTilesetRect.X, floorTilesetRect.Y, floorTilesetRect.Width, floorTilesetRect.Height), ProceduralDungeonManager.AutotileSize, ProceduralDungeonManager.AutotileSize);

            TilesetManager.Instance.SetTilesets(wallTileset, floorTileset); // Ensure TilesetManager is initialized

            _tilemap = ((ProceduralDungeonManager)_dungeonManager).GenerateBasicDungeon(
                Content,
                dungeonColumns, dungeonRows,
                wallTileset, wallAutotileXml,
                floorTileset, floorAutotileXml
            );
            _playerStart = ((ProceduralDungeonManager)_dungeonManager).GetPlayerStart(_tilemap);

            // Validate player start position
            if (float.IsNaN(_playerStart.X) || float.IsNaN(_playerStart.Y))
            {
                System.Diagnostics.Debug.WriteLine("❌ CRITICAL: Player start position is NaN!");
                _playerStart = new Vector2(100, 100); // Emergency fallback
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"✅ Valid player start: {_playerStart}");
            }

            Rectangle dungeonBounds = new Rectangle(0, 0, _tilemap.Columns * (int)_tilemap.TileWidth, _tilemap.Rows * (int)_tilemap.TileHeight);

            // Create MapManager for CameraManager integration
            _mapManager = CreateMapManagerFromTilemap(_tilemap);

            // Gather dungeon element rectangles
            var dungeonRects = _dungeonManager.GetAllElements()
                .Select(e => {
                    var boundsProp = e.GetType().GetProperty("Bounds");
                    return boundsProp != null ? (Rectangle)boundsProp.GetValue(e) : Rectangle.Empty;
                })
                .Where(r => r != Rectangle.Empty)
                .ToList();

            // Gather wall rectangles using MapUtils - this now properly handles autotiled walls
            var wallRects = MapUtils.GetWallRectangles(_tilemap);
            allObstacles = wallRects.Concat(dungeonRects).ToList();

            // Create player and set up collision properties BEFORE spawning NPCs
            _player = new Player(
                _heroAtlas, _playerStart, _bounceSoundEffect, _collectSoundEffect, _playerAttackSoundEffect, MOVEMENT_SPEED
            );
            // Pass tilemap and wall info to the player for collision
            _player.Tilemap = _tilemap;

            // Initialize managers that depend on the player or other systems
            _weaponManager = new WeaponManager(_heroAtlas, _weaponAtlas, dungeonBounds, new List<NPC>());
            _npcManager = new NpcManager(_heroAtlas, dungeonBounds, _tilemap, _weaponManager, _weaponAtlas, _arrowAtlas);

            // Now that managers are ready, spawn NPCs and update weapon manager's NPC list
            _npcManager.SpawnAllNpcTypesTest(_player);
            _weaponManager.UpdateNpcList(_npcManager.Npcs);

            System.Diagnostics.Debug.WriteLine($"Camera initialized to player position: {CameraManager.Instance.Position}");
            System.Diagnostics.Debug.WriteLine($"Player position after creation: {_player.Position}");

            // Initialize singletons ONCE with the now-initialized camera and player
            SingletonManager.InitializeForGameScene(
                _atlas, _font, _debugFont, _uiSoundEffect, camera, MOVEMENT_SPEED,
                (movement) => _player.Move(movement, dungeonBounds, _player.Sprite.Width, _player.Sprite.Height, _npcManager.Npcs, allObstacles ?? new List<Rectangle>()),
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

            // Initialize the debug viewer for tilesets
            TilesetDebugManager.Initialize(_debugFont);
            var debugKeys = new List<(string, string)>
            {
                ("F2", "Grid"),
                ("F3", "Debug"),
                ("F4", "Tileset Viewer"),
                ("F5", "Coords"),
                ("F6", "Tile Coords"), // Add the new F6 key for tile coordinates
                // Add more as needed
            };
            _debugKeysBar = new DebugKeysBar(_font, GameUIManager.Instance.WhitePixel, debugKeys);
        }

        public override void Update(GameTime gameTime)
        {
            // Update the dungeon manager singleton
            DungeonManager.Instance.Update(gameTime);

            // Only update Gum UI when paused or dialog is open
            if (GameUIManager.Instance.IsPausePanelVisible || GameUIManager.Instance.IsDialogOpen)
            {
                GumService.Default.Update(gameTime);
                return;
            }

            // InputHandler now handles all input including UI/Debug
            InputHandler.Instance.Update(gameTime);

            // Add debug output to see what's happening
            if (gameTime.TotalGameTime.TotalSeconds % 1.0 < 0.016) // Every second roughly
            {
                System.Diagnostics.Debug.WriteLine($"DEBUG: Player Position: {_player?.Position ?? Vector2.Zero}");
                System.Diagnostics.Debug.WriteLine($"DEBUG: Camera Position: {CameraManager.Instance.Position}");
                System.Diagnostics.Debug.WriteLine($"DEBUG: Player Start was: {_playerStart}");
            }

            // Update game systems
            _player.Update(gameTime, _npcManager.Npcs);
            _npcManager.Update(gameTime, _player, allObstacles);
            CombatManager.Instance.Update(gameTime);
            CombatEffectsManager.Instance.Update(gameTime);
            _dialogManager.Update();
            _player.ClampToBounds(new Rectangle(0, 0, _tilemap.Columns * (int)_tilemap.TileWidth, _tilemap.Rows * (int)_tilemap.TileHeight));
            GameUIManager.Instance.UpdateWeaponUI(_player.EquippedWeapon);

            UpdateViewport(Core.GraphicsDevice.Viewport);

            // Update camera using CameraManager
            Rectangle margin = new Rectangle(
                (int)(100 / CameraManager.Instance.Zoom), (int)(80 / CameraManager.Instance.Zoom),
                (int)(_viewport.Width / CameraManager.Instance.Zoom) - (int)(200 / CameraManager.Instance.Zoom),
                (int)(_viewport.Height / CameraManager.Instance.Zoom) - (int)(160 / CameraManager.Instance.Zoom)
            );
            
            CameraManager.Instance.UpdateCamera(
                _player.Position, 
                new Point((int)_player.Sprite.Width, (int)_player.Sprite.Height),
                margin, 
                _mapManager, 
                CombatEffectsManager.Instance, 
                gameTime
            );

            // Update player movement
            _player.Move(
                InputHandler.Instance.GetMovement(), 
                new Rectangle(0, 0, _tilemap.Columns * (int)_tilemap.TileWidth, _tilemap.Rows * (int)_tilemap.TileHeight), 
                _player.Sprite.Width, _player.Sprite.Height,
                _npcManager.Npcs, allObstacles ?? new List<Rectangle>());

            // Update NPCs
            foreach (var npc in _npcManager.Npcs)
                npc.Update(gameTime, _npcManager.Npcs, _player, allObstacles);

            if (Keyboard.GetState().IsKeyDown(Keys.F5) && !_f5WasDown)
            {
                TilesetDebugManager.ToggleShowTileCoordinates();
            }
            _f5WasDown = Keyboard.GetState().IsKeyDown(Keys.F5);
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
            GameUIManager.Instance.DrawDungeonElementCollisionBoxes(Core.SpriteBatch, DungeonManager.Instance.GetAllElements(), CameraManager.Instance.GetViewMatrix());
            // Use GameUIManager for tile coordinates overlay instead of TilesetDebugManager
            GameUIManager.Instance.DrawTileCoordinatesOverlay(Core.SpriteBatch, _tilemap);
            DebugManager.Instance.Draw(Core.SpriteBatch, _player, _npcManager.Npcs, DungeonManager.Instance.GetAllElements(), CameraManager.Instance.GetViewMatrix());
        }

        public override void DrawUI(GameTime gameTime)
        {
            GumService.Default.Draw();

            // Draw UI in screen space (no camera transform)
            // Position health bar in top-left with proper spacing
            var healthBarPosition = new Vector2(25, 25);
            var healthBarSize = new Vector2(120, 16);
            GameUIManager.Instance.DrawPlayerHealthBar(Core.SpriteBatch, _player, healthBarPosition, healthBarSize);

            // Score is now positioned in top-right by ScoreManager
            ScoreManager.Instance.Draw(Core.SpriteBatch);

            // Use CameraManager for camera position
            GameUIManager.Instance.DrawDebugInfo(
                Core.SpriteBatch,
                gameTime,
                _player?.Position ?? Vector2.Zero,
                CameraManager.Instance.Position,
                _viewport
            );

            // Draw the UI debug grid if enabled
            if (DebugManager.Instance?.ShowUIDebugGrid == true)
            {
                DebugManager.Instance.DrawUIDebugGrid(Core.SpriteBatch, Core.GraphicsDevice.Viewport, 40, 40, Color.Black * 0.25f, _debugFont);
            }

            // Draw the tileset debug viewer if enabled
            TilesetDebugManager.Draw(Core.SpriteBatch);
            _debugKeysBar.Draw(Core.SpriteBatch, Core.GraphicsDevice.Viewport.Width, Core.GraphicsDevice.Viewport.Height);
        }

        // Optionally, override Draw to do nothing or throw NotImplementedException
        public override void Draw(GameTime gameTime)
        {
            // Intentionally left blank; use DrawWorld and DrawUI instead.
        }
        
        private void HandleInteraction()
        {
            // Now we can use the singleton instance
            var aSwitch = DungeonManager.Instance.GetElement<DungeonSwitch>("switch_1");
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

        public Matrix GetViewMatrix()
        {
            return CameraManager.Instance.GetViewMatrix();
        }

        /// <summary>
        /// Creates a MapManager from the procedural tilemap for CameraManager integration
        /// </summary>
        private MapManager CreateMapManagerFromTilemap(Tilemap tilemap)
        {
            // This is a temporary implementation to provide MapManager functionality
            // You may need to create a proper MapManager implementation or modify CameraManager
            // For now, we'll create a simple wrapper
            return new TilemapMapManager(tilemap, _playerStart);
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

    /// <summary>
    /// Simple MapManager implementation that wraps a Tilemap for CameraManager compatibility
    /// </summary>
    public class TilemapMapManager : MapManager
    {
        private readonly Tilemap _tilemap;
        private readonly Vector2 _playerSpawnPoint;

        public TilemapMapManager(Tilemap tilemap, Vector2 playerSpawnPoint)
            : base() // Use the protected parameterless constructor
        {
            _tilemap = tilemap ?? throw new ArgumentNullException(nameof(tilemap));
            _playerSpawnPoint = playerSpawnPoint;
        }

        // Override properties to use our tilemap
        public override int MapWidthInPixels => _tilemap.Columns * (int)_tilemap.TileWidth;
        public override int MapHeightInPixels => _tilemap.Rows * (int)_tilemap.TileHeight;
        public override int TileWidth => (int)_tilemap.TileWidth;
        public override int TileHeight => (int)_tilemap.TileHeight;

        public override void Update(GameTime gameTime)
        {
            // No update needed for static tilemap - don't call base
        }

        public override void Draw(Matrix transform)
        {
            // Drawing is handled separately in GameScene - don't call base
        }

        public override Vector2 GetPlayerSpawnPoint()
        {
            return _playerSpawnPoint;
        }

        public override TiledMapObjectLayer GetObjectLayer(string name)
        {
            // Not applicable for procedural tilemaps
            return null;
        }
    }
}