using Hearthvale.GameCode.Utils;
using Microsoft.Xna.Framework;
using MonoGameLibrary.Graphics;
using System;

namespace Hearthvale.GameCode.Entities.Components
{
    /// <summary>
    /// Manages character position, movement, and bounds calculations
    /// </summary>
    public class CharacterMovementComponent
    {
        private readonly Character _character;
        private Vector2 _position;
        private float _movementSpeed = 2f;
        private bool _facingRight = true;
        private Vector2 _lastMovementVector = Vector2.Zero;
        private CardinalDirection _facingDirection = CardinalDirection.South;

        public Vector2 Position => _position;
        public float MovementSpeed => _movementSpeed;
        public bool FacingRight
        {
            get => _facingRight;
            set => _facingRight = value;
        }

        public CardinalDirection FacingDirection
        {
            get => _facingDirection;
            set => _facingDirection = value;
        }

        public Vector2 LastMovementVector
        {
            get => _lastMovementVector;
            set
            {
                _lastMovementVector = value;
                if (value != Vector2.Zero)
                {
                    // Update the facing direction
                    _facingDirection = value.ToCardinalDirection();
                    _facingRight = (_facingDirection == CardinalDirection.East);
                }
            }
        }

        public CharacterMovementComponent(Character character, Vector2 initialPosition, float movementSpeed = 2f)
        {
            _character = character;
            _position = initialPosition;
            _movementSpeed = movementSpeed;
        }

        /// <summary>
        /// Moves in a cardinal direction
        /// </summary>
        public void MoveInDirection(CardinalDirection direction, float deltaTime)
        {
            _facingDirection = direction;
            _facingRight = (direction == CardinalDirection.East);

            Vector2 movement = direction.ToVector() * _movementSpeed * deltaTime;
            _position += movement;
            _lastMovementVector = direction.ToVector();

            UpdateSpritePosition();
        }

        public void SetPosition(Vector2 position)
        {
            _position = position;
            UpdateSpritePosition();
        }

        public void SetMovementSpeed(float speed)
        {
            _movementSpeed = speed;
        }

        public void ClampToBounds(Rectangle bounds)
        {
            if (_character.Sprite == null) return;

            float clampedX = MathHelper.Clamp(Position.X, bounds.Left, bounds.Right - _character.Sprite.Width);
            float clampedY = MathHelper.Clamp(Position.Y, bounds.Top, bounds.Bottom - _character.Sprite.Height);
            SetPosition(new Vector2(clampedX, clampedY));
        }

        public Rectangle CalculateBounds()
        {
            return new Rectangle(
                (int)_position.X + 8,
                (int)_position.Y + 16,
                (int)_character.Sprite?.Width / 2,
                (int)_character.Sprite?.Height / 2
            );
        }

        private void UpdateSpritePosition()
        {
            if (_character.Sprite != null)
            {
                _character.Sprite.Position = _position;
            }
        }
    }
}