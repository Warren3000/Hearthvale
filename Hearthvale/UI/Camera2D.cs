using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using System;

namespace Hearthvale.UI
{
    public class Camera2D
    {
        private Matrix _transform;
        private Vector2 _position;
        private Viewport _viewport;
        private float _zoom;
        private float _rotation;

        //screen shake variables
        private Vector2 _shakeOffset = Vector2.Zero;
        private float _shakeDuration = 0f;
        private float _shakeIntensity = 0f;

        public Camera2D(Viewport viewport)
        {
            _viewport = viewport;
            _zoom = 1f;
            _rotation = 0f;

        }


        public Matrix GetViewMatrix()
        {
            Vector2 finalPosition = _position + _shakeOffset;

            return Matrix.CreateTranslation(new Vector3(-finalPosition, 0)) *
                   Matrix.CreateRotationZ(_rotation) *
                   Matrix.CreateScale(new Vector3(_zoom, _zoom, 1)) *
                   Matrix.CreateTranslation(new Vector3(_viewport.Width / 2f, _viewport.Height / 2f, 0));
        }

        public void Follow(Vector2 target)
        {
            _position = target;
        }

        public void FollowSmooth(Vector2 target, float smoothing = 0.1f)
        {
            //smoothing should be between 0.05f (slow) and 0.2f (snappy). Lower = smoother.
            _position = Vector2.Lerp(_position, target, smoothing);
        }

        public void ClampToMap(int mapWidth, int mapHeight, int tileSize)
        {
            float mapWidthInPixels = mapWidth * tileSize;
            float mapHeightInPixels = mapHeight * tileSize;


            Vector2 halfScreenSize = new Vector2(_viewport.Width, _viewport.Height) / (2f * _zoom);

            float minX = halfScreenSize.X;
            float minY = halfScreenSize.Y;

            float maxX = mapWidthInPixels - halfScreenSize.X;
            float maxY = mapHeightInPixels - halfScreenSize.Y;

            // If map smaller than screen, lock to center
            if (maxX < minX)
            {
                minX = maxX = mapWidthInPixels / 2f;
            }
            if (maxY < minY)
            {
                minY = maxY = mapHeightInPixels / 2f;
            }

            _position.X = MathHelper.Clamp(_position.X, minX, maxX);
            _position.Y = MathHelper.Clamp(_position.Y, minY, maxY);

        }

        public void FollowWithMargin(Vector2 target, Rectangle screenMargin, float smoothing = 0.1f)
        {
            // Convert the screen margin rectangle from screen pixels to world units.
            RectangleF marginWorld = new RectangleF(
                _position.X - (_viewport.Width / 4 - screenMargin.X) / _zoom,
                _position.Y - (_viewport.Height / 4 - screenMargin.Y) / _zoom,
                screenMargin.Width / _zoom,
                screenMargin.Height / _zoom
            );

            Vector2 desiredPosition = _position;

            if (target.X < marginWorld.Left)
                desiredPosition.X -= marginWorld.Left - target.X;
            else if (target.X > marginWorld.Right)
                desiredPosition.X += target.X - marginWorld.Right;

            if (target.Y < marginWorld.Top)
                desiredPosition.Y -= marginWorld.Top - target.Y;
            else if (target.Y > marginWorld.Bottom)
                desiredPosition.Y += target.Y - marginWorld.Bottom;

            // Smoothly interpolate the camera position towards desired position
            _position = Vector2.Lerp(_position, desiredPosition, smoothing);
        }

        public void FollowWithDeadzone(Vector2 target, RectangleF deadzone)
        {
            // Convert camera center to world space
            Vector2 cameraTopLeft = _position - new Vector2(_viewport.Width / (2f * _zoom), _viewport.Height / (2f * _zoom));
            RectangleF worldDeadzone = new RectangleF(
                cameraTopLeft.X + deadzone.X,
                cameraTopLeft.Y + deadzone.Y,
                deadzone.Width,
                deadzone.Height
            );

            if (!worldDeadzone.Contains(target))
            {
                _position = target;
            }
        }

        public Vector2 Position => _position;

        public float Zoom
        {
            get => _zoom;
            set => _zoom = MathHelper.Clamp(value, 0.1f, 5f);
        }

        public float Rotation
        {
            get => _rotation;
            set => _rotation = value;
        }

        public void Shake(float duration, float intensity)
        {
            _shakeDuration = duration;
            _shakeIntensity = intensity;
        }


        public void Update(GameTime gameTime)
        {
            if (_shakeDuration > 0)
            {
                _shakeDuration -= (float)gameTime.ElapsedGameTime.TotalSeconds;
                float shakeAmount = _shakeIntensity * (_shakeDuration > 0 ? 1f : 0f);
                _shakeOffset = new Vector2(
                    (float)(Random.Shared.NextDouble() * 2 - 1) * shakeAmount,
                    (float)(Random.Shared.NextDouble() * 2 - 1) * shakeAmount
                );
            }
            else
            {
                _shakeOffset = Vector2.Zero;
            }
        }
    }
}

