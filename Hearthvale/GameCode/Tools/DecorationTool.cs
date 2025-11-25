using Hearthvale.GameCode.Data;
using Hearthvale.GameCode.Managers;
using Hearthvale.GameCode.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using WinForms = System.Windows.Forms;

namespace Hearthvale.GameCode.Tools
{
    public class DecorationTool
    {
        private enum ToolMode
        {
            Decoration,
            Tile
        }

        private class GroupData
        {
            public string Name;
            public List<Rectangle> Regions;
        }

        private class TilesetData
        {
            public Texture2D Texture;
            public List<GroupData> Groups;
        }

        private Dictionary<string, TilesetData> _tilesets = new Dictionary<string, TilesetData>();
        private string _currentTilesetName;
        private int _currentGroupIndex = 0;
        
        private List<PlacedDecoration> _decorations = new List<PlacedDecoration>();
        private Rectangle _sourceRect;
        private int _tileSize = 32;
        private int[] _gridSizes = new int[] { 32, 16, 8, 4, 1 };
        private int _currentGridSizeIndex = 0;
        private int _currentTileIndex = 0;
        private bool _isActive = false;
        private float _inputDelay = 0f;
        private bool _hasChanges = false;

        private ToolMode _currentMode = ToolMode.Decoration;
        private Tilemap _tilemap;
        private int _currentTileId = 0;

        public bool IsActive { get { return _isActive; } set { _isActive = value; } }

        public DecorationTool(Tilemap tilemap = null)
        {
            _tilemap = tilemap;
            LoadLastSession();
        }

        private void LoadLastSession()
        {
            string path = GetLastSessionPath();
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                LoadLevelFromFile(path, true);
            }
        }

