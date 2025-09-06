using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Graphics;
using System.Collections.Generic;

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

        private TilesetManager() 
        { 
            _tilesets = new Dictionary<string, Tileset>();
            _layerOrder = new List<string>();
        }
        
        public static void Initialize()
        {
            _instance = new TilesetManager();
        }

        private Tileset _wallTileset;
        private Tileset _floorTileset;
        private Tilemap _tilemap;
        private Dictionary<string, Tileset> _tilesets;
        private List<string> _layerOrder;

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
            
            // Register in the dictionary
            RegisterTileset("WallTileset", wallTileset, 1);
            RegisterTileset("FloorTileset", floorTileset, 0);
        }
        
        public void SetTilemap(Tilemap tilemap)
        {
            _tilemap = tilemap;
        }

        /// <summary>
        /// Register a tileset with a specific name and layer order.
        /// Lower order values render first (background).
        /// </summary>
        public void RegisterTileset(string name, Tileset tileset, int layerOrder = 0)
        {
            _tilesets[name] = tileset;
            
            // Maintain layer order
            if (!_layerOrder.Contains(name))
            {
                _layerOrder.Add(name);
                _layerOrder.Sort((a, b) => GetLayerOrder(a).CompareTo(GetLayerOrder(b)));
            }
        }

        /// <summary>
        /// Get the render order for a tileset layer.
        /// </summary>
        private int GetLayerOrder(string name)
        {
            // Define default layer orders
            return name switch
            {
                "FloorTileset" => 0,
                "WallTileset" => 1,
                "Decorations" => 2,
                "Interactive" => 3,
                "Effects" => 4,
                _ => 99
            };
        }

        /// <summary>
        /// Get all tilesets in render order.
        /// </summary>
        public IEnumerable<(string Name, Tileset Tileset)> GetTilesetsInRenderOrder()
        {
            foreach (var name in _layerOrder)
            {
                if (_tilesets.TryGetValue(name, out var tileset))
                    yield return (name, tileset);
            }
        }

        /// <summary>
        /// Get a tileset by name ("WallTileset" or "FloorTileset").
        /// </summary>
        public static Tileset GetTileset(string name)
        {
            if (Instance._tilesets.TryGetValue(name, out var tileset))
                return tileset;
                
            // Fallback to legacy properties
            if (name == "WallTileset")
                return Instance.WallTileset;
            if (name == "FloorTileset")
                return Instance.FloorTileset;
            return null;
        }
    }
}