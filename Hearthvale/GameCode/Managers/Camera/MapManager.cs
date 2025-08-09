using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Tiled;
using MonoGame.Extended.Tiled.Renderers;
using System;
using System.Linq;

namespace Hearthvale.GameCode.Managers
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

        /// <summary>
        /// Protected constructor for derived classes that don't load from file
        /// </summary>
        protected MapManager()
        {
            // Allow derived classes to initialize their own way
        }

        public virtual void Update(GameTime gameTime)
        {
            _mapRenderer?.Update(gameTime);
        }

        public virtual void Draw(Matrix transform)
        {
            _mapRenderer?.Draw(transform);
        }

        public virtual Vector2 GetPlayerSpawnPoint()
        {
            var entityLayer = GetObjectLayer("Entities");
            if (entityLayer == null)
                throw new Exception("Entities layer not found in the map.");

            var playerObject = entityLayer.Objects.FirstOrDefault(obj => obj.Type == "Player");
            if (playerObject == null)
                throw new Exception("Player spawn point not found in Entities layer.");

            return new Vector2(playerObject.Position.X, playerObject.Position.Y);
        }

        public virtual TiledMapObjectLayer GetObjectLayer(string name)
        {
            return _map?.ObjectLayers.FirstOrDefault(layer => layer.Name == name);
        }

        public virtual int MapWidthInPixels => _map?.WidthInPixels ?? 0;
        public virtual int MapHeightInPixels => _map?.HeightInPixels ?? 0;
        public virtual int TileWidth => _map?.TileWidth ?? 0;
        public virtual int TileHeight => _map?.TileHeight ?? 0;
    }
}
