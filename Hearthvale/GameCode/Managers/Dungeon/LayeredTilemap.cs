using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Graphics;
using System.Collections.Generic;

namespace Hearthvale.GameCode.Managers
{
    public class LayeredTilemap
    {
        private Dictionary<string, Tilemap> _layers;
        private List<string> _layerOrder;
        private int _columns;
        private int _rows;
        private int _tileWidth;
        private int _tileHeight;

        public LayeredTilemap(int columns, int rows, int tileWidth = 32, int tileHeight = 32)
        {
            _columns = columns;
            _rows = rows;
            _tileWidth = tileWidth;
            _tileHeight = tileHeight;
            _layers = new Dictionary<string, Tilemap>();
            _layerOrder = new List<string>();
        }

        /// <summary>
        /// Add a new layer with the specified tileset.
        /// </summary>
        public void AddLayer(string layerName, Tileset tileset)
        {
            var tilemap = new Tilemap(tileset, _columns, _rows);
            _layers[layerName] = tilemap;

            if (!_layerOrder.Contains(layerName))
                _layerOrder.Add(layerName);
        }

        /// <summary>
        /// Get a specific layer's tilemap.
        /// </summary>
        public Tilemap GetLayer(string layerName)
        {
            return _layers.TryGetValue(layerName, out var layer) ? layer : null;
        }

        /// <summary>
        /// Set a tile on a specific layer.
        /// </summary>
        public void SetTile(string layerName, int x, int y, int tileId)
        {
            if (_layers.TryGetValue(layerName, out var layer))
            {
                layer.SetTile(x, y, tileId);
            }
        }

        /// <summary>
        /// Render all layers in order.
        /// </summary>
        public void Draw(SpriteBatch spriteBatch)
        {
            foreach (var layerName in _layerOrder)
            {
                if (_layers.TryGetValue(layerName, out var layer))
                {
                    layer.Draw(spriteBatch);
                }
            }
        }

        /// <summary>
        /// Render all layers in order with viewport culling.
        /// </summary>
        public void Draw(SpriteBatch spriteBatch, Rectangle viewportBounds)
        {
            // Since the base Tilemap doesn't support viewport bounds,
            // we just call the regular Draw method.
            // The tilemap will handle its own culling internally if implemented.
            Draw(spriteBatch);
        }

        /// <summary>
        /// Clear a specific tile position across all layers.
        /// </summary>
        public void ClearTile(int x, int y)
        {
            foreach (var layer in _layers.Values)
            {
                layer.SetTile(x, y, -1); // -1 typically means empty
            }
        }

        /// <summary>
        /// Get the total number of columns in this layered tilemap.
        /// </summary>
        public int Columns => _columns;

        /// <summary>
        /// Get the total number of rows in this layered tilemap.
        /// </summary>
        public int Rows => _rows;

        /// <summary>
        /// Get the width of each tile.
        /// </summary>
        public int TileWidth => _tileWidth;

        /// <summary>
        /// Get the height of each tile.
        /// </summary>
        public int TileHeight => _tileHeight;
    }
}