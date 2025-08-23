using Hearthvale.GameCode.UI;
using Microsoft.Xna.Framework;
using System;

namespace Hearthvale.GameCode.Managers
{
    public class CameraManager
    {
        private static CameraManager _instance;
        public static CameraManager Instance => _instance ?? throw new InvalidOperationException("CameraManager not initialized. Call Initialize first.");

        private Camera2D _camera;
        private CombatEffectsManager _effectsManager;
        public Camera2D Camera2D => _camera;

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
            var matrix = Camera2D?.GetViewMatrix() ?? Matrix.Identity;

#if DEBUG
            // Add some debug output to see what's being returned
            if (DateTime.Now.Millisecond % 100 < 5) // Occasional debug output
            {
                //System.Diagnostics.Debug.WriteLine($"CameraManager returning matrix: {matrix}");
                //System.Diagnostics.Debug.WriteLine($"Camera position: {Camera2D?.Position ?? Vector2.Zero}");
            }
#endif

            return matrix;
        }

        public float Zoom
        {
            get => _camera.Zoom;
            set => _camera.Zoom = value;
        }

        public Vector2 Position
        {
            get => _camera.Position;
            set => _camera.Position = value;
        }
    }
}