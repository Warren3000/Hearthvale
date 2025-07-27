using Hearthvale.GameCode.UI;
using Microsoft.Xna.Framework;

namespace Hearthvale.GameCode.Managers
{
    public class CameraManager
    {
        private readonly Camera2D _camera;

        public CameraManager(Camera2D camera)
        {
            _camera = camera;
        }

        public void UpdateCamera(Vector2 playerPosition, Point playerSpriteSize, Rectangle margin, MapManager mapManager, GameTime gameTime)
        {
            // Center of the player
            Vector2 playerCenter = playerPosition + new Vector2(playerSpriteSize.X / 2f, playerSpriteSize.Y / 2f);

            _camera.FollowWithMargin(playerCenter, margin, 0.1f);
            _camera.ClampToMap(mapManager.MapWidthInPixels, mapManager.MapHeightInPixels, mapManager.TileWidth);
            _camera.Update(gameTime);
        }

        public Matrix GetViewMatrix()
        {
            return _camera.GetViewMatrix();
        }

        public float Zoom
        {
            get => _camera.Zoom;
            set => _camera.Zoom = value;
        }

        public Vector2 Position => _camera.Position;
    }
}