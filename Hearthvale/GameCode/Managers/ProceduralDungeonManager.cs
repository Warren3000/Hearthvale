using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Hearthvale.GameCode.Utils;

public class ProceduralDungeonManager : DungeonManager
{
    public const int DefaultWallTileId = 0;
    public const int DefaultFloorTileId = 80; // Updated to use autotile floor

    // Support for different tile sizes
    public const int AutotileSize = 16; // 16x16 for autotiles
    public const int StandardTileSize = 32; // 32x32 for standard tiles

    // Room generation parameters
    private const int MinRoomSize = 8;
    private const int MaxRoomSize = 16;
    private const int MaxRoomAttempts = 50;
    private const int CorridorWidth = 3;

    private Random _random;
    private List<Rectangle> _rooms;
    private List<Rectangle> _corridors;

    public int WallTileId => DefaultWallTileId;

    public Tilemap GenerateBasicDungeon(ContentManager content)
    {
        AutotileMapper.Initialize(content);
        
        // Add comprehensive debug output
        System.Diagnostics.Debug.WriteLine("=== DEBUG: Dungeon Generation Start ===");
        AutotileMapper.DebugPrintFloorConfiguration();
        AutotileMapper.DebugTilesetPositions(); // Add this new debug method
        
        _random = new Random();
        _rooms = new List<Rectangle>();
        _corridors = new List<Rectangle>();

        int columns = 80;
        int rows = 60;

        int initialWallTileId = AutotileMapper.GetWallTileIndex("isolated");
        int[] floorTileIds = AutotileMapper.GetFloorTileIds();
        
        System.Diagnostics.Debug.WriteLine($"Initial wall tile ID: {initialWallTileId}");
        System.Diagnostics.Debug.WriteLine($"Floor tile IDs for generation: [{string.Join(", ", floorTileIds)}]");
        System.Diagnostics.Debug.WriteLine($"Floor tile count: {floorTileIds.Length}");

        // Create tileset with 16x16 tiles
        var tileset = new Tileset(
            new TextureRegion(content.Load<Texture2D>("Tilesets/DampDungeons/Tiles/Dungeon_WallsAndFloors"), 0, 0, 640, 480),
            AutotileSize, AutotileSize);

        System.Diagnostics.Debug.WriteLine($"Tileset created: {tileset.Columns} columns x {tileset.Rows} rows = {tileset.Count} total tiles");

        var tilemap = new Tilemap(tileset, columns, rows);

        FillWithWalls(tilemap, columns, rows);
        GenerateRooms(columns, rows);
        ConnectRooms();
        
        // Debug room carving with detailed tile tracking
        System.Diagnostics.Debug.WriteLine($"About to carve {_rooms.Count} rooms");
        CarveRoomsWithDebug(tilemap, floorTileIds);
        CarveCorridors(tilemap, floorTileIds[0]);

        AutotileMapper.ApplyAutotiling(tilemap, initialWallTileId);
        PlaceDungeonElements(tilemap, columns, rows, floorTileIds);

        return tilemap;
    }

    private void FillWithWalls(Tilemap tilemap, int columns, int rows)
    {
        // Get the appropriate initial wall tile ID from XML configuration
        int initialWallTileId = AutotileMapper.GetWallTileIndex("isolated");

        for (int y = 0; y < rows; y++)
            for (int x = 0; x < columns; x++)
                tilemap.SetTile(x, y, initialWallTileId);
    }

