using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Graphics;

namespace Hearthvale.GameCode.Managers
{
    /// <summary>
    /// Singleton manager for global access to the current wall and floor tilesets.
    /// </summary>
    public class TilesetManager
    {
        private static TilesetManager _instance;
        public static TilesetManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new TilesetManager();
                return _instance;
            }
        }

        private TilesetManager() { }
        public static void Initialize()
        {
            _instance = new TilesetManager();
        }

        private Tileset _wallTileset;
        private Tileset _floorTileset;
        private Tilemap _tilemap;

        /// <summary>
        /// The current wall tileset.
        /// </summary>
        public Tileset WallTileset => _wallTileset;

        /// <summary>
        /// The current floor tileset.
        /// </summary>
        public Tileset FloorTileset => _floorTileset;
        public Tilemap Tilemap => _tilemap;

        /// <summary>
        /// Set the wall and floor tilesets for global access.
        /// </summary>
        public void SetTilesets(Tileset wallTileset, Tileset floorTileset)
        {
            _wallTileset = wallTileset;
            _floorTileset = floorTileset;
        }
        public void SetTilemap(Tilemap tilemap)
        {
            _tilemap = tilemap;
        }

        /// <summary>
        /// Get a tileset by name ("WallTileset" or "FloorTileset").
        /// </summary>
        public static Tileset GetTileset(string name)
        {
            if (name == "WallTileset")
                return Instance.WallTileset;
            if (name == "FloorTileset")
                return Instance.FloorTileset;
            return null;
        }
    }
}