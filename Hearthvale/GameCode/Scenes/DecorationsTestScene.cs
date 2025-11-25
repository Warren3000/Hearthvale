using Hearthvale.GameCode.Data.Atlases;
using Hearthvale.GameCode.Entities.Players;
using Hearthvale.GameCode.Input;
using Hearthvale.GameCode.Managers;
using Hearthvale.GameCode.Managers.Dungeon;
using Hearthvale.GameCode.Rendering;
using Hearthvale.GameCode.Tools;
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
using System.Linq;

namespace Hearthvale.Scenes
{
    public class DecorationsTestScene : Scene, ICameraProvider
    {
        private readonly InputHandler _inputHandler;
        private CameraManager _cameraManager;

        private Vector2 _cameraPosition;
        private const float CAMERA_SPEED = 500.0f;

        private TextureAtlas _heroAtlas;
        private TextureAtlas _weaponAtlas;
        private TextureAtlas _arrowAtlas;

        private SoundEffect _bounceSoundEffect;
        private SoundEffect _collectSoundEffect;
        private SoundEffect _playerAttackSoundEffect;
        private SpriteFont _font;
        private SpriteFont _debugFont;

        private DungeonManager _dungeonManager;
        private MonoGameLibrary.Graphics.Tilemap _tilemap;
        private DecorationTool _decorationTool;
        private DebugKeysBar _debugKeysBar;

        private Viewport _viewport;

        public DecorationsTestScene()
        {
            // Basic initialization if needed before LoadContent
        }

        public override void Initialize()
        {
            base.Initialize();
            Core.ExitOnEscape = false;
        }

        public override void LoadContent()
        {
            // Load Assets
            _heroAtlas = TextureAtlas.FromFile(Core.Content, "images/xml/warrior-atlas.xml");
            _weaponAtlas = TextureAtlas.FromFile(Core.Content, "images/xml/weapon-atlas.xml");
            _arrowAtlas = TextureAtlas.FromFile(Core.Content, "images/xml/arrow-atlas.xml");

            _bounceSoundEffect = Content.Load<SoundEffect>("audio/bounce");
            _collectSoundEffect = Content.Load<SoundEffect>("audio/collect");
            _playerAttackSoundEffect = Content.Load<SoundEffect>("audio/player_attack");
            _font = Core.Content.Load<SpriteFont>("fonts/04B_30");
            _debugFont = Content.Load<SpriteFont>("fonts/DebugFont");

            // Initialize Camera
            var camera = new Camera2D(Core.GraphicsDevice.Viewport) { Zoom = 3.0f };
            CameraManager.Initialize(camera);

            // Initialize Dungeon (Simple empty arena for testing)
            _dungeonManager = new ProceduralDungeonManager();
            DungeonManager.Initialize(_dungeonManager);

            int dungeonColumns = 40;
            int dungeonRows = 30;
            string wallTilesetPath = "Tilesets/DampDungeons/Tiles/dungeon-autotiles-walls";
            Rectangle wallTilesetRect = new Rectangle(0, 0, 256, 192);
            var wallAutotileXml = ConfigurationManager.Instance.LoadConfiguration("Content/Tilesets/DampDungeons/Tiles/Autotiles-Dungeon.xml");
            string floorTilesetPath = "Tilesets/DampDungeons/Tiles/Dungeon_WallsAndFloors";
            Rectangle floorTilesetRect = new Rectangle(0, 0, 96, 512);
            var floorAutotileXml = ConfigurationManager.Instance.LoadConfiguration("Content/Tilesets/DampDungeons/Tiles/Autotiles.xml");

            var wallTileset = new Tileset(new TextureRegion(Content.Load<Texture2D>(wallTilesetPath), wallTilesetRect.X, wallTilesetRect.Y, wallTilesetRect.Width, wallTilesetRect.Height), ProceduralDungeonManager.AutotileSize, ProceduralDungeonManager.AutotileSize);
            var floorTileset = new Tileset(new TextureRegion(Content.Load<Texture2D>(floorTilesetPath), floorTilesetRect.X, floorTilesetRect.Y, floorTilesetRect.Width, floorTilesetRect.Height), ProceduralDungeonManager.AutotileSize, ProceduralDungeonManager.AutotileSize);

            TilesetManager.Instance.SetTilesets(wallTileset, floorTileset);

            _tilemap = ((ProceduralDungeonManager)_dungeonManager).GenerateOpenArena(
                Content,
                dungeonColumns, dungeonRows,
                wallTileset, wallAutotileXml,
                floorTileset, floorAutotileXml);

            TilesetManager.Instance.SetTilemap(_tilemap);
            _cameraPosition = ((ProceduralDungeonManager)_dungeonManager).GetPlayerStart(_tilemap);

            // Initialize Input
            InputHandler.Initialize(
                0,
                movement => { },
                () => { }, // No spawn
                () => { }, // No projectile
                () => { }, // No melee
                () => { }, // No weapon rotate
                () => { }, // No weapon rotate
                () => { }  // No interaction
            );

            // Initialize Decoration Tool
            _decorationTool = new DecorationTool(_tilemap);
            
            // Load Decorations with Definition
            try
            {
                var decorationTexture = Content.Load<Texture2D>("Tilesets/DampDungeons/Tiles/DungeonDecorations");
                var definition = ConfigurationManager.Instance.LoadConfiguration<Hearthvale.GameCode.Data.DecorationSetDefinition>("Content/Tilesets/DampDungeons/Tiles/DungeonDecorations.json");
                
                if (definition != null)
                {
                    _decorationTool.LoadTilesetDefinition("Decorations", definition, decorationTexture);
                }
                else
                {
                    // Fallback if JSON fails
                    _decorationTool.AddTileset("Decorations", decorationTexture);
                }
                _decorationTool.IsActive = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load decorations: {ex.Message}");
            }
            
            // Add other tilesets for testing (Grid fallback)
            try 
            {
                _decorationTool.AddTileset("Walls", Content.Load<Texture2D>(wallTilesetPath));
                _decorationTool.AddTileset("Floors", Content.Load<Texture2D>(floorTilesetPath));
            }
            catch { /* Ignore if fails */ }

            // Debug UI
            TilesetDebugManager.Initialize(_debugFont);
            var debugKeys = new List<(string, string)>
            {
                ("F8", "Toggle Tool"),
                ("[ ]", "Cycle Tile"),
                ("LMB", "Place"),
                ("RMB", "Remove"),
                ("WASD", "Pan Camera")
            };
            _debugKeysBar = new DebugKeysBar(_font, GameUIManager.Instance.WhitePixel, debugKeys);

            CameraManager.Instance.Position = _cameraPosition;
        }

