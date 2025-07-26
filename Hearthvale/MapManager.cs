using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Tiled;
using MonoGame.Extended.Tiled.Renderers;
using System.Linq;

namespace Hearthvale
{
    public class MapManager
    {
        private TiledMap _map;
        private TiledMapRenderer _mapRenderer;
        private Rectangle _roomBounds;

        public TiledMap Map => _map;
        public Rectangle RoomBounds => _roomBounds;

        public MapManager(GraphicsDevice graphicsDevice, ContentManager content, string mapPath)
        {
            _map = content.Load<TiledMap>(mapPath);
            _mapRenderer = new TiledMapRenderer(graphicsDevice, _map);
            _roomBounds = new Rectangle(0, 0, _map.Width * _map.TileWidth, _map.Height * _map.TileHeight);
        }

        public void Update(GameTime gameTime)
        {
            _mapRenderer.Update(gameTime);
        }

        public void Draw(Matrix transform)
        {
            _mapRenderer.Draw(transform);
        }

        public TiledMapObjectLayer GetObjectLayer(string name)
        {
            return _map.ObjectLayers.FirstOrDefault(layer => layer.Name == name);
        }

        public int MapWidthInPixels => _map.WidthInPixels;
        public int MapHeightInPixels => _map.HeightInPixels;
        public int TileWidth => _map.TileWidth;
        public int TileHeight => _map.TileHeight;
    }
}
