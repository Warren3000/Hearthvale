using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using MonoGameLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Xml.Linq;
using System.IO;
using System.Linq;

namespace Hearthvale.GameCode.Utils
{
    /// <summary>
    /// Handles autotile mapping for wall tiles to create seamless connections
    /// </summary>
    public static class AutotileMapper
    {
        private static Dictionary<string, int> _tileIndices = new();
        private static Dictionary<int, int> _patternToIndex = new(); // bit pattern -> tile index
        private static HashSet<int> _wallTileIndices = new();
        private static bool _initialized = false;

        /// <summary>
        /// Initializes the AutotileMapper with configuration from XML file
        /// </summary>
        /// <param name="content">Content manager for loading configuration files</param>
        public static void Initialize(ContentManager content)
        {
            if (_initialized) return;

            try
            {
                string xmlPath = Path.Combine(content.RootDirectory, "Tilesets", "DampDungeons", "Tiles", "Autotiles.xml");
                LoadFromXml(xmlPath);
                _initialized = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load autotile configuration: {ex.Message}");
                InitializeFallback();
            }
        }

        private static void LoadFromXml(string xmlPath)
        {
            var doc = XDocument.Load(xmlPath);
            var wallTiles = doc.Root?.Element("WallTiles");
            
            if (wallTiles != null)
            {
                foreach (var tile in wallTiles.Elements("Tile"))
                {
                    var type = tile.Attribute("type")?.Value;
                    var indexStr = tile.Attribute("index")?.Value;
                    var colStr = tile.Attribute("col")?.Value;
                    var rowStr = tile.Attribute("row")?.Value;
                    var patternStr = tile.Attribute("pattern")?.Value;
                    
                    if (!string.IsNullOrEmpty(type))
                    {
                        int index;
                        
                        // Support both index and col/row formats
                        if (!string.IsNullOrEmpty(indexStr))
                        {
                            index = int.Parse(indexStr);
                        }
                        else if (!string.IsNullOrEmpty(colStr) && !string.IsNullOrEmpty(rowStr))
                        {
                            int col = int.Parse(colStr);
                            int row = int.Parse(rowStr);
                            index = row * 40 + col; // Convert 2D coordinates to 1D index
                        }
                        else
                        {
                            continue; // Skip invalid entries
                        }
                        
                        // Validate bounds
                        if (index < 0 || index >= 1200)
                        {
                            System.Diagnostics.Debug.WriteLine($"ERROR: Tile '{type}' has invalid index {index}");
                            continue;
                        }
                        
                        // Calculate and log position
                        int tileCol = index % 40;
                        int tileRow = index / 40;
                        System.Diagnostics.Debug.WriteLine($"Loading tile '{type}': index {index} -> position ({tileCol}, {tileRow})");
                        
                        _tileIndices[type] = index;
                        
                        // Handle wall tile patterns
                        if (!string.IsNullOrEmpty(patternStr) && !type.StartsWith("floor_"))
                        {
                            int pattern = int.Parse(patternStr);
                            _patternToIndex[pattern] = index;
                            _wallTileIndices.Add(index);
                        }
                    }
                }
            }
            
            // Always include tile 0 as a wall for backward compatibility
            //_wallTileIndices.Add(0);
        }

        private static void InitializeFallback()
        {
            // Populate tile indices for fallback
            _tileIndices = new Dictionary<string, int>
            {
                { "isolated", 0 },
                { "horizontal", 1 },
                { "vertical", 40 },
                { "cross", 41 },
                { "corner_tl", 2 },
                { "corner_tr", 3 },
                { "corner_bl", 42 },
                { "corner_br", 43 },
                { "t_up", 6 },
                { "t_down", 46 },
                { "t_left", 47 },
                { "t_right", 7 },
                { "end_up", 8 },
                { "end_down", 48 },
                { "end_left", 49 },
                { "end_right", 9 },
                { "floor_basic", 80 },
                { "floor_variant1", 81 },
                { "floor_variant2", 82 }
            };

            // Populate pattern mappings for fallback
            _patternToIndex = new Dictionary<int, int>
            {
                { 0, 0 },     // isolated
                { 68, 1 },    // horizontal (E+W)
                { 17, 40 },   // vertical (N+S)
                { 85, 41 },   // cross (N+E+S+W)
                { 20, 2 },    // corner_tl (S+E)
                { 80, 3 },    // corner_tr (S+W)
                { 5, 42 },    // corner_bl (N+E)
                { 65, 43 },   // corner_br (N+W)
                { 84, 6 },    // t_up (S+E+W)
                { 69, 46 },   // t_down (N+E+W)
                { 21, 47 },   // t_left (N+S+E)
                { 81, 7 },    // t_right (N+S+W)
                { 1, 8 },     // end_up (N)
                { 16, 48 },   // end_down (S)
                { 64, 49 },   // end_left (W)
                { 4, 9 }      // end_right (E)
            };

            _wallTileIndices = new HashSet<int> { 0, 1, 40, 41, 2, 3, 42, 43, 6, 46, 47, 7, 8, 48, 49, 9 };
            _initialized = true;
        }