        private string GetLastSessionPath()
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "last_session.txt");
                if (File.Exists(path))
                {
                    return File.ReadAllText(path).Trim();
                }
            }
            catch { }
            return null;
        }

        private void SaveLastSessionPath(string filePath)
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "last_session.txt");
                File.WriteAllText(path, filePath);
            }
            catch { }
        }

        public void SetTilemap(Tilemap tilemap)
        {
            _tilemap = tilemap;
        }

        public void AddTileset(string name, Texture2D texture, List<Rectangle> regions = null)
        {
            if (!_tilesets.ContainsKey(name))
            {
                var data = new TilesetData
                {
                    Texture = texture,
                    Groups = new List<GroupData>()
                };

                if (regions == null || regions.Count == 0)
                {
                    // Fallback to grid if no regions provided
                    regions = new List<Rectangle>();
                    int cols = Math.Max(1, texture.Width / _tileSize);
                    int rows = Math.Max(1, texture.Height / _tileSize);
                    for (int y = 0; y < rows; y++)
                    {
                        for (int x = 0; x < cols; x++)
                        {
                            regions.Add(new Rectangle(x * _tileSize, y * _tileSize, _tileSize, _tileSize));
                        }
                    }
                }
                
                data.Groups.Add(new GroupData { Name = "Default", Regions = regions });
                _tilesets[name] = data;

                if (_currentTilesetName == null)
                {
                    _currentTilesetName = name;
                    _currentGroupIndex = 0;
                    UpdateSourceRect();
                }
            }
        }

        public void LoadTilesetDefinition(string name, DecorationSetDefinition definition, Texture2D texture)
        {
            var data = new TilesetData
            {
                Texture = texture,
                Groups = new List<GroupData>()
            };

            // Add ungrouped regions as "Default"
            if (definition.Regions != null && definition.Regions.Count > 0)
            {
                data.Groups.Add(new GroupData
                {
                    Name = "Default",
                    Regions = definition.Regions.Select(r => r.ToRectangle()).ToList()
                });
            }

            // Add groups
            if (definition.Groups != null)
            {
                foreach (var group in definition.Groups)
                {
                    data.Groups.Add(new GroupData
                    {
                        Name = group.Name,
                        Regions = group.Regions.Select(r => r.ToRectangle()).ToList()
                    });
                }
            }

            // If nothing loaded, fallback
            if (data.Groups.Count == 0)
            {
                AddTileset(name, texture, null);
                return;
            }

            _tilesets[name] = data;
            
            if (_currentTilesetName == null)
            {
                _currentTilesetName = name;
                _currentGroupIndex = 0;
                UpdateSourceRect();
            }
        }        public void Update(GameTime gameTime)
        {
            if (!_isActive) return;

            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_inputDelay > 0) _inputDelay -= dt;

            var keyboard = Keyboard.GetState();
            var mouse = Mouse.GetState();

            if (_inputDelay <= 0)
            {
                // Change tile with brackets
                if (keyboard.IsKeyDown(Keys.OemOpenBrackets))
                {
                    CycleTile(-1);
                    _inputDelay = 0.15f;
                }
                if (keyboard.IsKeyDown(Keys.OemCloseBrackets))
                {
                    CycleTile(1);
                    _inputDelay = 0.15f;
                }

                // Change tileset with Tab
                if (keyboard.IsKeyDown(Keys.Tab))
                {
                    if (keyboard.IsKeyDown(Keys.LeftShift) || keyboard.IsKeyDown(Keys.RightShift))
                    {
                        CycleGroup();
                    }
                    else
                    {
                        CycleTileset();
                    }
                    _inputDelay = 0.2f;
                }

                // Change grid size with G
                if (keyboard.IsKeyDown(Keys.G))
                {
                    CycleGridSize();
                    _inputDelay = 0.2f;
                }

                // Toggle Mode with M
                if (keyboard.IsKeyDown(Keys.M))
                {
                    _currentMode = _currentMode == ToolMode.Decoration ? ToolMode.Tile : ToolMode.Decoration;
                    _inputDelay = 0.2f;
                }

                // Exit with Escape
                if (keyboard.IsKeyDown(Keys.Escape))
                {
                    if (_hasChanges)
                    {
                        var result = WinForms.MessageBox.Show("You have unsaved changes. Are you sure you want to exit?", "Unsaved Changes", WinForms.MessageBoxButtons.YesNo, WinForms.MessageBoxIcon.Warning);
                        if (result == WinForms.DialogResult.Yes)
                        {
                            Environment.Exit(0);
                        }
                    }
                    else
                    {
                        Environment.Exit(0);
                    }
                    _inputDelay = 0.5f;
                }

                // New Level with F1
                if (keyboard.IsKeyDown(Keys.F1))
                {
                    NewLevel();
                    _inputDelay = 0.5f;
                }

                // Save with F5
                if (keyboard.IsKeyDown(Keys.F5))
                {
                    SaveLevel();
                    _inputDelay = 0.5f;
                }

                // Load with F9
                if (keyboard.IsKeyDown(Keys.F9))
                {
                    LoadLevel();
                    _inputDelay = 0.5f;
                }
            }

            // Place with Left Click
            if (mouse.LeftButton == ButtonState.Pressed)
            {
                HandleClick(mouse, true);
            }

            // Remove with Right Click
            if (mouse.RightButton == ButtonState.Pressed)
            {
                HandleClick(mouse, false);
            }
        }

        private void CycleTile(int direction)
        {
            if (_currentTilesetName == null) return;
            int maxTiles = GetMaxTiles(_currentTilesetName);
            if (maxTiles == 0) return;

            _currentTileIndex += direction;
            if (_currentTileIndex < 0) _currentTileIndex = maxTiles - 1;
            if (_currentTileIndex >= maxTiles) _currentTileIndex = 0;

            UpdateSourceRect();
        }

        private void CycleTileset()
        {
            if (_tilesets.Count <= 1) return;
            
            var keys = _tilesets.Keys.ToList();
            int index = keys.IndexOf(_currentTilesetName);
            index = (index + 1) % keys.Count;
            _currentTilesetName = keys[index];
            _currentGroupIndex = 0;
            _currentTileIndex = 0;
            UpdateSourceRect();
        }

        private void CycleGroup()
        {
            if (_currentTilesetName == null) return;
            var data = _tilesets[_currentTilesetName];
            if (data.Groups.Count <= 1) return;

            _currentGroupIndex = (_currentGroupIndex + 1) % data.Groups.Count;
            _currentTileIndex = 0;
            UpdateSourceRect();
        }

        private void CycleGridSize()
        {
            _currentGridSizeIndex = (_currentGridSizeIndex + 1) % _gridSizes.Length;
            _tileSize = _gridSizes[_currentGridSizeIndex];
        }

        private void HandleClick(MouseState mouse, bool isPlacing)
        {
            Vector2 worldPos = CameraManager.Instance.Camera2D.ScreenToWorld(new Vector2(mouse.X, mouse.Y));
            
            if (_currentMode == ToolMode.Decoration)
            {
                Vector2 snappedPos = SnapToGrid(worldPos);
                
                if (isPlacing && _currentTilesetName != null)
                {
                    var newDecoration = new PlacedDecoration 
                    { 
                        Position = snappedPos, 
                        SourceRect = _sourceRect,
                        TilesetName = _currentTilesetName
                    };

                    // Check for modifier to place behind
                    if (Keyboard.GetState().IsKeyDown(Keys.LeftControl))
                    {
                        // Insert at the beginning (draws first, so behind everything)
                        _decorations.Insert(0, newDecoration);
                    }
                    else
                    {
                        // Add to end (draws last, so on top)
                        _decorations.Add(newDecoration);
                    }
                    _hasChanges = true;
                }
                else if (!isPlacing)
                {
                    // Remove the top-most decoration at this position
                    int index = _decorations.FindLastIndex(d => d.Position == snappedPos);
                    if (index != -1)
                    {
                        _decorations.RemoveAt(index);
                        _hasChanges = true;
                    }
                }
            }
            else if (_currentMode == ToolMode.Tile && _tilemap != null)
            {
                int col = (int)(worldPos.X / _tilemap.TileWidth);
                int row = (int)(worldPos.Y / _tilemap.TileHeight);

                if (col >= 0 && col < _tilemap.Columns && row >= 0 && row < _tilemap.Rows)
                {
                    if (isPlacing)
                    {
                        // For now, just use the current tile index as the ID
                        // This assumes the user knows the ID or we add a visual picker later
                        _tilemap.SetTile(col, row, _currentTileIndex);
                        _hasChanges = true;
                    }
                    else
                    {
                        // Right click to erase (set to 0 or some default)
                        _tilemap.SetTile(col, row, 0);
                        _hasChanges = true;
                    }
                }
            }
        }

        private void NewLevel()
        {
            if (_hasChanges)
            {
                var result = WinForms.MessageBox.Show("You have unsaved changes. Are you sure you want to create a new level?", "Unsaved Changes", WinForms.MessageBoxButtons.YesNo, WinForms.MessageBoxIcon.Warning);
                if (result != WinForms.DialogResult.Yes)
                {
                    return;
                }
            }

            _decorations.Clear();

            if (_tilemap != null)
            {
                for (int y = 0; y < _tilemap.Rows; y++)
                {
                    for (int x = 0; x < _tilemap.Columns; x++)
                    {
                        _tilemap.SetTile(x, y, 0);
                    }
                }
            }

            _hasChanges = false;
            System.Diagnostics.Debug.WriteLine("Created new level");
        }

        private void SaveLevel()
        {
            if (_tilemap == null) return;

            string defaultPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Content", "Levels");
            Directory.CreateDirectory(defaultPath);

            using (var dialog = new WinForms.SaveFileDialog())
            {
                dialog.InitialDirectory = defaultPath;
                dialog.Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*";
                dialog.DefaultExt = "json";
                dialog.FileName = "level_chunk.json";

                if (dialog.ShowDialog() == WinForms.DialogResult.OK)
                {
                    var chunk = new LevelChunk(_tilemap.Columns, _tilemap.Rows);
                    
                    // Save Tiles
                    for (int y = 0; y < _tilemap.Rows; y++)
                    {
                        for (int x = 0; x < _tilemap.Columns; x++)
                        {
                            int index = y * _tilemap.Columns + x;
                            chunk.TileIds[index] = _tilemap.GetTileId(x, y);
                        }
                    }

                    // Save Decorations
                    chunk.Decorations = _decorations;

                    try
                    {
                        var options = new JsonSerializerOptions 
                        { 
                            WriteIndented = true,
                            IncludeFields = true 
                        };
                        string json = JsonSerializer.Serialize(chunk, options);
                        File.WriteAllText(dialog.FileName, json);
                        SaveLastSessionPath(dialog.FileName);
                        _hasChanges = false;
                        System.Diagnostics.Debug.WriteLine($"Saved level to {dialog.FileName}");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to save level: {ex.Message}");
                        WinForms.MessageBox.Show($"Failed to save level: {ex.Message}", "Error", WinForms.MessageBoxButtons.OK, WinForms.MessageBoxIcon.Error);
                    }
                }
            }
        }

        public void LoadLevelFromFile(string filePath, bool silent = false)
        {
            if (_tilemap == null) return;

            try
            {
                string json = File.ReadAllText(filePath);
                var options = new JsonSerializerOptions 
                { 
                    IncludeFields = true 
                };
                var chunk = JsonSerializer.Deserialize<LevelChunk>(json, options);

                // Load Tiles
                for (int y = 0; y < Math.Min(_tilemap.Rows, chunk.Height); y++)
                {
                    for (int x = 0; x < Math.Min(_tilemap.Columns, chunk.Width); x++)
                    {
                        int index = y * chunk.Width + x;
                        if (index < chunk.TileIds.Length)
                        {
                            _tilemap.SetTile(x, y, chunk.TileIds[index]);
                        }
                    }
                }

                // Load Decorations
                _decorations = chunk.Decorations ?? new List<PlacedDecoration>();
                _hasChanges = false;
                System.Diagnostics.Debug.WriteLine($"Loaded level from {filePath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load level: {ex.Message}");
                if (!silent)
                {
                    WinForms.MessageBox.Show($"Failed to load level: {ex.Message}", "Error", WinForms.MessageBoxButtons.OK, WinForms.MessageBoxIcon.Error);
                }
            }
        }

        private void LoadLevel()
        {
            if (_tilemap == null) return;

            string defaultPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Content", "Levels");
            Directory.CreateDirectory(defaultPath);

            using (var dialog = new WinForms.OpenFileDialog())
            {
                dialog.InitialDirectory = defaultPath;
                dialog.Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*";

                if (dialog.ShowDialog() == WinForms.DialogResult.OK)
                {
                    LoadLevelFromFile(dialog.FileName);
                    SaveLastSessionPath(dialog.FileName);
                }
            }
        }

        private Vector2 SnapToGrid(Vector2 pos)
        {
            return new Vector2(
                (float)Math.Floor(pos.X / _tileSize) * _tileSize,
                (float)Math.Floor(pos.Y / _tileSize) * _tileSize
            );
        }

        private void UpdateSourceRect()
        {
            if (_currentTilesetName == null) return;
            _sourceRect = GetSourceRect(_currentTilesetName, _currentTileIndex);
        }

        private int GetMaxTiles(string tilesetName)
        {
            if (tilesetName == null || !_tilesets.ContainsKey(tilesetName)) return 0;
            var data = _tilesets[tilesetName];
            if (_currentGroupIndex < 0 || _currentGroupIndex >= data.Groups.Count) return 0;
            return data.Groups[_currentGroupIndex].Regions.Count;
        }

        private Rectangle GetSourceRect(string tilesetName, int index)
        {
            if (tilesetName == null || !_tilesets.ContainsKey(tilesetName)) return Rectangle.Empty;
            var data = _tilesets[tilesetName];
            if (_currentGroupIndex < 0 || _currentGroupIndex >= data.Groups.Count) return Rectangle.Empty;
            
            var regions = data.Groups[_currentGroupIndex].Regions;
            if (index < 0 || index >= regions.Count) return Rectangle.Empty;
            return regions[index];
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (_decorations == null) return;

            foreach (var dec in _decorations)
            {
                if (!string.IsNullOrEmpty(dec.TilesetName) && _tilesets.TryGetValue(dec.TilesetName, out var data))
                {
                    spriteBatch.Draw(data.Texture, dec.Position, dec.SourceRect, Color.White);
                }
            }

            if (_isActive && _currentTilesetName != null)
            {
                var mouse = Mouse.GetState();
                Vector2 worldPos = CameraManager.Instance.Camera2D.ScreenToWorld(new Vector2(mouse.X, mouse.Y));
                Vector2 snappedPos = SnapToGrid(worldPos);

                if (_tilesets.TryGetValue(_currentTilesetName, out var data))
                {
                    // Draw ghost
                    spriteBatch.Draw(data.Texture, snappedPos, _sourceRect, Color.White * 0.7f);

                    // Draw selection box
                    DrawHollowRect(spriteBatch, new Rectangle((int)snappedPos.X, (int)snappedPos.Y, _sourceRect.Width, _sourceRect.Height), Color.Yellow, 2);
                }
            }
        }

        public void DrawUI(SpriteBatch spriteBatch, SpriteFont font)
        {
            if (!_isActive || _currentTilesetName == null) return;

            if (!_tilesets.TryGetValue(_currentTilesetName, out var data)) return;
            Texture2D tex = data.Texture;
            int maxTiles = GetMaxTiles(_currentTilesetName);
            
            string groupName = "Default";
            if (_currentGroupIndex >= 0 && _currentGroupIndex < data.Groups.Count)
            {
                groupName = data.Groups[_currentGroupIndex].Name;
            }

            // UI Container
            Vector2 uiPos = new Vector2(10, 100);
            int panelWidth = 200;
            int panelHeight = 200; // Increased height

            // Draw semi-transparent background
            if (GameUIManager.Instance.WhitePixel != null)
            {
                spriteBatch.Draw(GameUIManager.Instance.WhitePixel, new Rectangle((int)uiPos.X - 5, (int)uiPos.Y - 5, panelWidth, panelHeight), Color.Black * 0.5f);
            }

            // Header
            string header = "Decoration Tool";
            spriteBatch.DrawString(font, header, uiPos, Color.Yellow);
            uiPos.Y += 25;

            // Tileset Selector
            string tilesetText = $"Set: < {_currentTilesetName} >";
            spriteBatch.DrawString(font, tilesetText, uiPos, Color.White);
            uiPos.Y += 25;

            // Group Selector
            string groupText = $"Grp: < {groupName} >";
            spriteBatch.DrawString(font, groupText, uiPos, Color.LightGray);
            uiPos.Y += 25;

            // Tile Carousel
            // Prev
            int prevIndex = (_currentTileIndex - 1 + maxTiles) % maxTiles;
            Rectangle prevRect = GetSourceRect(_currentTilesetName, prevIndex);

            // Next
            int nextIndex = (_currentTileIndex + 1) % maxTiles;
            Rectangle nextRect = GetSourceRect(_currentTilesetName, nextIndex);

            int previewSize = 48;
            int smallSize = 32;
            int spacing = 10;

            Vector2 carouselPos = uiPos + new Vector2(10, 10);

            // Draw Prev
            Rectangle prevDest = new Rectangle((int)carouselPos.X, (int)carouselPos.Y + (previewSize - smallSize)/2, smallSize, smallSize);
            // Scale to fit
            spriteBatch.Draw(tex, prevDest, prevRect, Color.Gray);
            DrawHollowRect(spriteBatch, prevDest, Color.Gray);

            // Draw Current (Center)
            Rectangle currDest = new Rectangle(prevDest.Right + spacing, (int)carouselPos.Y, previewSize, previewSize);
            spriteBatch.Draw(tex, currDest, _sourceRect, Color.White);
            DrawHollowRect(spriteBatch, currDest, Color.Yellow, 2);

            // Draw Next
            Rectangle nextDest = new Rectangle(currDest.Right + spacing, (int)carouselPos.Y + (previewSize - smallSize)/2, smallSize, smallSize);
            spriteBatch.Draw(tex, nextDest, nextRect, Color.Gray);
            DrawHollowRect(spriteBatch, nextDest, Color.Gray);

            uiPos.Y += previewSize + 20;

            // Info
            string info = $"Mode: {_currentMode} (M)\nTile: {_currentTileIndex}\nGrid: {_tileSize}px (G)\n[ ] Cycle Tile\nTab Cycle Set\nShift+Tab Cycle Grp\nLMB Place (Ctrl=Behind)\nRMB Remove Top\nF1 New / F5 Save / F9 Load";
            spriteBatch.DrawString(font, info, uiPos, Color.White);
        }        private void DrawHollowRect(SpriteBatch spriteBatch, Rectangle rect, Color color, int thickness = 1)
        {
            var pixel = GameUIManager.Instance.WhitePixel;
            if (pixel == null) return;
            
            spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
            spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
            spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
            spriteBatch.Draw(pixel, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
        }
    }

    public struct PlacedDecoration
    {
        public Vector2 Position;
        public Rectangle SourceRect;
        public string TilesetName;
    }
}