    private void GenerateRooms(int mapWidth, int mapHeight)
    {
        for (int attempt = 0; attempt < MaxRoomAttempts; attempt++)
        {
            int roomWidth = _random.Next(MinRoomSize, MaxRoomSize + 1);
            int roomHeight = _random.Next(MinRoomSize, MaxRoomSize + 1);
            int roomX = _random.Next(2, mapWidth - roomWidth - 2);
            int roomY = _random.Next(2, mapHeight - roomHeight - 2);

            var newRoom = new Rectangle(roomX, roomY, roomWidth, roomHeight);

            // Check if this room overlaps with existing rooms (with padding)
            bool overlaps = false;
            foreach (var existingRoom in _rooms)
            {
                var paddedRoom = new Rectangle(
                    existingRoom.X - 2, existingRoom.Y - 2,
                    existingRoom.Width + 4, existingRoom.Height + 4);

                if (newRoom.Intersects(paddedRoom))
                {
                    overlaps = true;
                    break;
                }
            }

            if (!overlaps)
            {
                _rooms.Add(newRoom);

                // Stop when we have enough rooms
                if (_rooms.Count >= 8)
                    break;
            }
        }

        // Ensure we have at least one room
        if (_rooms.Count == 0)
        {
            _rooms.Add(new Rectangle(10, 10, 12, 12));
        }
    }

    private void ConnectRooms()
    {
        if (_rooms.Count < 2) return;

        // Sort rooms by position for better connectivity
        var sortedRooms = _rooms.OrderBy(r => r.X + r.Y).ToList();

        for (int i = 0; i < sortedRooms.Count - 1; i++)
        {
            var roomA = sortedRooms[i];
            var roomB = sortedRooms[i + 1];

            CreateCorridor(roomA, roomB);
        }

        // Add some additional connections for interesting layouts
        if (_rooms.Count > 3)
        {
            int additionalConnections = Math.Min(2, _rooms.Count / 3);
            for (int i = 0; i < additionalConnections; i++)
            {
                var roomA = _rooms[_random.Next(_rooms.Count)];
                var roomB = _rooms[_random.Next(_rooms.Count)];
                if (roomA != roomB)
                {
                    CreateCorridor(roomA, roomB);
                }
            }
        }
    }

    private void CreateCorridor(Rectangle roomA, Rectangle roomB)
    {
        var centerA = roomA.Center;
        var centerB = roomB.Center;

        // Create L-shaped corridor
        if (_random.Next(2) == 0)
        {
            // Horizontal first, then vertical
            var horizontalCorridor = new Rectangle(
                Math.Min(centerA.X, centerB.X) - CorridorWidth / 2,
                centerA.Y - CorridorWidth / 2,
                Math.Abs(centerB.X - centerA.X) + CorridorWidth,
                CorridorWidth);

            var verticalCorridor = new Rectangle(
                centerB.X - CorridorWidth / 2,
                Math.Min(centerA.Y, centerB.Y) - CorridorWidth / 2,
                CorridorWidth,
                Math.Abs(centerB.Y - centerA.Y) + CorridorWidth);

            _corridors.Add(horizontalCorridor);
            _corridors.Add(verticalCorridor);
        }
        else
        {
            // Vertical first, then horizontal
            var verticalCorridor = new Rectangle(
                centerA.X - CorridorWidth / 2,
                Math.Min(centerA.Y, centerB.Y) - CorridorWidth / 2,
                CorridorWidth,
                Math.Abs(centerB.Y - centerA.Y) + CorridorWidth);

            var horizontalCorridor = new Rectangle(
                Math.Min(centerA.X, centerB.X) - CorridorWidth / 2,
                centerB.Y - CorridorWidth / 2,
                Math.Abs(centerB.X - centerA.X) + CorridorWidth,
                CorridorWidth);

            _corridors.Add(verticalCorridor);
            _corridors.Add(horizontalCorridor);
        }
    }

    private void CarveRooms(Tilemap tilemap, int[] floorTileIds)
    {
        foreach (var room in _rooms)
        {
            for (int y = room.Y; y < room.Y + room.Height; y++)
            {
                for (int x = room.X; x < room.X + room.Width; x++)
                {
                    if (x >= 0 && x < tilemap.Columns && y >= 0 && y < tilemap.Rows)
                    {
                        int floorTileId = floorTileIds[_random.Next(floorTileIds.Length)];
                        tilemap.SetTile(x, y, floorTileId);
                    }
                }
            }
        }
    }

