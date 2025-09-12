using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Graphics;
using System.Collections.Generic;
using Hearthvale.GameCode.Collision;
using Hearthvale.GameCode.Utils;
using MonoGame.Extended;
using Microsoft.Xna.Framework;

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
            _wallCollisionActors = new List<WallCollisionActor>();
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
        
        // Physics collision integration
        private List<WallCollisionActor> _wallCollisionActors;
        private CollisionWorld _collisionWorld;

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
            
            // If we have a collision world, initialize collision for the new tilemap
            if (_collisionWorld != null)
            {
                InitializePhysicsCollision(_collisionWorld);
            }
        }

        /// <summary>
        /// Initialize physics-based collision actors for wall tiles in the current tilemap.
        /// This replaces the legacy tile-based collision checking.
        /// </summary>
        public void InitializePhysicsCollision(CollisionWorld collisionWorld)
        {
            _collisionWorld = collisionWorld;
            
            // Clear existing wall collision actors
            ClearWallCollisionActors();
            
            if (_tilemap == null || _wallTileset == null)
            {
                return;
            }

            // Convert wall tiles to collision actors
            // Use run-length encoding to create fewer, larger collision rectangles for better performance
            var wallRectangles = ExtractWallRectangles();
            
            foreach (var rect in wallRectangles)
            {
                var rectF = new RectangleF(rect.X, rect.Y, rect.Width, rect.Height);
                var wallActor = new WallCollisionActor(rectF); // Fixed: pass bounds to constructor
                
                _wallCollisionActors.Add(wallActor);
                _collisionWorld.AddActor(wallActor);
            }
        }

        /// <summary>
        /// Extracts wall collision rectangles using run-length encoding for better performance.
        /// This creates fewer, larger rectangles instead of one per tile.
        /// </summary>
        private List<Rectangle> ExtractWallRectangles()
        {
            var rectangles = new List<Rectangle>();
            
            if (_tilemap == null || _wallTileset == null)
                return rectangles;

            int rows = _tilemap.Rows;
            int cols = _tilemap.Columns;
            float tileWidth = _tilemap.TileWidth;
            float tileHeight = _tilemap.TileHeight;

            // Process each row to find horizontal runs of wall tiles
            for (int row = 0; row < rows; row++)
            {
                int runStart = -1;
                
                for (int col = 0; col <= cols; col++) // Note: <= to handle end of row
                {
                    bool isWall = col < cols && IsWallTile(col, row);
                    
                    if (isWall)
                    {
                        // Start of a new run
                        if (runStart == -1)
                            runStart = col;
                    }
                    else if (runStart != -1)
                    {
                        // End of a run, create rectangle
                        int runLength = col - runStart;
                        var rect = new Rectangle(
                            (int)(runStart * tileWidth),
                            (int)(row * tileHeight),
                            (int)(runLength * tileWidth),
                            (int)tileHeight
                        );
                        rectangles.Add(rect);
                        runStart = -1;
                    }
                }
            }

            return rectangles;
        }

        /// <summary>
        /// Determines if the tile at the given coordinates is a wall tile.
        /// </summary>
        private bool IsWallTile(int col, int row)
        {
            if (col < 0 || col >= _tilemap.Columns || row < 0 || row >= _tilemap.Rows)
                return false;

            var tileTileset = _tilemap.GetTileset(col, row);
            var tileId = _tilemap.GetTileId(col, row);
            
            return tileTileset == _wallTileset && AutotileMapper.IsWallTile(tileId);
        }

        /// <summary>
        /// Clears all wall collision actors from the collision world.
        /// </summary>
        private void ClearWallCollisionActors()
        {
            if (_collisionWorld != null)
            {
                foreach (var actor in _wallCollisionActors)
                {
                    _collisionWorld.RemoveActor(actor);
                }
            }
            
            _wallCollisionActors.Clear();
        }

        /// <summary>
        /// Gets all wall collision actors for debugging purposes.
        /// </summary>
        public IReadOnlyList<WallCollisionActor> GetWallCollisionActors() => _wallCollisionActors.AsReadOnly();

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

        /// <summary>
        /// Legacy method for backwards compatibility.
        /// Returns true if the tile at the given world coordinates is a wall.
        /// Note: This is deprecated - use physics collision system instead.
        /// </summary>
        [System.Obsolete("Use physics collision system instead of direct tile checking")]
        public bool IsWallAt(float worldX, float worldY)
        {
            if (_tilemap == null) return false;

            int col = (int)(worldX / _tilemap.TileWidth);
            int row = (int)(worldY / _tilemap.TileHeight);
            
            return IsWallTile(col, row);
        }

        /// <summary>
        /// Cleanup method to properly dispose of collision actors.
        /// </summary>
        public void Cleanup()
        {
            ClearWallCollisionActors();
            _collisionWorld = null;
        }
    }

}