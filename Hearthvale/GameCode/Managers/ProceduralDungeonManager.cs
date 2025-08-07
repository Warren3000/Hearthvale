using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Hearthvale.GameCode.Utils;
using System.Xml.Linq;

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

    public Tilemap GenerateBasicDungeon(
         ContentManager content,
         int columns, int rows,
         Tileset wallTileset, XDocument wallAutotileXml,
         Tileset floorTileset, XDocument floorAutotileXml)
        {
        AutotileMapper.Initialize(wallAutotileXml, floorAutotileXml);

        _random = new Random();
        _rooms = new List<Rectangle>();
        _corridors = new List<Rectangle>();

        int initialWallTileId = AutotileMapper.GetWallTileIndex("isolated");
        int[] floorTileIds = AutotileMapper.GetFloorTileIds();

        System.Diagnostics.Debug.WriteLine($"Initial wall tile ID (for wall tileset): {initialWallTileId}");
        System.Diagnostics.Debug.WriteLine($"Floor tile IDs (for floor tileset): [{string.Join(", ", floorTileIds)}]");

        var tilemap = new Tilemap(floorTileset, columns, rows);

        FillWithWalls(tilemap, columns, rows, wallTileset, initialWallTileId);
        GenerateRooms(columns, rows);
        ConnectRooms();

        CarveRoomsWithDebug(tilemap, floorTileIds, floorTileset);
        CarveCorridors(tilemap, floorTileIds[0], floorTileset);

        AutotileMapper.ApplyAutotiling(tilemap, initialWallTileId, wallTileset, floorTileset);

        PlaceDungeonElements(tilemap, columns, rows, floorTileIds, floorTileset);

        return tilemap;
    }

    // Update FillWithWalls to accept tileset parameter
    private void FillWithWalls(Tilemap tilemap, int columns, int rows, Tileset wallTileset, int initialWallTileId)
    {
        for (int y = 0; y < rows; y++)
            for (int x = 0; x < columns; x++)
                tilemap.SetTile(x, y, initialWallTileId, wallTileset);
    }
    private void ValidateTilesetIndices(int[] floorTileIds, Tileset floorTileset, Tileset wallTileset)
    {
        System.Diagnostics.Debug.WriteLine("=== TILESET VALIDATION ===");

        // Check floor tile indices
        System.Diagnostics.Debug.WriteLine($"Floor tileset has {floorTileset.Count} tiles");
        foreach (int tileId in floorTileIds)
        {
            if (tileId >= floorTileset.Count || tileId < 0)
            {
                System.Diagnostics.Debug.WriteLine($"❌ ERROR: Floor tile ID {tileId} is out of bounds (0-{floorTileset.Count - 1})");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"✅ Floor tile ID {tileId} is valid");
            }
        }

        // Check wall tile indices
        System.Diagnostics.Debug.WriteLine($"Wall tileset has {wallTileset.Count} tiles");
        var wallTileIds = AutotileMapper.GetWallTileIndices();
        foreach (int tileId in wallTileIds)
        {
            if (tileId >= wallTileset.Count || tileId < 0)
            {
                System.Diagnostics.Debug.WriteLine($"❌ ERROR: Wall tile ID {tileId} is out of bounds (0-{wallTileset.Count - 1})");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"✅ Wall tile ID {tileId} is valid");
            }
        }
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

    private void CarveRoomsWithDebug(Tilemap tilemap, int[] floorTileIds, Tileset floorTileset)
    {
        System.Diagnostics.Debug.WriteLine($"=== CarveRooms DEBUG ===");

        int totalTilesCarved = 0;
        Dictionary<int, int> tileUsageCount = new Dictionary<int, int>();

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

                        // ✅ FIXED: Use tileset parameter correctly
                        if (floorTileset != null)
                        {
                            tilemap.SetTile(x, y, floorTileId, floorTileset);
                        }
                        else
                        {
                            tilemap.SetTile(x, y, floorTileId); // Use default tileset
                        }

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

    private void CarveCorridors(Tilemap tilemap, int floorTileId, Tileset floorTileset)
    {
        foreach (var corridor in _corridors)
        {
            for (int y = corridor.Y; y < corridor.Y + corridor.Height; y++)
            {
                for (int x = corridor.X; x < corridor.X + corridor.Width; x++)
                {
                    if (x >= 0 && x < tilemap.Columns && y >= 0 && y < tilemap.Rows)
                    {
                        // Use the floor tileset for corridor tiles
                        tilemap.SetTile(x, y, floorTileId, floorTileset);
                    }
                }
            }
        }
    }

    private void PlaceDungeonElements(Tilemap tilemap, int columns, int rows, int[] floorTileIds, Tileset floorTileset)
    {
        if (_rooms.Count < 2) return;

        // Find valid floor positions
        var floorPositions = FindFloorPositions(tilemap, columns, rows, floorTileset);

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

        if (_rooms.Any())
        {
            // Spawn in the center of the first room
            var firstRoom = _rooms.First();
            var spawnPos = new Vector2(
                firstRoom.Center.X * tilemap.TileWidth,
                firstRoom.Center.Y * tilemap.TileHeight
            );
            System.Diagnostics.Debug.WriteLine($"✅ Valid player start in first room: {spawnPos}");
            return spawnPos;
        }

        // Emergency fallback to center
        Vector2 centerPos = new Vector2(
            (tilemap.Columns / 2) * tilemap.TileWidth,
            (tilemap.Rows / 2) * tilemap.TileHeight
        );

        System.Diagnostics.Debug.WriteLine($"❌ No rooms found! Using center as emergency fallback: {centerPos}");
        return centerPos;
    }

    private List<Point> FindFloorPositions(Tilemap tilemap, int columns, int rows, Tileset floorTileset)
    {
        var floorPositions = new List<Point>();
        for (int row = 2; row < rows - 2; row++) // Avoid edges
        {
            for (int col = 2; col < columns - 2; col++) // Avoid edges
            {
                if (tilemap.GetTileset(col, row) == floorTileset && AutotileMapper.IsFloorTile(tilemap.GetTileId(col, row)))
                {
                    floorPositions.Add(new Point(col, row));
                }
            }
        }
        return floorPositions;
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
}