    private void CarveCorridors(Tilemap tilemap, int floorTileId)
    {
        foreach (var corridor in _corridors)
        {
            for (int y = corridor.Y; y < corridor.Y + corridor.Height; y++)
            {
                for (int x = corridor.X; x < corridor.X + corridor.Width; x++)
                {
                    if (x >= 0 && x < tilemap.Columns && y >= 0 && y < tilemap.Rows)
                    {
                        tilemap.SetTile(x, y, floorTileId);
                    }
                }
            }
        }
    }

    private void PlaceDungeonElements(Tilemap tilemap, int columns, int rows, int[] floorTileIds)
    {
        if (_rooms.Count < 2) return;

        // Find valid floor positions
        var floorPositions = FindFloorPositions(tilemap, columns, rows, floorTileIds);

        if (floorPositions.Count >= 4)
        {
            var selectedPositions = new List<Point>();

            // Try to place elements in different rooms
            foreach (var room in _rooms.Take(4))
            {
                var roomFloorPositions = floorPositions
                    .Where(p => room.Contains(p))
                    .ToList();

                if (roomFloorPositions.Any())
                {
                    var randomPos = roomFloorPositions[_random.Next(roomFloorPositions.Count)];
                    selectedPositions.Add(randomPos);
                    floorPositions.Remove(randomPos);
                }
            }

            // Fill remaining positions if needed
            while (selectedPositions.Count < 4 && floorPositions.Any())
            {
                var randomPos = floorPositions[_random.Next(floorPositions.Count)];
                selectedPositions.Add(randomPos);
                floorPositions.Remove(randomPos);
            }

            // Create doors and switches with proper autotile IDs
            if (selectedPositions.Count >= 4)
            {
                var wallTileId = AutotileMapper.GetWallTileIndex("isolated");

                var door1 = new DungeonDoor("door_1", selectedPositions[0].X, selectedPositions[0].Y,
                    wallTileId, floorTileIds[0]);
                var switch1 = new DungeonSwitch("switch_1", selectedPositions[1].X, selectedPositions[1].Y,
                    floorTileIds[0], wallTileId);

                var door2 = new DungeonDoor("door_2", selectedPositions[2].X, selectedPositions[2].Y,
                    wallTileId, floorTileIds[0]);
                var switch2 = new DungeonSwitch("switch_2", selectedPositions[3].X, selectedPositions[3].Y,
                    floorTileIds[0], wallTileId);

                AddElement(door1);
                AddElement(switch1);
                AddElement(door2);
                AddElement(switch2);

                WireUp("switch_1", "door_1");
                WireUp("switch_2", "door_2");
            }
        }
    }

    public Vector2 GetPlayerStart(Tilemap tilemap)
    {
        System.Diagnostics.Debug.WriteLine("=== PLAYER SPAWN DEBUG ===");
        System.Diagnostics.Debug.WriteLine($"Tilemap size: {tilemap.Columns}x{tilemap.Rows}");
        System.Diagnostics.Debug.WriteLine($"Tile size: {tilemap.TileWidth}x{tilemap.TileHeight}");
        
        int floorTilesFound = 0;
        int wallTilesFound = 0;
        Vector2 firstFloorTile = Vector2.Zero;
        bool foundFirstFloor = false;
        
        // Find a safe spawn position (not on a wall)
        for (int row = 1; row < tilemap.Rows - 1; row++)
        {
            for (int col = 1; col < tilemap.Columns - 1; col++)
            {
                int tileId = tilemap.GetTileId(col, row);
                
                if (AutotileMapper.IsWallTile(tileId))
                {
                    wallTilesFound++;
                }
                else
                {
                    floorTilesFound++;
                    if (!foundFirstFloor)
                    {
                        firstFloorTile = new Vector2(col * tilemap.TileWidth, row * tilemap.TileHeight);
                        foundFirstFloor = true;
                        System.Diagnostics.Debug.WriteLine($"First floor tile found at grid ({col}, {row}) = world ({firstFloorTile.X}, {firstFloorTile.Y}), tileId = {tileId}");
                    }
                    
                    // Return the first valid floor tile we find
                    Vector2 spawnPos = new Vector2(col * tilemap.TileWidth, row * tilemap.TileHeight);
                    System.Diagnostics.Debug.WriteLine($"✅ Player spawn position: ({spawnPos.X}, {spawnPos.Y})");
                    return spawnPos;
                }
            }
        }
        
        System.Diagnostics.Debug.WriteLine($"Floor tiles found: {floorTilesFound}");
        System.Diagnostics.Debug.WriteLine($"Wall tiles found: {wallTilesFound}");
        
        if (foundFirstFloor)
        {
            System.Diagnostics.Debug.WriteLine($"⚠️  Using first floor tile as fallback: {firstFloorTile}");
            return firstFloorTile;
        }
        
        // Emergency fallback to center
        Vector2 centerPos = new Vector2(
            (tilemap.Columns / 2) * tilemap.TileWidth,
            (tilemap.Rows / 2) * tilemap.TileHeight
        );
        
        System.Diagnostics.Debug.WriteLine($"❌ No floor tiles found! Using center as emergency fallback: {centerPos}");
        return centerPos;
    }

