using Hearthvale.GameCode.Rendering;
using Hearthvale.GameCode.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Hearthvale.GameCode.Managers.Dungeon
{
    public class ProceduralDungeonManager : DungeonManager
    {
        public const int AutotileSize = 16;
        private Random _random = new Random();
        private DungeonElementRenderer _elementRenderer;

        // Room generation parameters
        private const float CHEST_SPAWN_CHANCE = 0.3f; // 30% chance per room
        private const float TRAPPED_CHEST_CHANCE = 0.2f; // 20% of chests are trapped
        private const float MULTI_CHEST_CHANCE = 0.1f; // 10% chance for multiple chests

        // Store room information for element placement
        private List<Rectangle> _rooms = new List<Rectangle>();

        public MonoGameLibrary.Graphics.Tilemap GenerateBasicDungeon(
            ContentManager content,
            int columns, int rows,
            Tileset wallTileset, XDocument wallAutotileXml,
            Tileset floorTileset, XDocument floorAutotileXml)
        {
            // Create the tilemap with wall tileset as default
            var tilemap = new MonoGameLibrary.Graphics.Tilemap(wallTileset, columns, rows);

            // Initialize with all walls (don't use SetTile here, just set the tile IDs)
            int initialWallTileId = 15; // Original wall tile ID before autotiling
            for (int x = 0; x < columns; x++)
            {
                for (int y = 0; y < rows; y++)
                {
                    // Use the wall tile ID that's compatible with the autotile system
                    tilemap.SetTile(x, y, initialWallTileId); // 15 is typically a solid wall in autotile systems
                }
            }

            // Generate rooms
            GenerateRooms(tilemap, 10, 15); // Generate 10-15 rooms

            // Generate corridors to connect rooms
            GenerateCorridors(tilemap);

            // Apply autotiling BEFORE placing elements
            ApplyAutotiling(tilemap, initialWallTileId, wallTileset, wallAutotileXml, floorTileset, floorAutotileXml);

            // Place dungeon elements (treasure chests, etc.) AFTER autotiling
            PlaceDungeonElements(tilemap);

            return tilemap;
        }

        private void GenerateRooms(MonoGameLibrary.Graphics.Tilemap tilemap, int minRooms, int maxRooms)
        {
            int roomCount = _random.Next(minRooms, maxRooms + 1);
            _rooms.Clear();

            for (int i = 0; i < roomCount; i++)
            {
                int roomWidth = _random.Next(5, 12);
                int roomHeight = _random.Next(5, 10);
                int x = _random.Next(2, tilemap.Columns - roomWidth - 2);
                int y = _random.Next(2, tilemap.Rows - roomHeight - 2);

                Rectangle newRoom = new Rectangle(x, y, roomWidth, roomHeight);

                // Check if room overlaps with existing rooms
                bool overlaps = false;
                foreach (var room in _rooms)
                {
                    if (newRoom.Intersects(room))
                    {
                        overlaps = true;
                        break;
                    }
                }

                if (!overlaps)
                {
                    _rooms.Add(newRoom);
                    CarveRoom(tilemap, newRoom);
                }
            }
        }

        private void CarveRoom(MonoGameLibrary.Graphics.Tilemap tilemap, Rectangle room)
        {
            // Temporarily mark floor tiles as -1 to distinguish them during autotiling
            for (int x = room.X; x < room.X + room.Width; x++)
            {
                for (int y = room.Y; y < room.Y + room.Height; y++)
                {
                    tilemap.SetTile(x, y, -1); // Temporary floor marker
                }
            }
        }

        private void GenerateCorridors(MonoGameLibrary.Graphics.Tilemap tilemap)
        {
            // Connect each room to the next
            for (int i = 0; i < _rooms.Count - 1; i++)
            {
                Rectangle roomA = _rooms[i];
                Rectangle roomB = _rooms[i + 1];

                Point centerA = new Point(roomA.Center.X, roomA.Center.Y);
                Point centerB = new Point(roomB.Center.X, roomB.Center.Y);

                // Create L-shaped corridor
                if (_random.NextDouble() < 0.5)
                {
                    // Horizontal first, then vertical
                    CarveHorizontalCorridor(tilemap, centerA.X, centerB.X, centerA.Y);
                    CarveVerticalCorridor(tilemap, centerA.Y, centerB.Y, centerB.X);
                }
                else
                {
                    // Vertical first, then horizontal
                    CarveVerticalCorridor(tilemap, centerA.Y, centerB.Y, centerA.X);
                    CarveHorizontalCorridor(tilemap, centerA.X, centerB.X, centerB.Y);
                }
            }
        }

        private void CarveHorizontalCorridor(MonoGameLibrary.Graphics.Tilemap tilemap, int x1, int x2, int y)
        {
            int minX = Math.Min(x1, x2);
            int maxX = Math.Max(x1, x2);

            for (int x = minX; x <= maxX; x++)
            {
                if (y > 0 && y < tilemap.Rows - 1)
                {
                    tilemap.SetTile(x, y, -1); // Temporary floor marker
                    // Make corridors 3 tiles wide
                    if (y > 1) tilemap.SetTile(x, y - 1, -1);
                    if (y < tilemap.Rows - 2) tilemap.SetTile(x, y + 1, -1);
                }
            }
        }

        private void CarveVerticalCorridor(MonoGameLibrary.Graphics.Tilemap tilemap, int y1, int y2, int x)
        {
            int minY = Math.Min(y1, y2);
            int maxY = Math.Max(y1, y2);

            for (int y = minY; y <= maxY; y++)
            {
                if (x > 0 && x < tilemap.Columns - 1)
                {
                    tilemap.SetTile(x, y, -1); // Temporary floor marker
                    // Make corridors 3 tiles wide
                    if (x > 1) tilemap.SetTile(x - 1, y, -1);
                    if (x < tilemap.Columns - 2) tilemap.SetTile(x + 1, y, -1);
                }
            }
        }

        private void PlaceDungeonElements(MonoGameLibrary.Graphics.Tilemap tilemap)
        {
            foreach (var room in _rooms)
            {
                // Decide if this room should have treasure
                if (_random.NextDouble() < CHEST_SPAWN_CHANCE)
                {
                    PlaceTreasureChest(tilemap, room);

                    // Small chance for additional chests
                    if (_random.NextDouble() < MULTI_CHEST_CHANCE)
                    {
                        PlaceTreasureChest(tilemap, room);
                    }
                }
            }
        }

        private void PlaceTreasureChest(MonoGameLibrary.Graphics.Tilemap tilemap, Rectangle room)
        {
            // Find a valid position in the room (not against walls)
            List<Point> validPositions = new List<Point>();

            for (int x = room.X + 1; x < room.X + room.Width - 1; x++)
            {
                for (int y = room.Y + 1; y < room.Y + room.Height - 1; y++)
                {
                    // Check if position is floor (we'll check for proper floor tiles after autotiling)
                    if (!HasNearbyElement(x, y))
                    {
                        validPositions.Add(new Point(x, y));
                    }
                }
            }

            if (validPositions.Count > 0)
            {
                Point position = validPositions[_random.Next(validPositions.Count)];

                // Determine if chest is trapped
                bool isTrapped = _random.NextDouble() < TRAPPED_CHEST_CHANCE;
                string chestId = $"chest_{position.X}_{position.Y}";
                string lootTableId = isTrapped ? "rare_loot" : "common_loot";
                
                // Create trap first if needed
                string trapId = null;
                if (isTrapped)
                {
                    trapId = $"trap_{position.X}_{position.Y}";
                    var trap = new DungeonTrap(trapId, TrapType.Spikes, 15f, 3f, position.X, position.Y);
                    AddElement(trap);
                }

                // Create the chest with trap ID
                var chest = new DungeonLoot(chestId, lootTableId, position.X, position.Y, isTrapped, trapId);
                AddElement(chest);

                Log.Info(LogArea.Dungeon, $"Placed {(isTrapped ? "trapped " : "")}chest at ({position.X}, {position.Y})");
            }
        }

        private bool HasNearbyElement(int x, int y)
        {
            // Check if there's already an element within 2 tiles
            foreach (var element in _elements)
            {
                if (element is DungeonLoot loot)
                {
                    int distance = Math.Abs(loot.Column - x) + Math.Abs(loot.Row - y);
                    if (distance < 2)
                        return true;
                }
            }
            return false;
        }

        public Vector2 GetPlayerStart(MonoGameLibrary.Graphics.Tilemap tilemap)
        {
            // Place player in the center of the first room
            if (_rooms.Count > 0)
            {
                Rectangle firstRoom = _rooms[0];
                return new Vector2(
                    firstRoom.Center.X * tilemap.TileWidth,
                    firstRoom.Center.Y * tilemap.TileHeight
                );
            }

            // Fallback: find any floor tile
            for (int x = 0; x < tilemap.Columns; x++)
            {
                for (int y = 0; y < tilemap.Rows; y++)
                {
                    var tileset = tilemap.GetTileset(x, y);
                    if (tileset == TilesetManager.Instance.FloorTileset)
                    {
                        return new Vector2(x * tilemap.TileWidth, y * tilemap.TileHeight);
                    }
                }
            }

            return new Vector2(100, 100); // Emergency fallback
        }

        private void ApplyAutotiling(MonoGameLibrary.Graphics.Tilemap tilemap,
            int originalWallTileId,
            Tileset wallTileset, XDocument wallAutotileXml,
            Tileset floorTileset, XDocument floorAutotileXml)
        {
            // First pass: Convert temporary floor markers to actual floor tiles
            for (int x = 0; x < tilemap.Columns; x++)
            {
                for (int y = 0; y < tilemap.Rows; y++)
                {
                    int tileId = tilemap.GetTileId(x, y);
                    if (tileId == -1) // Our temporary floor marker
                    {
                        // Set as floor tile with the floor tileset
                        tilemap.SetTile(x, y, 0, floorTileset);
                    }
                }
            }

            // Initialize AutotileMapper with the configuration
            AutotileMapper.Initialize(wallAutotileXml, floorAutotileXml);

            // Apply autotiling using the existing AutotileMapper function
            AutotileMapper.ApplyAutotiling(tilemap, originalWallTileId, wallTileset, floorTileset);
        }

        public void SetElementRenderer(DungeonElementRenderer renderer)
        {
            _elementRenderer = renderer;
        }

        public void DrawElements(SpriteBatch spriteBatch)
        {
            if (_elementRenderer == null || spriteBatch == null) return;

            int tileSize = (int)TilesetManager.Instance.Tilemap.TileWidth;

            // Draw all non-chest elements with the legacy renderer
            foreach (var element in _elements)
            {
                if (element is DungeonLoot)
                    continue; // Chests now handled exclusively by DungeonLootRenderer

                _elementRenderer.DrawElement(spriteBatch, element, tileSize);
            }

            // Draw chests with row-based depth (adjust baseDepth/increment to fit your draw order)
            var chests = GetElements<DungeonLoot>();
            DungeonLootRenderer.Draw(spriteBatch, chests, loot =>
            {
                // Simple y-sort to avoid overlap issues (tweak as needed)
                return MathHelper.Clamp(0.45f + loot.Row * 0.00005f, 0f, 1f);
            });
        }

        public MonoGameLibrary.Graphics.Tilemap GenerateOpenArena(
            ContentManager content,
            int columns, int rows,
            Tileset wallTileset, XDocument wallAutotileXml,
            Tileset floorTileset, XDocument floorAutotileXml)
        {
            // Create the tilemap with wall tileset as default
            var tilemap = new MonoGameLibrary.Graphics.Tilemap(wallTileset, columns, rows);

            // Initialize with all walls
            int initialWallTileId = 15; 
            for (int x = 0; x < columns; x++)
            {
                for (int y = 0; y < rows; y++)
                {
                    tilemap.SetTile(x, y, initialWallTileId);
                }
            }

            // Create a large central arena
            // Leave a border of walls
            int border = 5;
            Rectangle arena = new Rectangle(border, border, columns - (border * 2), rows - (border * 2));
            
            _rooms.Clear();
            _rooms.Add(arena);
            
            CarveRoom(tilemap, arena);

            // Apply autotiling
            ApplyAutotiling(tilemap, initialWallTileId, wallTileset, wallAutotileXml, floorTileset, floorAutotileXml);

            // Place dungeon elements (maybe fewer or specific ones for arena)
            // For now, let's skip random chests in the arena or place them specifically
            // PlaceDungeonElements(tilemap); 

            return tilemap;
        }
    }
}