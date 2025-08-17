using Hearthvale.GameCode.Managers;
using Hearthvale.GameCode.Utils;
using Microsoft.Xna.Framework;
using MonoGameLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Hearthvale.GameCode.Utils
{
    /// <summary>
    /// Handles autotile mapping for wall tiles to create seamless connections
    /// </summary>
    public static class AutotileMapper
    {
        private static Dictionary<int, int> _patternToIndex = new(); // bit pattern -> tile index
        private static Dictionary<string, int> _wallTileIndices = new();
        private static Dictionary<string, int> _floorTileIndices = new(); // Track floor tiles separately

        private static bool _initialized = false;

        public static void Initialize(XDocument wallXml, XDocument floorXml)
        {
            if (_initialized)
            {
                // If already initialized, clear previous data to allow re-initialization with new files
                _wallTileIndices.Clear();
                _floorTileIndices.Clear();
                _patternToIndex.Clear();
                _initialized = false;
            }

            try
            {
                LoadFromXml(wallXml, loadWalls: true, loadFloors: false);
                LoadFromXml(floorXml, loadWalls: false, loadFloors: true);

                // Validate that we loaded essential tiles
                if (!_wallTileIndices.ContainsKey("isolated"))
                {
                    throw new InvalidDataException("Missing required 'isolated' wall tile in XML configuration");
                }

                if (!_floorTileIndices.Any())
                {
                    throw new InvalidDataException("No floor tiles found in XML configuration");
                }

                _initialized = true;
                Log.Info(LogArea.Dungeon, "✅ AutotileMapper initialized successfully from XML");
            }
            catch (Exception ex)
            {
                Log.Error(LogArea.Dungeon, $"❌ AutotileMapper initialization failed: {ex.Message}");

#if DEBUG
                throw new InvalidOperationException($"XML loading failed. Fix your XML files before continuing: {ex.Message}", ex);
#else
                Log.Warn(LogArea.Dungeon, "⚠️ Falling back to hardcoded values");
#endif
            }
        }

        private static void LoadFromXml(XDocument doc, bool loadWalls, bool loadFloors)
        {
            Log.Info(LogArea.Dungeon, $"Loading from XML: loadWalls={loadWalls}, loadFloors={loadFloors}");

            try
            {
                var root = doc.Root;
                var wallTiles = root?.Element("WallTiles");

                if (wallTiles == null)
                {
                    Log.Error(LogArea.Dungeon, $"❌ No WallTiles element found in XML Doc; loadWalls={loadWalls}, loadFloors={loadFloors}");
                    throw new InvalidDataException($"Invalid XML structure in XML Doc; loadWalls={loadWalls}, loadFloors={loadFloors}");
                }

                // Read the number of columns from the TilesetInfo, with a fallback
                int columnsInTileset = 40; // Default fallback for older XML formats
                var tilesetInfo = root?.Element("TilesetInfo");
                if (tilesetInfo != null)
                {
                    var columnsElement = tilesetInfo.Element("Columns");
                    if (columnsElement != null && int.TryParse(columnsElement.Value, out int parsedColumns))
                    {
                        columnsInTileset = parsedColumns;
                        Log.Info(LogArea.Dungeon, $"✅ Found TilesetInfo: Using {columnsInTileset} columns for index calculation.");
                    }
                    else
                    {
                        Log.Warn(LogArea.Dungeon, $"⚠️ TilesetInfo found, but 'Columns' element is missing or invalid. Falling back to {columnsInTileset} columns.");
                    }
                }
                else
                {
                    Log.Warn(LogArea.Dungeon, $"⚠️ No TilesetInfo found in XML. Falling back to {columnsInTileset} columns for index calculation.");
                }

                int tilesLoaded = 0;
                foreach (var tile in wallTiles.Elements("Tile"))
                {
                    var type = tile.Attribute("type")?.Value;
                    var colStr = tile.Attribute("col")?.Value;
                    var rowStr = tile.Attribute("row")?.Value;
                    var patternStr = tile.Attribute("pattern")?.Value;

                    if (string.IsNullOrEmpty(type))
                    {
                        Log.Warn(LogArea.Dungeon, "⚠️ Skipping tile with missing type attribute");
                        continue;
                    }

                    bool isFloor = type.StartsWith("floor_");

                    // Skip if we're not loading this type
                    if ((isFloor && !loadFloors) || (!isFloor && !loadWalls))
                    {
                        continue;
                    }

                    int index;
                    if (!string.IsNullOrEmpty(colStr) && !string.IsNullOrEmpty(rowStr))
                    {
                        if (!int.TryParse(colStr, out int col) || !int.TryParse(rowStr, out int row))
                        {
                            Log.Warn(LogArea.Dungeon, $"❌ Invalid col/row values for tile '{type}': col={colStr}, row={rowStr}");
                            continue;
                        }
                        index = row * columnsInTileset + col;
                    }
                    else
                    {
                        Log.Warn(LogArea.Dungeon, $"❌ Tile '{type}' missing col/row attributes");
                        continue;
                    }

                    if (index < 0 || index >= 1200)
                    {
                        Log.Warn(LogArea.Dungeon, $"❌ Tile '{type}' has invalid index {index} (must be 0-1199)");
                        continue;
                    }

                    tilesLoaded++;

                    if (isFloor)
                    {
                        _floorTileIndices[type] = index;
                        Log.Info(LogArea.Dungeon, $"✅ Added floor tile '{type}' -> floor tileset index {index}");
                    }
                    else
                    {
                        _wallTileIndices[type] = index;

                        if (!string.IsNullOrEmpty(patternStr))
                        {
                            if (int.TryParse(patternStr, out int pattern))
                            {
                                _patternToIndex[pattern] = index;
                                Log.Info(LogArea.Dungeon, $"✅ Added pattern {pattern} -> wall tileset index {index}");
                            }
                            else
                            {
                                Log.Warn(LogArea.Dungeon, $"❌ Invalid pattern value for tile '{type}': {patternStr}");
                            }
                        }
                    }
                }

                Log.Info(LogArea.Dungeon, $"✅ Successfully loaded {tilesLoaded} tiles; loadWalls={loadWalls}, loadFloors={loadFloors}");
            }
            catch (Exception ex)
            {
                Log.Error(LogArea.Dungeon, $"❌ Error loading XML from; loadWalls={loadWalls}, loadFloors={loadFloors}: {ex.Message}");
                throw;
            }
        }

        private static int GetTileFromMask(int mask)
        {
            if (!_initialized)
            {
                throw new InvalidOperationException("AutotileMapper not initialized. Call Initialize() first.");
            }

            int cardinalMask = mask & (1 | 4 | 16 | 64);

            if (_patternToIndex.TryGetValue(cardinalMask, out int tileIndex))
            {
                return tileIndex;
            }

            return _wallTileIndices.GetValueOrDefault("isolated", 0);
        }

        public static bool IsWallTile(int tileId)
        {
            var wallTileset = TilesetManager.Instance.WallTileset;
            if (!_initialized || wallTileset == null)
                return false;
            return _wallTileIndices.Values.Contains(tileId);
        }

        public static bool IsFloorTile(int tileId)
        {
            var floorTileset = TilesetManager.Instance.FloorTileset;
            if (!_initialized || floorTileset == null)
                return false;
            return _floorTileIndices.Values.Contains(tileId);
        }

        public static int GetWallTileIndex(string wallType)
        {
            if (!_initialized)
                throw new InvalidOperationException("AutotileMapper not initialized. Call Initialize() first.");

            return _wallTileIndices.GetValueOrDefault(wallType, 0);
        }

        public static int[] GetWallTileIndices()
        {
            if (!_initialized)
            {
                throw new InvalidOperationException("AutotileMapper not initialized. Call Initialize() first.");
            }

            return _wallTileIndices.Values.ToArray();
        }

        public static int GetFloorTileIndex(string floorType)
        {
            if (!_initialized)
                throw new InvalidOperationException("AutotileMapper not initialized. Call Initialize() first.");

            return _floorTileIndices.GetValueOrDefault(floorType, 0);
        }

        public static void ApplyAutotiling(Tilemap tilemap, int originalWallTileId, Tileset wallTileset, Tileset floorTileset)
        {
            if (!_initialized)
            {
                throw new InvalidOperationException("AutotileMapper not initialized. Call Initialize() first.");
            }

            Log.Info(LogArea.Dungeon, "=== APPLY AUTOTILING DEBUG ===");
            Log.Info(LogArea.Dungeon, $"Original wall tile ID: {originalWallTileId}");

            var originalTiles = new (int id, Tileset tileset)[tilemap.Columns, tilemap.Rows];
            for (int row = 0; row < tilemap.Rows; row++)
            {
                for (int col = 0; col < tilemap.Columns; col++)
                {
                    originalTiles[col, row] = (tilemap.GetTileId(col, row), tilemap.GetTileset(col, row));
                }
            }

            int wallTilesProcessed = 0;
            var autotileUsage = new Dictionary<int, int>();

            for (int row = 0; row < tilemap.Rows; row++)
            {
                for (int col = 0; col < tilemap.Columns; col++)
                {
                    var (currentTileId, currentTileset) = originalTiles[col, row];

                    if (currentTileset == floorTileset && IsFloorTile(currentTileId))
                    {
                        continue;
                    }

                    if (currentTileset == wallTileset && (currentTileId == originalWallTileId || IsWallTile(currentTileId)))
                    {
                        int autotileIndex = GetAutotileIndexFromOriginal(originalTiles, col, row, originalWallTileId, tilemap.Columns, tilemap.Rows, wallTileset);

                        autotileUsage.TryGetValue(autotileIndex, out int currentCount);
                        autotileUsage[autotileIndex] = currentCount + 1;

                        tilemap.SetTile(col, row, autotileIndex, wallTileset);
                        wallTilesProcessed++;
                    }
                }
            }

            Log.Info(LogArea.Dungeon, $"Processed {wallTilesProcessed} wall tiles");
            Log.Verbose(LogArea.Dungeon, "Autotile usage:");
            foreach (var kvp in autotileUsage)
            {
                Log.Verbose(LogArea.Dungeon, $"  Tile ID {kvp.Key}: used {kvp.Value} times");
            }
        }

        private static int GetAutotileIndexFromOriginal((int id, Tileset tileset)[,] originalTiles, int col, int row, int wallTileId, int columns, int rows, Tileset wallTileset)
        {
            bool n = IsWallInOriginal(originalTiles, col, row - 1, wallTileId, columns, rows, wallTileset);
            bool e = IsWallInOriginal(originalTiles, col + 1, row, wallTileId, columns, rows, wallTileset);
            bool s = IsWallInOriginal(originalTiles, col, row + 1, wallTileId, columns, rows, wallTileset);
            bool w = IsWallInOriginal(originalTiles, col - 1, row, wallTileId, columns, rows, wallTileset);

            int mask = 0;
            if (n) mask |= 1;
            if (e) mask |= 4;
            if (s) mask |= 16;
            if (w) mask |= 64;

            return GetTileFromMask(mask);
        }
        
        public static int[] GetFloorTileIds()
        {
            if (!_initialized)
            {
                throw new InvalidOperationException("AutotileMapper not initialized. Call Initialize() first.");
            }

            return _floorTileIndices.Values.ToArray();
        }

        private static bool IsWallInOriginal((int id, Tileset tileset)[,] originalTiles, int col, int row, int originalWallTileId, int columns, int rows, Tileset wallTileset)
        {
            if (col < 0 || col >= columns || row < 0 || row >= rows)
                return true; // Treat out-of-bounds as walls

            var (tileId, tileTileset) = originalTiles[col, row];

            return tileTileset == wallTileset && (tileId == originalWallTileId || IsWallTile(tileId));
        }

        public static void DebugTileSeparation()
        {
            if (!_initialized)
            {
                Log.Warn(LogArea.Dungeon, "AutotileMapper not initialized!");
                return;
            }

            Log.Info(LogArea.Dungeon, "=== TILE DEBUG ===");

            Log.Info(LogArea.Dungeon, "Wall tileset tiles:");
            foreach (var kvp in _wallTileIndices)
            {
                Log.Info(LogArea.Dungeon, $"  '{kvp.Key}' -> wall tileset index {kvp.Value}");
            }

            Log.Info(LogArea.Dungeon, "\nFloor tileset tiles:");
            foreach (var kvp in _floorTileIndices)
            {
                Log.Info(LogArea.Dungeon, $"  '{kvp.Key}' -> floor tileset index {kvp.Value}");
            }

            var wallValues = _wallTileIndices.Values.Distinct().OrderBy(v => v);
            var floorValues = _floorTileIndices.Values.Distinct().OrderBy(v => v);

            Log.Info(LogArea.Dungeon, $"\nWall tileset indices: [{string.Join(", ", wallValues)}]");
            Log.Info(LogArea.Dungeon, $"Floor tileset indices: [{string.Join(", ", floorValues)}]");
        }
    }
}