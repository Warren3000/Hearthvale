using Hearthvale.GameCode.Tools;
using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace Hearthvale.GameCode.Data
{
    public class LevelChunk
    {
        public int Width { get; set; }
        public int Height { get; set; }
        
        // Tile Data
        // Flattened array: index = y * Width + x
        public int[] TileIds { get; set; }
        public string[] TilesetNames { get; set; } // Name of tileset for each tile

        // Decoration Data
        public List<PlacedDecoration> Decorations { get; set; } = new List<PlacedDecoration>();

        // Entity Data (Placeholder for now)
        public List<EntityData> Entities { get; set; } = new List<EntityData>();

        public LevelChunk() { }

        public LevelChunk(int width, int height)
        {
            Width = width;
            Height = height;
            TileIds = new int[width * height];
            TilesetNames = new string[width * height];
            
            // Initialize with -1 or empty
            for(int i=0; i<TileIds.Length; i++) TileIds[i] = -1;
        }
    }

    public class EntityData
    {
        public string Type { get; set; }
        public Vector2 Position { get; set; }
        public Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();
    }
}