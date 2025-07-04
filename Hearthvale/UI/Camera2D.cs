using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hearthvale.UI
{
    public class Camera2D
    {
        private Matrix _transform;
        private Vector2 _position;
        private Viewport _viewport;
        private float _zoom;
        private float _rotation;

        public Camera2D(Viewport viewport)
        {
            _viewport = viewport;
            _zoom = 1f;
            _rotation = 0f;
        }

        public Matrix GetViewMatrix()
        {
            return Matrix.CreateTranslation(new Vector3(-_position, 0)) *
                   Matrix.CreateRotationZ(_rotation) *
                   Matrix.CreateScale(new Vector3(_zoom, _zoom, 1)) *
                   Matrix.CreateTranslation(new Vector3(_viewport.Width * 0.5f, _viewport.Height * 0.5f, 0));
        }

        public void Follow(Vector2 target)
        {
            _position = target;
        }

        public void ClampToMap(int mapWidth, int mapHeight, int tileSize)
        {
            Vector2 halfScreen = new Vector2(_viewport.Width, _viewport.Height) / 2f;

            _position.X = MathHelper.Clamp(_position.X, halfScreen.X, mapWidth * tileSize - halfScreen.X);
            _position.Y = MathHelper.Clamp(_position.Y, halfScreen.Y, mapHeight * tileSize - halfScreen.Y);
        }

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

        public Vector2 Position => _position;
    }
}

