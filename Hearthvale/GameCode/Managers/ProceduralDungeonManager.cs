using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Graphics;
using System;

public class ProceduralDungeonManager : DungeonManager
{
    public const int DefaultWallTileId = 2; // Change this value as needed

    public int WallTileId => DefaultWallTileId;
    public Tilemap GenerateBasicDungeon(ContentManager content)
    {
        int columns = 40; // Increased from 20
        int rows = 30;    // Increased from 15
        int[] floorTileIds = { 1, 2, 3 }; // Replace with your actual floor tile IDs

        var tileset = new Tileset(
            new TextureRegion(content.Load<Texture2D>("Tilesets/DampDungeons/Tiles/Dungeon_WallsAndFloors"), 0, 0, 640, 480),
            32, 32);

        var tilemap = new Tilemap(tileset, columns, rows);

        // Fill with walls
        for (int y = 0; y < rows; y++)
            for (int x = 0; x < columns; x++)
                tilemap.SetTile(x, y, DefaultWallTileId);

        // Carve out a room in the center
        for (int y = 3; y < rows - 3; y++)
            for (int x = 3; x < columns - 3; x++)
                tilemap.SetTile(x, y, floorTileIds[0]);

        // Carve out two rooms
        for (int y = 3; y < 10; y++)
            for (int x = 3; x < 15; x++)
                tilemap.SetTile(x, y, floorTileIds[0]);

        for (int y = 20; y < 27; y++)
            for (int x = 25; x < 37; x++)
                tilemap.SetTile(x, y, floorTileIds[0]);

        // Connect rooms with a corridor
        for (int y = 10; y < 20; y++)
            for (int x = 10; x < 15; x++)
                tilemap.SetTile(x, y, floorTileIds[0]);

        // Place a door and a switch
        var door = new DungeonDoor("door_1", columns / 2, 3, DefaultWallTileId, floorTileIds[0]);
        var switchObj = new DungeonSwitch("switch_1", columns / 2, rows - 4, floorTileIds[0], DefaultWallTileId);

        AddElement(door);
        AddElement(switchObj);
        WireUp("switch_1", "door_1");

        // Additional door and switch
        var door2 = new DungeonDoor("door_2", 25, 20, DefaultWallTileId, floorTileIds[0]);
        var switch2 = new DungeonSwitch("switch_2", 10, 9, floorTileIds[0], DefaultWallTileId);
        AddElement(door2);
        AddElement(switch2);
        WireUp("switch_2", "door_2");

        var rand = new Random();
        int roomCount = 5;
        for (int i = 0; i < roomCount; i++)
        {
            int roomX = rand.Next(2, columns - 10);
            int roomY = rand.Next(2, rows - 10);
            int roomW = rand.Next(5, 10);
            int roomH = rand.Next(5, 10);
            for (int y = roomY; y < roomY + roomH; y++)
                for (int x = roomX; x < roomX + roomW; x++)
                {
                    int floorTileId = floorTileIds[rand.Next(floorTileIds.Length)];
                    tilemap.SetTile(x, y, floorTileId);
                }
        }

        return tilemap;
    }

    public Vector2 GetPlayerStart(Tilemap tilemap)
    {
        int startCol = tilemap.Columns / 2;
        int floorTileId = 1; // Must match the value used in GenerateBasicDungeon

        // --- Defensive checks for tile size ---
        if (tilemap.TileWidth == 0 || tilemap.TileHeight == 0)
        {
            return Vector2.Zero;
        }

        // Scan from the bottom of the carved room upwards
        for (int y = tilemap.Rows - 4; y >= 3; y--)
        {
            int tileIndex = y * tilemap.Columns + startCol;
            if (tilemap != null && tilemap.GetTile(startCol, y) != null)
            {
                var tileId = GetTileId(tilemap, startCol, y);
                if (tileId == floorTileId)
                {
                    var spawn = new Vector2(startCol * tilemap.TileWidth, y * tilemap.TileHeight);
                    return spawn;
                }
            }
        }
        // Fallback: use the center of the carved room
        int fallbackRow = tilemap.Rows / 2;
        var fallbackSpawn = new Vector2(startCol * tilemap.TileWidth, fallbackRow * tilemap.TileHeight);
        return fallbackSpawn;
    }

    private int GetTileId(Tilemap tilemap, int column, int row)
    {
        // The tilemap stores tile IDs in its _tiles array
        // Index = row * columns + column
        var tilesField = typeof(Tilemap).GetField("_tiles", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (tilesField != null)
        {
            int[] tiles = (int[])tilesField.GetValue(tilemap);
            int index = row * tilemap.Columns + column;
            if (index >= 0 && index < tiles.Length)
                return tiles[index];
        }
        return -1;
    }
}