using Hearthvale.GameCode.UI;
using Hearthvale.GameCode.Utils;
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
        
        // Fixed camera Y position
        private float _fixedY = float.MinValue;
        
        // Significant vertical movement threshold before updating camera Y
        private const float VERTICAL_MOVEMENT_THRESHOLD = 32.0f;
        
        // Smoothing for horizontal movement only
        private const float HORIZONTAL_SMOOTHING = 0.2f;
        
        // Debug variables
        private int _framesWithSameY = 0;
        private bool _debugLoggingEnabled = true;

        private CameraManager(Camera2D camera)
        {
            _camera = camera;
            Log.Info(LogArea.Camera, "Camera manager initialized with vertical position lock");
        }

        /// <summary>
        /// Initializes the singleton instance. Call this once at startup.
        /// </summary>
        public static void Initialize(Camera2D camera)
        {
            _instance = new CameraManager(camera);
        }

        /// <summary>
        /// Updates the camera to follow the player with a fixed Y position to eliminate animation bouncing
        /// </summary>
        public void UpdateCamera(Vector2 playerPosition, Point playerSpriteSize, Rectangle margin, MapManager mapManager, CombatEffectsManager effectsManager, GameTime gameTime)
        {
            _effectsManager = effectsManager;
            
            // Initialize fixed Y position if not set
            if (_fixedY == float.MinValue)
            {
                _fixedY = playerPosition.Y + playerSpriteSize.Y / 2f;
                Log.Info(LogArea.Camera, $"Camera Y position initialized to: {_fixedY}");
            }
            
            // Calculate player center X for responsive horizontal movement
            float playerCenterX = playerPosition.X + playerSpriteSize.X / 2f;
            
            // Check if player has moved significantly in the Y direction
            float playerCenterY = playerPosition.Y + playerSpriteSize.Y / 2f;
            float yDifference = Math.Abs(playerCenterY - _fixedY);
            
            if (yDifference > VERTICAL_MOVEMENT_THRESHOLD)
            {
                // Player has moved significantly up or down (like climbing stairs)
                // Update fixed Y position to follow player
                _fixedY = playerCenterY;
                Log.Info(LogArea.Camera, $"Significant vertical movement detected ({yDifference}px). Camera Y updated to {_fixedY}");
                _framesWithSameY = 0;
            }
            else
            {
                // Count frames with stable Y for debugging
                _framesWithSameY++;
                
                // Log stable Y position every 300 frames if debug enabled
                if (_debugLoggingEnabled && _framesWithSameY % 300 == 0)
                {
                    Log.Info(LogArea.Camera, $"Camera Y position stable at {_fixedY} for {_framesWithSameY} frames");
                }
            }
            
            // Create target position with current X and fixed Y
            Vector2 targetPosition = new Vector2(playerCenterX, _fixedY);
            
            // Only apply smoothing to X movement for responsive horizontal control
            float smoothedX = MathHelper.Lerp(_camera.Position.X, targetPosition.X, HORIZONTAL_SMOOTHING);
            
            // Set the camera position with smoothed X and locked Y
            _camera.Position = new Vector2(smoothedX, _fixedY);
            
            // Handle map boundaries
            _camera.ClampToMap(mapManager.MapWidthInPixels, mapManager.MapHeightInPixels, mapManager.TileWidth);
            
            // Update camera effects like screen shake
            _camera.Update(gameTime);
        }

        public Matrix GetViewMatrix()
        {
            var matrix = Camera2D?.GetViewMatrix() ?? Matrix.Identity;
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
            set
            {
                // When explicitly setting position, update fixed Y
                _fixedY = value.Y;
                _camera.Position = value;
                Log.Info(LogArea.Camera, $"Camera position explicitly set to: {value}");
            }
        }
        
        /// <summary>
        /// Resets tracking when changing scenes or teleporting
        /// </summary>
        public void ResetSmoothing(Vector2 newPosition)
        {
            _fixedY = newPosition.Y;
            _camera.Position = newPosition;
            _framesWithSameY = 0;
            Log.Info(LogArea.Camera, $"Camera tracking reset to position: {newPosition}");
        }
        
        /// <summary>
        /// Enables or disables debug logging
        /// </summary>
        public void SetDebugLogging(bool enabled)
        {
            _debugLoggingEnabled = enabled;
            Log.Info(LogArea.Camera, $"Camera debug logging {(enabled ? "enabled" : "disabled")}");
        }
    }
}