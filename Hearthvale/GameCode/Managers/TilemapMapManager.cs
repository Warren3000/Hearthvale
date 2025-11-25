using Hearthvale.GameCode.Managers;
using Microsoft.Xna.Framework;
using MonoGame.Extended.Tiled;
using MonoGameLibrary.Graphics;
using System;

namespace Hearthvale.GameCode.Managers
{
    /// <summary>
    /// Simple MapManager implementation that wraps a Tilemap for CameraManager compatibility
    /// </summary>
    public class TilemapMapManager : MapManager
    {
        private readonly Tilemap _tilemap;
        private readonly Vector2 _playerSpawnPoint;

        public TilemapMapManager(Tilemap tilemap, Vector2 playerSpawnPoint)
            : base() // Use the protected parameterless constructor
        {
            _tilemap = tilemap ?? throw new ArgumentNullException(nameof(tilemap));
            _playerSpawnPoint = playerSpawnPoint;
        }

        // Override properties to use our tilemap
        public override int MapWidthInPixels => _tilemap.Columns * (int)_tilemap.TileWidth;
        public override int MapHeightInPixels => _tilemap.Rows * (int)_tilemap.TileHeight;
        public override int TileWidth => (int)_tilemap.TileWidth;
        public override int TileHeight => (int)_tilemap.TileHeight;
        
        // Override RoomBounds to calculate from our tilemap
        public new Rectangle RoomBounds => new Rectangle(0, 0, MapWidthInPixels, MapHeightInPixels);

        public override void Update(GameTime gameTime)
        {
            // No update needed for static tilemap - don't call base
        }

        public override void Draw(Matrix transform)
        {
            // Drawing is handled separately in GameScene - don't call base
        }

        public override Vector2 GetPlayerSpawnPoint()
        {
            return _playerSpawnPoint;
        }

        public override TiledMapObjectLayer GetObjectLayer(string name)
        {
            // Not applicable for procedural tilemaps
            return null;
        }
    }
}
