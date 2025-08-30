using Hearthvale.GameCode.Managers;
using Hearthvale.GameCode.UI;
using Hearthvale.GameCode.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;
using MonoGameLibrary.Scenes;
using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace Hearthvale.GameCode.Scenes
{
    /// <summary>
    /// Test scene for visualizing all autotile wall configurations
    /// </summary>
    public class AutotileTestScene : Scene, ICameraProvider
    {
        private Tilemap _tilemap;
        private Tileset _wallTileset;
        private Tileset _floorTileset;
        private SpriteFont _font;
        private Camera2D _camera;
        private KeyboardState _previousKeyboardState;

        // Test pattern configurations
        private readonly List<(string name, int[,] pattern)> _testPatterns = new()
        {
            ("Isolated", new int[,] {
                { 0, 0, 0 },
                { 0, 1, 0 },
                { 0, 0, 0 }
            }),

            ("Horizontal", new int[,] {
                { 0, 0, 0 },
                { 1, 1, 1 },
                { 0, 0, 0 }
            }),

            ("Vertical", new int[,] {
                { 0, 1, 0 },
                { 0, 1, 0 },
                { 0, 1, 0 }
            }),

            ("Cross", new int[,] {
                { 0, 1, 0 },
                { 1, 1, 1 },
                { 0, 1, 0 }
            }),

            ("Corner TL", new int[,] {
                { 0, 0, 0 },
                { 0, 1, 1 },
                { 0, 1, 0 }
            }),

            ("Corner TR", new int[,] {
                { 0, 0, 0 },
                { 1, 1, 0 },
                { 0, 1, 0 }
            }),

            ("Corner BL", new int[,] {
                { 0, 1, 0 },
                { 0, 1, 1 },
                { 0, 0, 0 }
            }),

            ("Corner BR", new int[,] {
                { 0, 1, 0 },
                { 1, 1, 0 },
                { 0, 0, 0 }
            }),

            ("T-Junction Up", new int[,] {
                { 0, 1, 0 },
                { 1, 1, 1 },
                { 0, 0, 0 }
            }),

            ("T-Junction Down", new int[,] {
                { 0, 0, 0 },
                { 1, 1, 1 },
                { 0, 1, 0 }
            }),

            ("T-Junction Left", new int[,] {
                { 0, 1, 0 },
                { 0, 1, 1 },
                { 0, 1, 0 }
            }),

            ("T-Junction Right", new int[,] {
                { 0, 1, 0 },
                { 1, 1, 0 },
                { 0, 1, 0 }
            }),

            ("End Cap Up", new int[,] {
                { 0, 1, 0 },
                { 0, 1, 0 },
                { 0, 0, 0 }
            }),

            ("End Cap Down", new int[,] {
                { 0, 0, 0 },
                { 0, 1, 0 },
                { 0, 1, 0 }
            }),

            ("End Cap Left", new int[,] {
                { 0, 0, 0 },
                { 0, 1, 1 },
                { 0, 0, 0 }
            }),

            ("End Cap Right", new int[,] {
                { 0, 0, 0 },
                { 1, 1, 0 },
                { 0, 0, 0 }
            })
        };

        public override void Initialize()
        {
            base.Initialize();
            Core.ExitOnEscape = true;
        }

        public override void LoadContent()
        {
            _font = Content.Load<SpriteFont>("fonts/DebugFont");

            // Load tilesets
            var wallTexture = Content.Load<Texture2D>("Tilesets/DampDungeons/Tiles/dungeon-autotiles-walls");
            var floorTexture = Content.Load<Texture2D>("Tilesets/DampDungeons/Tiles/Dungeon_WallsAndFloors");

            _wallTileset = new Tileset(
                new TextureRegion(wallTexture, 0, 0, 256, 192),
                16, 16
            );

            _floorTileset = new Tileset(
                new TextureRegion(floorTexture, 0, 0, 96, 512),
                16, 16
            );

            // Initialize TilesetManager
            TilesetManager.Instance.SetTilesets(_wallTileset, _floorTileset);

            // Load XML configurations
            var wallXml = ConfigurationManager.Instance.LoadConfiguration("Content/Tilesets/DampDungeons/Tiles/Autotiles-Dungeon.xml");
            var floorXml = ConfigurationManager.Instance.LoadConfiguration("Content/Tilesets/DampDungeons/Tiles/Autotiles.xml");

            // Initialize AutotileMapper
            AutotileMapper.Initialize(wallXml, floorXml);

            // Create tilemap
            CreateTestTilemap();

            // Initialize camera with higher zoom and positioned to show first pattern
            _camera = new Camera2D(Core.GraphicsDevice.Viewport)
            {
                Zoom = 5.0f,  // Increased from 2.0f to 5.0f for better tile visibility
                Position = new Vector2(
                    3.5f * _tilemap.TileWidth,  // Focus on the first pattern
                    3.5f * _tilemap.TileHeight
                )
            };

            // Initialize CameraManager with our camera
            CameraManager.Initialize(_camera);

            Log.Info(LogArea.Dungeon, "AutotileTestScene loaded successfully");
            
            // Debug output
            System.Diagnostics.Debug.WriteLine($"Tilemap size: {_tilemap.Columns}x{_tilemap.Rows}");
            System.Diagnostics.Debug.WriteLine($"Camera position: {_camera.Position}");
            System.Diagnostics.Debug.WriteLine($"Camera zoom: {_camera.Zoom}");
        }

        private void CreateTestTilemap()
        {
            // Calculate tilemap size based on number of patterns
            int patternsPerRow = 4;
            int patternSpacing = 2; // Tiles between patterns
            int patternSize = 5; // 3x3 pattern + 1 tile border on each side

            int tilemapCols = patternsPerRow * (patternSize + patternSpacing);
            int tilemapRows = ((_testPatterns.Count + patternsPerRow - 1) / patternsPerRow) * (patternSize + patternSpacing);

            _tilemap = new Tilemap(_floorTileset, tilemapCols, tilemapRows);

            // Debug: Check if we have valid floor tile
            var floorTileId = AutotileMapper.GetFloorTileIndex("floor_stone");
            System.Diagnostics.Debug.WriteLine($"Floor tile ID: {floorTileId}");
            
            // Fill with floor tiles first
            for (int y = 0; y < tilemapRows; y++)
            {
                for (int x = 0; x < tilemapCols; x++)
                {
                    _tilemap.SetTile(x, y, floorTileId, _floorTileset);
                }
            }

            // Get a wall tile ID to use for placement
            var wallTileId = AutotileMapper.GetWallTileIndex("isolated");
            System.Diagnostics.Debug.WriteLine($"Wall tile ID: {wallTileId}");

            // Place each test pattern
            for (int i = 0; i < _testPatterns.Count; i++)
            {
                var (name, pattern) = _testPatterns[i];

                int row = i / patternsPerRow;
                int col = i % patternsPerRow;

                int startX = col * (patternSize + patternSpacing) + 1; // +1 for border
                int startY = row * (patternSize + patternSpacing) + 1; // +1 for border

                // Place the pattern
                for (int py = 0; py < 3; py++)
                {
                    for (int px = 0; px < 3; px++)
                    {
                        if (pattern[py, px] == 1)
                        {
                            _tilemap.SetTile(startX + px, startY + py, wallTileId, _wallTileset);
                        }
                    }
                }
            }

            // Apply autotiling
            AutotileMapper.ApplyAutotiling(_tilemap, wallTileId, _wallTileset, _floorTileset);

            // Set the tilemap in TilesetManager
            TilesetManager.Instance.SetTilemap(_tilemap);
            
            // Debug: Count non-empty tiles
            int nonEmptyCount = 0;
            for (int y = 0; y < _tilemap.Rows; y++)
            {
                for (int x = 0; x < _tilemap.Columns; x++)
                {
                    if (_tilemap.GetTileId(x, y) >= 0)
                        nonEmptyCount++;
                }
            }
            System.Diagnostics.Debug.WriteLine($"Non-empty tiles: {nonEmptyCount}");
        }

        public override void Update(GameTime gameTime)
        {
            var keyboardState = Keyboard.GetState();

            // Camera controls - adjusted speed for higher zoom
            float cameraSpeed = 100f * (float)gameTime.ElapsedGameTime.TotalSeconds / _camera.Zoom;  // Scale speed with zoom

            if (keyboardState.IsKeyDown(Keys.W) || keyboardState.IsKeyDown(Keys.Up))
                _camera.Position -= new Vector2(0, cameraSpeed);
            if (keyboardState.IsKeyDown(Keys.S) || keyboardState.IsKeyDown(Keys.Down))
                _camera.Position += new Vector2(0, cameraSpeed);
            if (keyboardState.IsKeyDown(Keys.A) || keyboardState.IsKeyDown(Keys.Left))
                _camera.Position -= new Vector2(cameraSpeed, 0);
            if (keyboardState.IsKeyDown(Keys.D) || keyboardState.IsKeyDown(Keys.Right))
                _camera.Position += new Vector2(cameraSpeed, 0);

            // Zoom controls - adjusted for finer control
            if (keyboardState.IsKeyDown(Keys.Q) && !_previousKeyboardState.IsKeyDown(Keys.Q))
                _camera.Zoom = Math.Max(1.0f, _camera.Zoom - 0.5f);
            if (keyboardState.IsKeyDown(Keys.E) && !_previousKeyboardState.IsKeyDown(Keys.E))
                _camera.Zoom = Math.Min(10f, _camera.Zoom + 0.5f);  // Increased max zoom to 10

            // Number keys for quick zoom levels
            if (keyboardState.IsKeyDown(Keys.D1) && !_previousKeyboardState.IsKeyDown(Keys.D1))
                _camera.Zoom = 1.0f;
            if (keyboardState.IsKeyDown(Keys.D2) && !_previousKeyboardState.IsKeyDown(Keys.D2))
                _camera.Zoom = 2.0f;
            if (keyboardState.IsKeyDown(Keys.D3) && !_previousKeyboardState.IsKeyDown(Keys.D3))
                _camera.Zoom = 3.0f;
            if (keyboardState.IsKeyDown(Keys.D4) && !_previousKeyboardState.IsKeyDown(Keys.D4))
                _camera.Zoom = 4.0f;
            if (keyboardState.IsKeyDown(Keys.D5) && !_previousKeyboardState.IsKeyDown(Keys.D5))
                _camera.Zoom = 5.0f;

            // Reset view - now focuses on first pattern with good zoom
            if (keyboardState.IsKeyDown(Keys.R) && !_previousKeyboardState.IsKeyDown(Keys.R))
            {
                _camera.Position = new Vector2(3.5f * _tilemap.TileWidth, 3.5f * _tilemap.TileHeight);
                _camera.Zoom = 5.0f;
            }

            // Tab to cycle through patterns
            if (keyboardState.IsKeyDown(Keys.Tab) && !_previousKeyboardState.IsKeyDown(Keys.Tab))
            {
                // Find current pattern index based on camera position
                int patternsPerRow = 4;
                int patternSpacing = 2;
                int patternSize = 5;
                
                int currentCol = (int)(_camera.Position.X / _tilemap.TileWidth / (patternSize + patternSpacing));
                int currentRow = (int)(_camera.Position.Y / _tilemap.TileHeight / (patternSize + patternSpacing));
                int currentIndex = currentRow * patternsPerRow + currentCol;
                
                // Move to next pattern
                currentIndex = (currentIndex + 1) % _testPatterns.Count;
                
                // Calculate new position
                int newRow = currentIndex / patternsPerRow;
                int newCol = currentIndex % patternsPerRow;
                
                _camera.Position = new Vector2(
                    (newCol * (patternSize + patternSpacing) + 2.5f) * _tilemap.TileWidth,
                    (newRow * (patternSize + patternSpacing) + 2.5f) * _tilemap.TileHeight
                );
            }

            // Debug autotile info
            if (keyboardState.IsKeyDown(Keys.F1) && !_previousKeyboardState.IsKeyDown(Keys.F1))
            {
                AutotileMapper.DebugTileSeparation();
            }

            _camera.Update(gameTime);
            _previousKeyboardState = keyboardState;
        }

        public override void DrawWorld(GameTime gameTime)
        {
            // Draw tilemap in world space
            _tilemap.Draw(Core.SpriteBatch);

            // Draw pattern labels
            DrawPatternLabels();
        }

        public override void DrawUI(GameTime gameTime)
        {
            // Draw instructions in screen space
            DrawInstructions();
        }

        public override void Draw(GameTime gameTime)
        {
            // This should be empty as we're using DrawWorld and DrawUI
        }

        private void DrawPatternLabels()
        {
            int patternsPerRow = 4;
            int patternSpacing = 2;
            int patternSize = 5;

            for (int i = 0; i < _testPatterns.Count; i++)
            {
                var (name, _) = _testPatterns[i];

                int row = i / patternsPerRow;
                int col = i % patternsPerRow;

                int startX = col * (patternSize + patternSpacing) + 1;
                int startY = row * (patternSize + patternSpacing) + 1;

                // Convert tile position to world position
                Vector2 labelPos = new Vector2(
                    (startX + 1.5f) * _tilemap.TileWidth,
                    (startY - 0.5f) * _tilemap.TileHeight
                );

                // Draw label centered above pattern - scale text based on zoom
                var textSize = _font.MeasureString(name);
                float textScale = Math.Min(0.5f, 2.0f / _camera.Zoom);  // Scale text inversely with zoom
                
                Core.SpriteBatch.DrawString(
                    _font,
                    name,
                    labelPos - new Vector2(textSize.X * textScale / 2f, 0),
                    Color.White,
                    0f,
                    Vector2.Zero,
                    textScale,
                    SpriteEffects.None,
                    0f
                );
            }
        }

        private void DrawInstructions()
        {
            var instructions = new[]
            {
                "Autotile Test Scene",
                "",
                "Controls:",
                "WASD/Arrows - Move camera",
                "Q/E - Zoom out/in",
                "1-5 - Quick zoom levels",
                "Tab - Next pattern",
                "R - Reset view",
                "F1 - Debug tile info",
                "ESC - Exit",
                "",
                $"Camera: {_camera.Position:F0}",
                $"Zoom: {_camera.Zoom:F1}x"
            };

            Vector2 position = new Vector2(10, 10);
            foreach (var line in instructions)
            {
                Core.SpriteBatch.DrawString(_font, line, position, Color.White);
                position.Y += 20;
            }
        }
        
        /// <summary>
        /// Implement ICameraProvider to provide camera transform matrix
        /// </summary>
        public Matrix GetViewMatrix()
        {
            return _camera?.GetViewMatrix() ?? Matrix.Identity;
        }
        
        protected override void Dispose(bool disposing)
        {
            if (IsDisposed) return;

            if (disposing)
            {
                // Clean up any resources if needed
            }

            base.Dispose(disposing);
        }
    }
}