        public override void Update(GameTime gameTime)
        {
            InputHandler.Instance.Update(gameTime);
            
            UpdateViewport(Core.GraphicsDevice.Viewport);

            // Camera Panning
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            var keyboard = Keyboard.GetState();
            Vector2 movement = Vector2.Zero;

            if (keyboard.IsKeyDown(Keys.W) || keyboard.IsKeyDown(Keys.Up)) movement.Y -= 1;
            if (keyboard.IsKeyDown(Keys.S) || keyboard.IsKeyDown(Keys.Down)) movement.Y += 1;
            if (keyboard.IsKeyDown(Keys.A) || keyboard.IsKeyDown(Keys.Left)) movement.X -= 1;
            if (keyboard.IsKeyDown(Keys.D) || keyboard.IsKeyDown(Keys.Right)) movement.X += 1;

            if (movement != Vector2.Zero)
            {
                movement.Normalize();
                _cameraPosition += movement * CAMERA_SPEED * dt;
            }

            // Camera Update
            CameraManager.Instance.Update(
                _cameraPosition,
                _tilemap.Columns,
                _tilemap.Rows,
                (int)_tilemap.TileWidth,
                gameTime,
                null
            );

            // Decoration Tool
            if (Keyboard.GetState().IsKeyDown(Keys.F8) && InputHandler.Instance.IsNewKeyPress(Keys.F8))
            {
                if (_decorationTool != null)
                    _decorationTool.IsActive = !_decorationTool.IsActive;
            }

            if (_decorationTool != null)
                _decorationTool.Update(gameTime);
        }

        public void UpdateViewport(Viewport viewport)
        {
            _viewport = viewport;
        }

        public override void DrawWorld(GameTime gameTime)
        {
            _tilemap.Draw(Core.SpriteBatch);

            if (_decorationTool != null)
                _decorationTool.Draw(Core.SpriteBatch);
        }

        public override void DrawUI(GameTime gameTime)
        {
            GumService.Default.Draw();

            if (_decorationTool != null)
                _decorationTool.DrawUI(Core.SpriteBatch, _font);

            _debugKeysBar.Draw(Core.SpriteBatch, Core.GraphicsDevice.Viewport.Width, Core.GraphicsDevice.Viewport.Height);
        }

        public override void Draw(GameTime gameTime)
        {
            // Use DrawWorld and DrawUI
        }

        public Matrix GetViewMatrix()
        {
            return CameraManager.Instance?.GetViewMatrix() ?? Matrix.Identity;
        }
    }
}