        /// <summary>
        /// Maps a bitmask to the appropriate tile index using XML configuration
        /// </summary>
        private static int GetTileFromMask(int mask)
        {
            if (!_initialized)
            {
                throw new InvalidOperationException("AutotileMapper not initialized. Call Initialize() first.");
            }

            // Extract cardinal directions (N=1, E=4, S=16, W=64)
            int cardinalMask = mask & (1 | 4 | 16 | 64);

            // Look up the tile index directly from our pattern mapping
            if (_patternToIndex.TryGetValue(cardinalMask, out int tileIndex))
            {
                return tileIndex;
            }

            // Fallback to isolated tile
            return _tileIndices.GetValueOrDefault("isolated", 0);
        }

        /// <summary>
        /// Gets the correct autotile index based on surrounding wall tiles
        /// </summary>
        public static int GetAutotileIndex(Tilemap tilemap, int col, int row, int wallTileId)
        {
            if (tilemap.GetTileId(col, row) != wallTileId)
                return tilemap.GetTileId(col, row);

            // Create bitmask based on surrounding tiles
            int mask = 0;
            if (IsWall(tilemap, col, row - 1, wallTileId)) mask |= 1;    // North
            if (IsWall(tilemap, col + 1, row - 1, wallTileId)) mask |= 2; // Northeast
            if (IsWall(tilemap, col + 1, row, wallTileId)) mask |= 4;     // East
            if (IsWall(tilemap, col + 1, row + 1, wallTileId)) mask |= 8; // Southeast
            if (IsWall(tilemap, col, row + 1, wallTileId)) mask |= 16;    // South
            if (IsWall(tilemap, col - 1, row + 1, wallTileId)) mask |= 32; // Southwest
            if (IsWall(tilemap, col - 1, row, wallTileId)) mask |= 64;    // West
            if (IsWall(tilemap, col - 1, row - 1, wallTileId)) mask |= 128; // Northwest

            return GetTileFromMask(mask);
        }

        private static bool IsWall(Tilemap tilemap, int col, int row, int originalWallTileId)
        {
            if (col < 0 || col >= tilemap.Columns || row < 0 || row >= tilemap.Rows)
                return true;

            int tileId = tilemap.GetTileId(col, row);
            return tileId == originalWallTileId || IsWallTile(tileId);
        }

        /// <summary>
        /// Checks if a tile ID represents any type of wall using XML configuration
        /// </summary>
        public static bool IsWallTile(int tileId)
        {
            if (!_initialized)
                return tileId == 0;

            return _wallTileIndices.Contains(tileId);
        }

        public static int GetWallTileIndex(string wallType)
        {
            if (!_initialized)
                throw new InvalidOperationException("AutotileMapper not initialized. Call Initialize() first.");

            return _tileIndices.GetValueOrDefault(wallType, 0);
        }

        /// <summary>
        /// Applies autotiling to an entire tilemap with support for existing autotiled walls
        /// </summary>
        public static void ApplyAutotiling(Tilemap tilemap, int originalWallTileId)
        {
            if (!_initialized)
            {
                throw new InvalidOperationException("AutotileMapper not initialized. Call Initialize() first.");
            }

            // Create a copy of the current tile data
            int[,] originalTiles = new int[tilemap.Columns, tilemap.Rows];
            for (int row = 0; row < tilemap.Rows; row++)
            {
                for (int col = 0; col < tilemap.Columns; col++)
                {
                    originalTiles[col, row] = tilemap.GetTileId(col, row);
                }
            }

            // Apply autotiling based on original data
            for (int row = 0; row < tilemap.Rows; row++)
            {
                for (int col = 0; col < tilemap.Columns; col++)
                {
                    int currentTileId = originalTiles[col, row];
                    // Check if this tile is any type of wall (original or already autotiled)
                    if (currentTileId == originalWallTileId || IsWallTile(currentTileId))
                    {
                        int autotileIndex = GetAutotileIndexFromOriginal(originalTiles, col, row, originalWallTileId, tilemap.Columns, tilemap.Rows);
                        tilemap.SetTile(col, row, autotileIndex);
                    }
                }
            }
        }

