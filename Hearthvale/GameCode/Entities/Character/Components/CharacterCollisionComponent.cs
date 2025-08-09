using Microsoft.Xna.Framework;
using MonoGame.Extended.Tiled;
using Hearthvale.GameCode.Managers;
using Hearthvale.GameCode.Utils;
using System.Collections.Generic;
using System.Linq;
using MonoGameLibrary.Graphics;

namespace Hearthvale.GameCode.Entities.Components
{
    public class CharacterCollisionComponent
    {
        private readonly Character _character;
        private Vector2 _knockbackVelocity;
        private float _knockbackTimer;
        private const float KnockbackDuration = 0.2f;
        private const float BounceDamping = 0.5f;

        public Tilemap Tilemap { get; set; }
        public bool IsKnockedBack => _knockbackTimer > 0;

        public CharacterCollisionComponent(Character character)
        {
            _character = character;
        }

        public void SetKnockback(Vector2 velocity)
        {
            _knockbackVelocity = velocity;
            _knockbackTimer = KnockbackDuration;
        }
        public Vector2 GetKnockbackVelocity()
        {
            return _knockbackVelocity;
        }

        public void UpdateKnockback(GameTime gameTime)
        {
            if (_knockbackTimer > 0)
            {
                float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;
                _knockbackTimer -= elapsed;
                Vector2 nextPosition = _character.Position + _knockbackVelocity * elapsed;

                if (ValidatePosition(nextPosition))
                {
                    if (!TryMoveWithWallSliding(nextPosition, elapsed))
                    {
                        _knockbackVelocity = Vector2.Zero;
                        _knockbackTimer = 0;
                    }
                }

                if (_knockbackTimer <= 0)
                {
                    _knockbackVelocity = Vector2.Zero;
                }
            }
        }

        private bool ValidatePosition(Vector2 position)
        {
            if (float.IsNaN(position.X) || float.IsNaN(position.Y))
            {
                System.Diagnostics.Debug.WriteLine($"❌ KNOCKBACK NaN detected");
                _knockbackVelocity = Vector2.Zero;
                _knockbackTimer = 0;
                return false;
            }
            return true;
        }

        private bool TryMoveWithWallSliding(Vector2 nextPosition, float elapsed)
        {
            Vector2 currentPos = _character.Position;
            Vector2 movement = nextPosition - currentPos;

            if (movement.LengthSquared() < 0.001f)
                return true;

            Rectangle nextBounds = GetBoundsAtPosition(nextPosition);
            if (!IsPositionBlocked(nextBounds))
            {
                _character.SetPosition(nextPosition);
                return true;
            }

            // Try wall sliding
            if (TrySlideHorizontally(nextPosition, currentPos) ||
                TrySlideVertically(nextPosition, currentPos))
            {
                return true;
            }

            // Apply bounce effect
            ApplyBounceEffect();
            return FindSafePosition(currentPos, nextPosition);
        }

        private bool TrySlideHorizontally(Vector2 nextPosition, Vector2 currentPos)
        {
            Vector2 horizontalTarget = new Vector2(nextPosition.X, currentPos.Y);
            Rectangle horizontalBounds = GetBoundsAtPosition(horizontalTarget);
            if (!IsPositionBlocked(horizontalBounds))
            {
                _character.SetPosition(horizontalTarget);
                if (_knockbackTimer > 0)
                {
                    _knockbackVelocity = new Vector2(_knockbackVelocity.X, -_knockbackVelocity.Y * BounceDamping);
                }
                return true;
            }
            return false;
        }

        private bool TrySlideVertically(Vector2 nextPosition, Vector2 currentPos)
        {
            Vector2 verticalTarget = new Vector2(currentPos.X, nextPosition.Y);
            Rectangle verticalBounds = GetBoundsAtPosition(verticalTarget);
            if (!IsPositionBlocked(verticalBounds))
            {
                _character.SetPosition(verticalTarget);
                if (_knockbackTimer > 0)
                {
                    _knockbackVelocity = new Vector2(-_knockbackVelocity.X * BounceDamping, _knockbackVelocity.Y);
                }
                return true;
            }
            return false;
        }

        private void ApplyBounceEffect()
        {
            if (_knockbackTimer > 0)
            {
                _knockbackVelocity = -_knockbackVelocity * BounceDamping;
                if (_knockbackVelocity.LengthSquared() < 10f)
                {
                    _knockbackVelocity = Vector2.Zero;
                    _knockbackTimer = 0;
                }
            }
        }

        private bool FindSafePosition(Vector2 currentPos, Vector2 targetPos)
        {
            Vector2 direction = targetPos - currentPos;

            for (float step = 0.1f; step <= 1.0f; step += 0.1f)
            {
                Vector2 testPos = currentPos + direction * (1.0f - step);
                Rectangle testBounds = GetBoundsAtPosition(testPos);

                if (!IsPositionBlocked(testBounds))
                {
                    _character.SetPosition(testPos);
                    return true;
                }
            }

            return false;
        }

        private Rectangle GetBoundsAtPosition(Vector2 position)
        {
            return new Rectangle(
                (int)position.X + 8,
                (int)position.Y + 16,
                (int)_character.Sprite.Width / 2,
                (int)_character.Sprite.Height / 2
            );
        }

        private bool IsPositionBlocked(Rectangle bounds)
        {
            return CheckWallCollision(bounds) || CheckObstacleCollision(bounds);
        }

        private bool CheckWallCollision(Rectangle bounds)
        {
            if (Tilemap == null || TilesetManager.Instance.WallTileset == null)
                return false;

            int leftTile = bounds.Left / (int)Tilemap.TileWidth;
            int rightTile = (bounds.Right - 1) / (int)Tilemap.TileWidth;
            int topTile = bounds.Top / (int)Tilemap.TileHeight;
            int bottomTile = (bounds.Bottom - 1) / (int)Tilemap.TileHeight;

            for (int col = leftTile; col <= rightTile; col++)
            {
                for (int row = topTile; row <= bottomTile; row++)
                {
                    if (IsValidTileCoordinate(col, row))
                    {
                        var tileTileset = Tilemap.GetTileset(col, row);
                        var tileId = Tilemap.GetTileId(col, row);
                        if (tileTileset == TilesetManager.Instance.WallTileset &&
                            AutotileMapper.IsWallTile(tileId))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private bool IsValidTileCoordinate(int col, int row)
        {
            return col >= 0 && col < Tilemap.Columns && row >= 0 && row < Tilemap.Rows;
        }

        private bool CheckObstacleCollision(Rectangle bounds)
        {
            var obstacles = _character.GetObstacleRectangles();
            if (obstacles == null) return false;

            return obstacles.Any(obstacle => bounds.Intersects(obstacle));
        }
    }
}