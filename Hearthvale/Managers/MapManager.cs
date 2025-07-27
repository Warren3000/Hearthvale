using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Tiled;
using MonoGame.Extended.Tiled.Renderers;
using System;
using System.Linq;

namespace Hearthvale.Managers
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
        public Vector2 GetPlayerSpawnPoint()
        {
            var entityLayer = GetObjectLayer("Entities");
            if (entityLayer == null)
                throw new Exception("Entities layer not found in the map.");

            var playerObject = entityLayer.Objects.FirstOrDefault(obj => obj.Type == "Player");
            if (playerObject == null)
                throw new Exception("Player spawn point not found in Entities layer.");

            return new Vector2(playerObject.Position.X, playerObject.Position.Y);
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
