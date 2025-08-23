using Hearthvale.GameCode.Entities.NPCs;
using Hearthvale.GameCode.Managers;
using Hearthvale.GameCode.Utils;
using Microsoft.Xna.Framework;
using MonoGameLibrary.Graphics;
using System.Collections.Generic;
using System.Linq;

namespace Hearthvale.GameCode.Entities.Components
{
    public class CharacterCollisionComponent
    {
        private readonly Character _character;
        private Vector2 _knockbackVelocity;
        private float _knockbackTimer;
        private const float KnockbackDuration = 0.2f;
        private const float BounceDamping = 0.5f;
        private IEnumerable<Rectangle> _currentObstacles;
        private IEnumerable<NPC> _currentNpcs;

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
        public void UpdateObstacles(IEnumerable<Rectangle> obstacleRects, IEnumerable<NPC> npcs)
        {
            _currentObstacles = obstacleRects;
            _currentNpcs = npcs;
        }
        public void UpdateKnockback(GameTime gameTime)
        {
            if (_knockbackTimer > 0)
            {
                float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;
                _knockbackTimer -= elapsed;

                // Apply velocity damping over time for more natural feel
                float dampingFactor = 1.0f - (elapsed * 2.0f); // Adjust multiplier as needed
                _knockbackVelocity *= MathHelper.Clamp(dampingFactor, 0.1f, 1.0f);

                Vector2 nextPosition = _character.Position + _knockbackVelocity * elapsed;

                if (ValidatePosition(nextPosition))
                {
                    if (!TryMoveWithWallSliding(nextPosition))
                    {
                        _knockbackVelocity = Vector2.Zero;
                        _knockbackTimer = 0;
                    }
                }

                if (_knockbackTimer <= 0)
                {
                    _knockbackVelocity = Vector2.Zero;
                    _knockbackTimer = 0;
                }
            }
        }
        // In ApplyBounceEffect method, add this check:
        private void ApplyBounceEffect()
        {
            if (_knockbackTimer > 0)
            {
                _knockbackVelocity = -_knockbackVelocity * BounceDamping;

                // Stop bouncing if velocity is too small
                if (_knockbackVelocity.LengthSquared() < 25f) // Increased from 10f
                {
                    _knockbackVelocity = Vector2.Zero;
                    _knockbackTimer = 0;
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

        public bool TryMove(Vector2 nextPosition, IEnumerable<Character> otherCharacters)
        {
            // Use Bounds for other characters instead of GetTightSpriteBounds
            var originalObstacles = _character.GetObstacleRectangles();
            _character.SetObstacleRectangles(otherCharacters.Select(c => c.Bounds));

            bool success = TryMoveWithWallSliding(nextPosition);

            // Restore the original obstacles
            _character.SetObstacleRectangles(originalObstacles);

            return success;
        }

        private bool TryMoveWithWallSliding(Vector2 nextPosition)
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

            // Apply bounce effect if being knocked back
            if (IsKnockedBack)
            {
                ApplyBounceEffect();
            }
            
            return FindSafePosition(currentPos, nextPosition);
        }

        private bool TrySlideHorizontally(Vector2 nextPosition, Vector2 currentPos)
        {
            Vector2 horizontalTarget = new(nextPosition.X, currentPos.Y);
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
        public IEnumerable<Rectangle> GetObstacleRectangles()
        {
            var obstacles = new List<Rectangle>();

            // Add static obstacles
            if (_currentObstacles != null)
            {
                obstacles.AddRange(_currentObstacles);
            }

            // Add NPC bounds (except defeated ones)
            if (_currentNpcs != null)
            {
                foreach (var npc in _currentNpcs.Where(n => !n.IsDefeated))
                {
                    obstacles.Add(npc.Bounds);
                }
            }

            return obstacles;
        }
        private bool FindSafePosition(Vector2 currentPos, Vector2 targetPos)
        {
            Vector2 direction = targetPos - currentPos;
            
            // Try more granular steps for finer movement
            for (float step = 0.05f; step <= 1.0f; step += 0.05f)
            {
                Vector2 testPos = currentPos + direction * (1.0f - step);
                Rectangle testBounds = GetBoundsAtPosition(testPos);

                if (!IsPositionBlocked(testBounds))
                {
                    _character.SetPosition(testPos);
                    return true;
                }
            }
            
            // If normal steps fail, try micro-nudges in cardinal directions
            float nudgeDistance = 1.0f;
            Vector2[] nudgeDirections = new Vector2[] 
            {
                new Vector2(nudgeDistance, 0),       // Right
                new Vector2(-nudgeDistance, 0),      // Left
                new Vector2(0, nudgeDistance),       // Down
                new Vector2(0, -nudgeDistance),      // Up
                new Vector2(nudgeDistance, nudgeDistance),    // Down-Right
                new Vector2(-nudgeDistance, nudgeDistance),   // Down-Left
                new Vector2(nudgeDistance, -nudgeDistance),   // Up-Right
                new Vector2(-nudgeDistance, -nudgeDistance)   // Up-Left
            };
            
            foreach (var nudge in nudgeDirections)
            {
                Vector2 nudgedPos = currentPos + nudge;
                Rectangle nudgedBounds = GetBoundsAtPosition(nudgedPos);
                
                if (!IsPositionBlocked(nudgedBounds))
                {
                    _character.SetPosition(nudgedPos);
                    return true;
                }
            }

            return false;
        }

        public Rectangle GetBoundsAtPosition(Vector2 position)
        {
            // Use the character's Bounds property directly
            Rectangle currentBounds = _character.Bounds;
            
            // Calculate the offset between logical position and bounds
            int offsetX = currentBounds.Left - (int)_character.Position.X;
            int offsetY = currentBounds.Top - (int)_character.Position.Y;
            
            // Apply the same offsets to the new position
            return new Rectangle(
                (int)position.X + offsetX,
                (int)position.Y + offsetY,
                currentBounds.Width,
                currentBounds.Height
            );
        }

        private bool IsPositionBlocked(Rectangle bounds)
        {
            return CheckWallCollision(bounds) || CheckObstacleCollision(bounds);
        }

        public bool CheckWallCollision(Rectangle bounds)
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