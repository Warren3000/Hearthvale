using Hearthvale.GameCode.Data;
using Hearthvale.GameCode.Data.Atlases;
using Hearthvale.GameCode.Entities;
using Hearthvale.GameCode.Entities.Interfaces;
using Hearthvale.GameCode.Entities.NPCs;
using Hearthvale.GameCode.Entities.Players;
using Hearthvale.GameCode.Input;
using Hearthvale.GameCode.Managers;
using Hearthvale.GameCode.Managers.Dungeon;
using Hearthvale.GameCode.Rendering;
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
using SharpDX.Direct2D1;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Hearthvale.Scenes
{
    public class GameScene : Scene, ICameraProvider
    {
        private readonly CombatEffectsManager _combatEffectsManager;
        private readonly CameraManager _cameraManager;
        private readonly InputHandler _inputHandler;

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

        private TextureAtlas _heroAtlas;
        private TextureAtlas _npcAtlas;
        private TextureAtlas _weaponAtlas;
        private TextureAtlas _arrowAtlas;
        private INpcAtlasCatalog _npcAtlasCatalog;

        private Viewport _viewport;

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
        public GameScene(
            CombatEffectsManager combatEffectsManager,
            CameraManager cameraManager,
            InputHandler inputHandler
        )
        {
            _combatEffectsManager = combatEffectsManager ?? throw new ArgumentNullException(nameof(combatEffectsManager));
            _cameraManager = cameraManager ?? throw new ArgumentNullException(nameof(cameraManager));
            _inputHandler = inputHandler ?? throw new ArgumentNullException(nameof(inputHandler));
        }
        public override void Initialize()
        {
            base.Initialize();
            Core.ExitOnEscape = false;
        }
        public override void LoadContent()
        {
            _heroAtlas = TextureAtlas.FromFile(Core.Content, "images/xml/warrior-atlas.xml");
            _npcAtlas = TextureAtlas.FromFile(Core.Content, "images/xml/skeleton-atlas.xml");
            _weaponAtlas = TextureAtlas.FromFile(Core.Content, "images/xml/weapon-atlas.xml");
            _arrowAtlas = TextureAtlas.FromFile(Core.Content, "images/xml/arrow-atlas.xml");

            _npcAtlasCatalog = new ManifestNpcAtlasCatalog(Core.Content);
            _npcAtlasCatalog.LoadManifest("atlas-configs/skeleton-npc.json");
            _npcAtlasCatalog.LoadManifest("atlas-configs/goblin-npc.json");
            _npcAtlasCatalog.LoadManifest("atlas-configs/warrior-npc.json");

            _bounceSoundEffect = Content.Load<SoundEffect>("audio/bounce");
            _collectSoundEffect = Content.Load<SoundEffect>("audio/collect");
            _uiSoundEffect = Core.Content.Load<SoundEffect>("audio/ui");
            _playerAttackSoundEffect = Content.Load<SoundEffect>("audio/player_attack");
            _font = Core.Content.Load<SpriteFont>("fonts/04B_30");
            _debugFont = Content.Load<SpriteFont>("fonts/DebugFont");

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
                floorTileset, floorAutotileXml);

            // Debug tilemap information
            System.Diagnostics.Debug.WriteLine($"✅ Tilemap created: {_tilemap.Columns}x{_tilemap.Rows}");
            System.Diagnostics.Debug.WriteLine($"✅ Tile size: {_tilemap.TileWidth}x{_tilemap.TileHeight}");
            
            // Check if tilemap has any tiles
            int nonEmptyTiles = 0;
            for (int x = 0; x < _tilemap.Columns; x++)
            {
                for (int y = 0; y < _tilemap.Rows; y++)
                {
                    if (_tilemap.GetTileId(x, y) > 0)
                        nonEmptyTiles++;
                }
            }
            System.Diagnostics.Debug.WriteLine($"✅ Non-empty tiles: {nonEmptyTiles}");

            TilesetManager.Instance.SetTilemap(_tilemap); // Set the tilemap in TilesetManager
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
            
            // Debug output for world bounds
            System.Diagnostics.Debug.WriteLine($"✅ MapManager created with RoomBounds: {_mapManager.RoomBounds}");

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

            // Create player BEFORE spawning NPCs
            _player = new Player(
                _heroAtlas, _playerStart, _bounceSoundEffect, _collectSoundEffect, _playerAttackSoundEffect, MOVEMENT_SPEED
            );

            // Initialize managers (weapon first, then NPC which creates wall colliders)
            _weaponManager = new WeaponManager(_heroAtlas, _weaponAtlas, dungeonBounds, new List<NPC>());
            _npcManager = new NpcManager(
                _heroAtlas,
                dungeonBounds,
                _tilemap,
                _weaponManager,
                _npcAtlasCatalog,
                _npcAtlas,
                _weaponAtlas,
                _arrowAtlas);

            // Register player with physics collision system so walls/chests block movement
            _npcManager.RegisterPlayer(_player);

            // Register all existing dungeon loot containers (chests) with the collision world
            // so that they participate in movement blocking (player & NPC physics queries).
            var dungeonLoots = DungeonManager.Instance.GetElements<DungeonLoot>();
            _npcManager.CollisionManager.RegisterChests(dungeonLoots);

            // Spawn NPC test set and sync weapon manager
            _npcManager.SpawnAllNpcTypesTest(_player);
            _weaponManager.UpdateNpcList(_npcManager.Npcs);

            InputManagerInitializer.InitializeForGameScene(
                CameraManager.Instance.Camera2D,
                MOVEMENT_SPEED,
                movement => _player.Move(
                    movement,
                    new Rectangle(0, 0, _tilemap.Columns * (int)_tilemap.TileWidth, _tilemap.Rows * (int)_tilemap.TileHeight),
                    _player.Bounds.Width,  
                    _player.Bounds.Height, 
                    _npcManager.Npcs,
                    allObstacles ?? new List<Rectangle>()
                ),
                () => _npcManager.SpawnRandomNpcAroundPlayer(_player),
                () => _player.CombatController.StartProjectileAttack(),
                () => _player.CombatController.StartMeleeAttack(),
                RotatePlayerWeaponLeft,
                RotatePlayerWeaponRight,
                HandleInteraction
            );

            CombatManager.Initialize(
                _npcManager,
                _player,
                ScoreManager.Instance,
                Core.SpriteBatch,
                _combatEffectsManager, // Use the injected instance
                _bounceSoundEffect,
                _collectSoundEffect,
                dungeonBounds
            );

            _playerWeapons = new List<Weapon>
            {
                new Weapon("Dagger-Copper", DataManager.Instance.GetWeaponStats("Dagger-Copper"), _weaponAtlas, _arrowAtlas),
                new Weapon("Dagger-Fire", DataManager.Instance.GetWeaponStats("Dagger-Fire"), _weaponAtlas, _arrowAtlas),
                new Weapon("Dagger-Nature", DataManager.Instance.GetWeaponStats("Dagger-Nature"), _weaponAtlas, _arrowAtlas)
            };
            _weaponManager.EquipWeapon(_player, _playerWeapons[_currentPlayerWeaponIndex]);
            _dialogManager = new DialogManager(GameUIManager.Instance, _player, _npcManager.Characters, dialogDistance);

            // Initialize the debug viewer for tilesets
            TilesetDebugManager.Initialize(_debugFont);
            var debugKeys = new List<(string, string)>
            {
                ("F1", "Debug UI"),
                ("F2", "Grid"),
                ("F3", "AI"),
                ("F4", "Weapon"),
                ("F5", "Tileset Viewer"),
                ("F6", "Tile Coords"), 
                ("F7", "Collision Bounds"), 
                // Add more as needed
            };
            _debugKeysBar = new DebugKeysBar(_font, GameUIManager.Instance.WhitePixel, debugKeys);


            if (CameraManager.Instance?.Camera2D != null)
            {
                CameraManager.Instance.Position = _playerStart;
                System.Diagnostics.Debug.WriteLine($"✅ Camera repositioned to: {CameraManager.Instance.Position}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("❌ CameraManager not initialized in LoadContent!");
            }

            // Add debug output
            System.Diagnostics.Debug.WriteLine($"✅ Camera initialized with position: {CameraManager.Instance.Position}");
            System.Diagnostics.Debug.WriteLine($"✅ Player start position: {_playerStart}");
            System.Diagnostics.Debug.WriteLine($"✅ Camera zoom: {CameraManager.Instance.Zoom}");
        }

        public override void Update(GameTime gameTime)
        {
            // Update the dungeon manager singleton
            DungeonManager.Instance.Update(gameTime);

            // Keep chest physics actors in sync with any animation/opening state changes
            _npcManager?.CollisionManager.SyncChestPositions();

            // Only update Gum UI when paused or dialog is open
            if (GameUIManager.Instance.IsPausePanelVisible || GameUIManager.Instance.IsDialogOpen)
            {
                GumService.Default.Update(gameTime);
                return;
            }

            // InputHandler now handles all input including UI/Debug
            InputHandler.Instance.Update(gameTime);

            // Update game systems
            _player.Update(gameTime, _npcManager.Npcs);
            _npcManager.Update(gameTime, _player, allObstacles);
            CombatManager.Instance.Update(gameTime);
            _combatEffectsManager.Update(gameTime);
            _dialogManager.Update();
            _player.ClampToBounds(new Rectangle(0, 0, _tilemap.Columns * (int)_tilemap.TileWidth, _tilemap.Rows * (int)_tilemap.TileHeight));
            GameUIManager.Instance.UpdateWeaponUI(_player.EquippedWeapon);

            UpdateViewport(Core.GraphicsDevice.Viewport);
            
            // Camera handling
            // Use a stable 32x32 anchor based on logical player position instead of tight sprite bounds that bounce with animation.
            // Assumption: Player sprites are 32x32 logical tiles; adjust if atlas changes.
            const int logicalSpriteSize = 32; // stable reference size
            Vector2 stableCenter = new Vector2(
                _player.Position.X + logicalSpriteSize / 2f,
                _player.Position.Y + logicalSpriteSize / 2f
            );

            // Update via CameraManager with smoothing (reduced vertical jitter)
            CameraManager.Instance.Update(
                stableCenter,
                _tilemap.Columns,
                _tilemap.Rows,
                (int)_tilemap.TileWidth,
                gameTime,
                null // default smoothing
            );

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
            DungeonLootRenderer.Draw(Core.SpriteBatch, DungeonManager.Instance.GetElements<DungeonLoot>(), layerDepth: 0.55f);
            // Draw entities
            _player.Draw(Core.SpriteBatch);
            foreach (var npc in _npcManager.Npcs)
                npc.Draw(Core.SpriteBatch);

#if DEBUG
    DebugDrawWeaponProbe();
#endif

    // Draw debug overlays that should follow the camera
    GameUIManager.Instance.DrawDungeonElementCollisionBoxes(Core.SpriteBatch, DungeonManager.Instance.GetAllElements(), CameraManager.Instance.GetViewMatrix());
    GameUIManager.Instance.DrawTileCoordinatesOverlay(Core.SpriteBatch, _tilemap);
    
    // UPDATED: Pass the debug font and wall rectangles for collision bounds debug overlay
    DebugManager.Instance.Draw(Core.SpriteBatch, _player, _npcManager.Npcs, DungeonManager.Instance.GetAllElements(), CameraManager.Instance.GetViewMatrix(), _debugFont, allObstacles);
}

#if DEBUG
private void DebugDrawWeaponProbe()
{
    if ((Log.EnabledAreas & LogArea.Probe) == 0) return;
    var white = GameUIManager.Instance.WhitePixel;

    var weapon = _player?.EquippedWeapon;
    if (weapon == null) { System.Diagnostics.Debug.WriteLine("[Probe] No equipped weapon."); return; }

    // The "center" supplied to Weapon.Draw
    Vector2 characterCenter = _player.Position + new Vector2(_player.Bounds.Width / 2f, _player.Bounds.Height / 1.4f);

    // 1) Mark the characterCenter with a small cross
    Core.SpriteBatch.Draw(white, new Rectangle((int)characterCenter.X - 2, (int)characterCenter.Y - 2, 5, 1), Color.Magenta);
    Core.SpriteBatch.Draw(white, new Rectangle((int)characterCenter.X - 2, (int)characterCenter.Y + 2, 5, 1), Color.Magenta);
    Core.SpriteBatch.Draw(white, new Rectangle((int)characterCenter.X - 2, (int)characterCenter.Y - 2, 1, 5), Color.Magenta);
    Core.SpriteBatch.Draw(white, new Rectangle((int)characterCenter.X + 2, (int)characterCenter.Y - 2, 1, 5), Color.Magenta);

    // 2) After _player.Draw, Weapon.Draw has run, so Position is set
    var weaponPos = weapon.Sprite?.Position ?? weapon.Position;

    // Mark the weapon draw position
    Core.SpriteBatch.Draw(white, new Rectangle((int)weaponPos.X - 1, (int)weaponPos.Y - 1, 3, 3), Color.Yellow);

    // 3) Draw a line from characterCenter to weaponPos (to visualize offset/origin)
    var dir = weaponPos - characterCenter;
    var len = dir.Length();
    if (len > 0.1f)
    {
        float angle = (float)Math.Atan2(dir.Y, dir.X);
        Core.SpriteBatch.Draw(white, weaponPos, null, Color.Yellow * 0.7f, angle + MathF.PI, Vector2.Zero, new Vector2(len, 1f), SpriteEffects.None, 0);
    }

    // 4) Draw the raw atlas region at the weapon position (bypass AnimatedSprite)
    var region = weapon.Sprite?.Region;
    if (region != null && _weaponAtlas?.Texture != null)
    {
        var src = region.SourceRectangle;
        // Slight offset so we see both the raw region and the AnimatedSprite if both render
        var rawPos = weaponPos + new Vector2(18, -18);
        Core.SpriteBatch.Draw(_weaponAtlas.Texture, rawPos, src, Color.White, 0f, new Vector2(src.Width / 2f, src.Height), 1f, SpriteEffects.None, 0f);

        System.Diagnostics.Debug.WriteLine($"[Probe] Draw raw region at {rawPos} src={src} weapon.Rotation={weapon.Rotation}");
    }
    else
    {
        System.Diagnostics.Debug.WriteLine("[Probe] Weapon region or atlas texture is null.");
    }
}
#endif

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
            try
            {
                // Ensure CameraManager is initialized
                if (CameraManager.Instance?.Camera2D == null)
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ CameraManager not initialized, returning identity matrix");
                    return Matrix.Identity;
                }

                var matrix = CameraManager.Instance.GetViewMatrix();
                
                // Validate the matrix isn't completely broken
                if (float.IsNaN(matrix.M41) || float.IsNaN(matrix.M42))
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ Camera matrix contains NaN values, returning identity matrix");
                    return Matrix.Identity;
                }

                return matrix;
            }
            catch (System.InvalidOperationException ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ CameraManager not initialized: {ex.Message}");
                return Matrix.Identity;
            }
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

        private void HandleStartClicked(object sender, EventArgs e)
        {
            // Play UI sound effect
            Core.Audio.PlaySoundEffect(_uiSoundEffect);

            // Initialize the CameraManager with a new Camera2D instance
            var camera = new Camera2D(Core.GraphicsDevice.Viewport) { Zoom = 3.0f };
            CameraManager.Initialize(camera);

            // Initialize CombatEffectsManager
            var combatEffectsManager = new CombatEffectsManager();

            // Initialize InputHandler
            InputHandler.Initialize(
                movementSpeed: 2.0f, // Example movement speed
                movePlayerCallback: movement => { /* Define player movement logic */ },
                spawnNpcCallback: () => { /* Define NPC spawn logic */ },
                projectileAttackCallback: () => { /* Define projectile attack logic */ },
                meleeAttackCallback: () => { /* Define melee attack logic */ },
                rotateWeaponLeftCallback: () => { /* Define weapon rotation left logic */ },
                rotateWeaponRightCallback: () => { /* Define weapon rotation right logic */ },
                interactionCallback: () => { /* Define interaction logic */ }
            );

            // Transition to the main game scene
            Core.ChangeScene(new GameScene(
                combatEffectsManager,
                CameraManager.Instance,
                InputHandler.Instance
            ));
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
        
        // Override RoomBounds to calculate from our tilemap
        public new Rectangle RoomBounds => new Rectangle(0, 0, MapWidthInPixels, MapHeightInPixels);

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