    private List<Point> FindFloorPositions(Tilemap tilemap, int columns, int rows, int[] floorTileIds)
    {
        var floorPositions = new List<Point>();

        for (int row = 2; row < rows - 2; row++) // Avoid edges
        {
            for (int col = 2; col < columns - 2; col++) // Avoid edges
            {
                int tileId = tilemap.GetTileId(col, row);
                if (AutotileMapper.IsFloorTile(tileId))
                {
                    floorPositions.Add(new Point(col, row));
                }
            }
        }

        return floorPositions;
    }

    private int GetTileId(Tilemap tilemap, int column, int row)
    {
        // Simply use the public method instead of reflection
        return tilemap.GetTileId(column, row);
    }

    /// <summary>
    /// Gets all room rectangles for debugging or other purposes
    /// </summary>
    public IEnumerable<Rectangle> GetRooms() => _rooms ?? Enumerable.Empty<Rectangle>();

    /// <summary>
    /// Gets all corridor rectangles for debugging
    /// </summary>
    public IEnumerable<Rectangle> GetCorridors() => _corridors ?? Enumerable.Empty<Rectangle>();

    public static Point ConvertToAutotileCoords(int col32, int row32)
    {
        return new Point(col32 * 2, row32 * 2);
    }

    public static Point ConvertFromAutotileCoords(int col16, int row16)
    {
        return new Point(col16 / 2, row16 / 2);
    }

    private void CarveRoomsWithDebug(Tilemap tilemap, int[] floorTileIds)
    {
        System.Diagnostics.Debug.WriteLine($"=== CarveRooms DEBUG ===");
        System.Diagnostics.Debug.WriteLine($"Using {floorTileIds.Length} floor tile variants: [{string.Join(", ", floorTileIds)}]");
        
        int totalTilesCarved = 0;
        Dictionary<int, int> tileUsageCount = new Dictionary<int, int>();
        
        // Initialize usage counters
        foreach (int tileId in floorTileIds)
        {
            tileUsageCount[tileId] = 0;
        }

        foreach (var room in _rooms)
        {
            System.Diagnostics.Debug.WriteLine($"Carving room at ({room.X}, {room.Y}) size {room.Width}x{room.Height}");
            
            for (int y = room.Y; y < room.Y + room.Height; y++)
            {
                for (int x = room.X; x < room.X + room.Width; x++)
                {
                    if (x >= 0 && x < tilemap.Columns && y >= 0 && y < tilemap.Rows)
                    {
                        int floorTileId = floorTileIds[_random.Next(floorTileIds.Length)];
                        tilemap.SetTile(x, y, floorTileId);
                        tileUsageCount[floorTileId]++;
                        totalTilesCarved++;
                    }
                }
            }
        }
        
        System.Diagnostics.Debug.WriteLine($"Carved {totalTilesCarved} floor tiles total:");
        foreach (var kvp in tileUsageCount)
        {
            System.Diagnostics.Debug.WriteLine($"  Tile ID {kvp.Key}: used {kvp.Value} times ({(kvp.Value * 100.0 / totalTilesCarved):F1}%)");
        }
    }
}