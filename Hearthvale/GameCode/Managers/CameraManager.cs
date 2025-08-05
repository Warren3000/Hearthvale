using Hearthvale.GameCode.UI;
using Microsoft.Xna.Framework;
using System;

namespace Hearthvale.GameCode.Managers
{
    public class CameraManager
    {
        private static CameraManager _instance;
        public static CameraManager Instance => _instance ?? throw new InvalidOperationException("CameraManager not initialized. Call Initialize first.");

        private readonly Camera2D _camera;
        private CombatEffectsManager _effectsManager;

        private CameraManager(Camera2D camera)
        {
            _camera = camera;
        }

        /// <summary>
        /// Initializes the singleton instance. Call this once at startup.
        /// </summary>
        public static void Initialize(Camera2D camera)
        {
            _instance = new CameraManager(camera);
        }

        public void UpdateCamera(Vector2 playerPosition, Point playerSpriteSize, Rectangle margin, MapManager mapManager, CombatEffectsManager effectsManager, GameTime gameTime)
        {
            _effectsManager = effectsManager;
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