using Hearthvale.GameCode.UI;
using Hearthvale.GameCode.Utils;
using Microsoft.Xna.Framework;
using System;

namespace Hearthvale.GameCode.Managers
{
    /// <summary>
    /// Simple singleton camera manager that follows a target point with optional smoothing.
    /// Refactored to remove vertical lock and per-frame sprite bound dependence that caused jitter.
    /// </summary>
    public class CameraManager
    {
        private static CameraManager _instance;
        public static CameraManager Instance => _instance ?? throw new InvalidOperationException("CameraManager not initialized. Call Initialize first.");

        private readonly Camera2D _camera;
        public Camera2D Camera2D => _camera;

        // Default smoothing factor (0..1). 1 = snap, lower = smoother.
        private float _defaultSmoothing = 0.18f;

        private CameraManager(Camera2D camera)
        {
            _camera = camera ?? throw new ArgumentNullException(nameof(camera));
            Log.Info(LogArea.Camera, "Camera manager initialized");
        }

        /// <summary>
        /// Initialize singleton.
        /// </summary>
        public static void Initialize(Camera2D camera) => _instance = new CameraManager(camera);

        /// <summary>
        /// Update the camera position towards target. Uses simple exponential smoothing.
        /// Pass smoothing = 1 to snap directly.
        /// </summary>
    // mapWidthTiles / mapHeightTiles: number of tiles (NOT pixels)
    public void Update(Vector2 target, int mapWidthTiles, int mapHeightTiles, int tileSize, GameTime gameTime, float? smoothing = null)
        {
            float lerpFactor = MathHelper.Clamp(smoothing ?? _defaultSmoothing, 0f, 1f);

            // Smooth follow both axes (target supplied should already be a stable anchor like 32x32 sprite center)
            Vector2 newPos = lerpFactor >= 1f
                ? target
                : Vector2.Lerp(_camera.Position, target, lerpFactor);

            _camera.Position = newPos;

            // Clamp to map
            _camera.ClampToMap(mapWidthTiles, mapHeightTiles, tileSize);

            // Apply shake/effects
            _camera.Update(gameTime);
        }

        public Matrix GetViewMatrix() => _camera?.GetViewMatrix() ?? Matrix.Identity;

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

        /// <summary>
        /// Allows changing the default smoothing dynamically.
        /// </summary>
        public void SetDefaultSmoothing(float smoothing)
        {
            _defaultSmoothing = MathHelper.Clamp(smoothing, 0f, 1f);
            Log.Info(LogArea.Camera, $"Camera smoothing set to {_defaultSmoothing:0.00}");
        }
    }
}