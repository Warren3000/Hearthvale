using Hearthvale.GameCode.Entities.Components;
using Hearthvale.GameCode.Managers;
using Hearthvale.GameCode.Utils;
using Microsoft.Xna.Framework;
using MonoGameLibrary.Graphics;
using System;

namespace Hearthvale.GameCode.Entities.NPCs.Components
{
    /// <summary>
    /// Component that manages NPC movement, including AI behaviors like wandering and chasing
    /// </summary>
    public class NpcMovementComponent : CharacterMovementComponent
    {
        private readonly NPC _owner;
        private Vector2 _velocity;
        private float _directionChangeTimer;
        private float _idleTimer;
        private bool _isIdle;
        private readonly Random _random = new();
        public Rectangle Bounds;
        public bool IsIdle => _isIdle;

        // Knockback support
        private float _knockbackTimer = 0f;
        private const float KnockbackDuration = 0.2f; // seconds

        private int _spriteWidth;
        private int _spriteHeight;

        // AI chase support
        private Vector2? _chaseTarget = null;
        private float _chaseSpeed = 40f;

        public NpcMovementComponent(NPC owner, Vector2 startPosition, float speed, Rectangle bounds, int spriteWidth, int spriteHeight)
            : base(owner, startPosition, speed)
        {
            _owner = owner;
            Bounds = bounds;
            _spriteWidth = spriteWidth;
            _spriteHeight = spriteHeight;
        }

        /// <summary>
        /// Set a target position to chase (e.g., the player's position).
        /// If null, NPC will wander randomly.
        /// </summary>
        public void SetChaseTarget(Vector2? target, float chaseSpeed = 40f)
        {
            _chaseTarget = target;
            _chaseSpeed = chaseSpeed;
        }

        public void SetRandomDirection()
        {
            float angle = (float)(_random.NextDouble() * Math.PI * 2);
            _velocity = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * MovementSpeed;
            _directionChangeTimer = 2f + (float)_random.NextDouble() * 3f;
            _isIdle = false;
        }

        public void SetIdle()
        {
            // Only set idle if not in knockback
            if (_knockbackTimer <= 0f)
            {
                _velocity = Vector2.Zero;
                _isIdle = true;
                _idleTimer = 1f + (float)_random.NextDouble() * 2f;
            }
        }

        public void Update(float elapsed, Func<Vector2, bool> collisionCheck)
        {
            // Handle knockback first, as it's a forced movement
            if (_knockbackTimer > 0f)
            {
                _knockbackTimer -= elapsed;
                Vector2 nextPosition = Position + _velocity * elapsed;

                // During knockback, use wall sliding for smoother movement
                Vector2 finalPosition = TryMoveWithWallSliding(Position, nextPosition, elapsed);
                SetPosition(Vector2.Clamp(
                    finalPosition,
                    new Vector2(Bounds.Left, Bounds.Top),
                    new Vector2(Bounds.Right, Bounds.Bottom)
                ));

                if (_knockbackTimer <= 0f)
                {
                    _velocity = Vector2.Zero; // Reset velocity only when timer expires
                    SetIdle(); // Transition to idle state after knockback
                }
                return; // IMPORTANT: Skip normal AI movement during knockback
            }

            // AI chase logic
            if (_chaseTarget.HasValue)
            {
                Vector2 direction = _chaseTarget.Value - Position;
                if (direction.LengthSquared() > 0.01f)
                {
                    direction.Normalize();
                    _velocity = direction * _chaseSpeed;
                    _isIdle = false;
                }
                else
                {
                    _velocity = Vector2.Zero;
                    _isIdle = true;
                }
            }

            // Normal AI movement logic (idle/wander)
            if (_isIdle)
            {
                _idleTimer -= elapsed;
                if (_idleTimer <= 0)
                    SetRandomDirection();
            }
            else
            {
                Vector2 nextPosition = Position + _velocity * elapsed;

                // Use wall sliding for smoother AI movement
                Vector2 finalPosition = TryMoveWithWallSliding(Position, nextPosition, elapsed);

                if (collisionCheck(finalPosition))
                {
                    SetIdle();
                    return;
                }

                SetPosition(Vector2.Clamp(
                    finalPosition,
                    new Vector2(Bounds.Left, Bounds.Top),
                    new Vector2(Bounds.Right, Bounds.Bottom)
                ));

                _directionChangeTimer -= elapsed;
                if (_directionChangeTimer <= 0 && !_chaseTarget.HasValue)
                    SetIdle();
            }
        }

        /// <summary>
        /// Attempts to move with wall sliding support
        /// </summary>
        private Vector2 TryMoveWithWallSliding(Vector2 currentPos, Vector2 targetPos, float elapsed)
        {
            Vector2 movement = targetPos - currentPos;

            // If no movement, return current position
            if (movement.LengthSquared() < 0.001f)
                return currentPos;

            // Try full movement first
            if (!IsWall(targetPos))
                return targetPos;

            // If full movement blocked, try sliding along walls

            // Try horizontal movement only
            Vector2 horizontalTarget = new Vector2(targetPos.X, currentPos.Y);
            if (!IsWall(horizontalTarget))
                return horizontalTarget;

            // Try vertical movement only
            Vector2 verticalTarget = new Vector2(currentPos.X, targetPos.Y);
            if (!IsWall(verticalTarget))
                return verticalTarget;

            // If both individual axes are blocked, stay at current position
            return currentPos;
        }

        // Helper to check for wall collision
        private bool IsWall(Vector2 pos)
        {
            Tilemap _tilemap = TilesetManager.Instance.Tilemap;
            if (_tilemap == null) return false;

            Rectangle candidateBounds = new Rectangle(
                (int)pos.X + 8,
                (int)pos.Y + 16,
                _spriteWidth / 2,
                _spriteHeight / 2
            );

            int leftTile = candidateBounds.Left / (int)_tilemap.TileWidth;
            int rightTile = (candidateBounds.Right - 1) / (int)_tilemap.TileWidth;
            int topTile = candidateBounds.Top / (int)_tilemap.TileHeight;
            int bottomTile = (candidateBounds.Bottom - 1) / (int)_tilemap.TileHeight;

            var wallTileset = TilesetManager.Instance.WallTileset;

            for (int col = leftTile; col <= rightTile; col++)
            {
                for (int row = topTile; row <= bottomTile; row++)
                {
                    if (col >= 0 && col < _tilemap.Columns && row >= 0 && row < _tilemap.Rows)
                    {
                        var tileTileset = _tilemap.GetTileset(col, row);
                        var tileId = _tilemap.GetTileId(col, row);
                        // Check if the tile at this position belongs to the wall tileset.
                        if (tileTileset == wallTileset && AutotileMapper.IsWallTile(tileId))
                            return true;
                    }
                }
            }
            return false;
        }

        public Vector2 GetVelocity() => _velocity;

        public void SetVelocity(Vector2 v)
        {
            _velocity = v;
            _knockbackTimer = KnockbackDuration;
            _isIdle = false;
        }
    }
}