        /// <summary>
        /// Gets autotile index from original tile data (before autotiling changes)
        /// </summary>
        private static int GetAutotileIndexFromOriginal(int[,] originalTiles, int col, int row, int wallTileId, int columns, int rows)
        {
            int mask = 0;

            // Check 8 directions using original data
            if (IsWallInOriginal(originalTiles, col, row - 1, wallTileId, columns, rows)) mask |= 1;     // North
            if (IsWallInOriginal(originalTiles, col + 1, row - 1, wallTileId, columns, rows)) mask |= 2; // Northeast
            if (IsWallInOriginal(originalTiles, col + 1, row, wallTileId, columns, rows)) mask |= 4;     // East
            if (IsWallInOriginal(originalTiles, col + 1, row + 1, wallTileId, columns, rows)) mask |= 8; // Southeast
            if (IsWallInOriginal(originalTiles, col, row + 1, wallTileId, columns, rows)) mask |= 16;    // South
            if (IsWallInOriginal(originalTiles, col - 1, row + 1, wallTileId, columns, rows)) mask |= 32; // Southwest
            if (IsWallInOriginal(originalTiles, col - 1, row, wallTileId, columns, rows)) mask |= 64;    // West
            if (IsWallInOriginal(originalTiles, col - 1, row - 1, wallTileId, columns, rows)) mask |= 128; // Northwest

            return GetTileFromMask(mask);
        }
        /// <summary>
        /// Checks if a tile ID represents any type of floor using XML configuration
        /// </summary>
        public static bool IsFloorTile(int tileId)
        {
            if (!_initialized)
            {
                throw new InvalidOperationException("AutotileMapper not initialized. Call Initialize() first.");
            }

            // Check all floor tile types from XML
            var floorTypes = _tileIndices.Where(kvp => kvp.Key.StartsWith("floor_"));
            return floorTypes.Any(kvp => kvp.Value == tileId);
        }

        /// <summary>
        /// Gets all floor tile IDs from XML configuration
        /// </summary>
        public static int[] GetFloorTileIds()
        {
            if (!_initialized)
            {
                throw new InvalidOperationException("AutotileMapper not initialized. Call Initialize() first.");
            }

            return _tileIndices.Where(kvp => kvp.Key.StartsWith("floor_")).Select(kvp => kvp.Value).ToArray();
        }
        /// <summary>
        /// Checks if a tile is a wall using original tile data (supports multiple wall types)
        /// </summary>
        private static bool IsWallInOriginal(int[,] originalTiles, int col, int row, int originalWallTileId, int columns, int rows)
        {
            if (col < 0 || col >= columns || row < 0 || row >= rows)
                return true; // Treat out-of-bounds as walls

            int tileId = originalTiles[col, row];
            return tileId == originalWallTileId || IsWallTile(tileId);
        }

        /// <summary>
        /// Debug method to verify XML loading and floor tile configuration
        /// </summary>
        public static void DebugPrintFloorConfiguration()
        {
            if (!_initialized)
            {
                System.Diagnostics.Debug.WriteLine("AutotileMapper not initialized!");
                return;
            }

            System.Diagnostics.Debug.WriteLine("=== Floor Tile Configuration DEBUG ===");
            System.Diagnostics.Debug.WriteLine($"Total tile indices loaded: {_tileIndices.Count}");
            
            var floorTypes = _tileIndices.Where(kvp => kvp.Key.StartsWith("floor_"));
            System.Diagnostics.Debug.WriteLine($"Found {floorTypes.Count()} floor tile types:");
            
            foreach (var kvp in floorTypes)
            {
                System.Diagnostics.Debug.WriteLine($"  {kvp.Key}: index {kvp.Value}");
            }
            
            var floorIds = GetFloorTileIds();
            System.Diagnostics.Debug.WriteLine($"GetFloorTileIds() returns: [{string.Join(", ", floorIds)}]");
            
            // Check if we're using fallback or XML data
            bool usingFallback = _tileIndices.ContainsKey("floor_basic") && _tileIndices["floor_basic"] == 80;
            System.Diagnostics.Debug.WriteLine($"Using fallback data: {usingFallback}");
            
            if (!usingFallback)
            {
                System.Diagnostics.Debug.WriteLine("✓ XML data loaded successfully");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("✗ Using fallback hardcoded data - XML may not be loading");
            }
        }
        /// <summary>
        /// Enhanced debug method to verify tileset positioning and graphics
        /// </summary>
        public static void DebugTilesetPositions()
        {
            if (!_initialized)
            {
                System.Diagnostics.Debug.WriteLine("AutotileMapper not initialized!");
                return;
            }

            System.Diagnostics.Debug.WriteLine("=== TILESET POSITION ANALYSIS ===");
            
            var floorTypes = _tileIndices.Where(kvp => kvp.Key.StartsWith("floor_"));
            
            foreach (var kvp in floorTypes)
            {
                int index = kvp.Value;
                int col = index % 40; // 40 columns in your tileset
                int row = index / 40;
                
                // Calculate pixel positions in the 640x480 tileset
                int pixelX = col * 16;
                int pixelY = row * 16;
                
                System.Diagnostics.Debug.WriteLine($"Floor tile '{kvp.Key}':");
                System.Diagnostics.Debug.WriteLine($"  Index: {index}");
                System.Diagnostics.Debug.WriteLine($"  Grid position: Column {col}, Row {row}");
                System.Diagnostics.Debug.WriteLine($"  Pixel position in tileset: ({pixelX}, {pixelY})");
                System.Diagnostics.Debug.WriteLine($"  Expected tile region: {pixelX},{pixelY} to {pixelX + 16},{pixelY + 16}");
                System.Diagnostics.Debug.WriteLine("");
            }
            
            System.Diagnostics.Debug.WriteLine("=== VERIFICATION QUESTIONS ===");
            System.Diagnostics.Debug.WriteLine("1. Do these pixel positions contain different floor graphics in your Dungeon_WallsAndFloors.png?");
            System.Diagnostics.Debug.WriteLine("2. Are the graphics at these positions actually different from each other?");
            System.Diagnostics.Debug.WriteLine("3. Are any of these positions empty/transparent in your tileset?");
        }
        /// <summary>
        /// Debug method to verify all tile mappings and detect conflicts
        /// </summary>
        public static void DebugTileMapping()
        {
            if (!_initialized)
            {
                System.Diagnostics.Debug.WriteLine("AutotileMapper not initialized!");
                return;
            }

            System.Diagnostics.Debug.WriteLine("=== COMPLETE TILE MAPPING DEBUG ===");
            
            // Show all tile indices
            System.Diagnostics.Debug.WriteLine($"All tile indices ({_tileIndices.Count} total):");
            foreach (var kvp in _tileIndices)
            {
                bool isWall = _wallTileIndices.Contains(kvp.Value);
                System.Diagnostics.Debug.WriteLine($"  '{kvp.Key}' -> index {kvp.Value} ({(isWall ? "WALL" : "FLOOR")})");
            }
            
            // Show all pattern mappings
            System.Diagnostics.Debug.WriteLine($"\nPattern mappings ({_patternToIndex.Count} total):");
            foreach (var kvp in _patternToIndex)
            {
                System.Diagnostics.Debug.WriteLine($"  Pattern {kvp.Key} -> index {kvp.Value}");
            }
            
            // Show wall tile indices
            System.Diagnostics.Debug.WriteLine($"\nWall tile indices: [{string.Join(", ", _wallTileIndices)}]");
            
            // Check for conflicts
            var tilesByIndex = new Dictionary<int, List<string>>();
            foreach (var kvp in _tileIndices)
            {
                if (!tilesByIndex.ContainsKey(kvp.Value))
                    tilesByIndex[kvp.Value] = new List<string>();
                tilesByIndex[kvp.Value].Add(kvp.Key);
            }
            
            bool conflicts = false;
            foreach (var kvp in tilesByIndex)
            {
                if (kvp.Value.Count > 1)
                {
                    conflicts = true;
                    System.Diagnostics.Debug.WriteLine($"⚠️  CONFLICT: Index {kvp.Key} used by: {string.Join(", ", kvp.Value)}");
                }
            }
            
            if (!conflicts)
            {
                System.Diagnostics.Debug.WriteLine("✅ No tile index conflicts detected!");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("❌ Tile conflicts found!");
            }
        }
    }
}