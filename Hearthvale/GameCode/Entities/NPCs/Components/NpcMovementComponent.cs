using Hearthvale.GameCode.Entities.Components;
using Hearthvale.GameCode.Managers;
using Hearthvale.GameCode.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
        private const float KnockbackDuration = 0.2f;
        private const float BounceDamping = 0.5f;

        // AI chase support
        private Vector2? _chaseTarget = null;
        private float _chaseSpeed = 40f;
        
        // Performance optimization - cache collision checks
        private int _collisionCacheFrame = -1;
        private Vector2 _lastTestedPosition;
        private bool _lastCollisionResult;

        // Track stuck state to help with hallway navigation
        private int _stuckFrameCount = 0;
        private Vector2 _lastStuckPosition;
        private const int STUCK_THRESHOLD = 12; // Reduced from 15
        
        // Wall avoidance parameters
        private const float WALL_BUFFER = 3f; // Small buffer to keep away from walls
        private Vector2 _lastGoodPosition; // Last known good position
        
        // Expose chasing state for animation
        public bool IsChasing => _chaseTarget.HasValue;

        // Public properties to expose internal state for debugging
        public bool IsStuck => _stuckFrameCount >= STUCK_THRESHOLD;

        public NpcMovementComponent(NPC owner, Vector2 startPosition, float speed, Rectangle bounds, int spriteWidth, int spriteHeight)
            : base(owner, startPosition, speed)
        {
            _owner = owner;
            Bounds = bounds;
            _lastGoodPosition = startPosition; // Initialize with starting position
        }

        public void SetChaseTarget(Vector2? target, float chaseSpeed = 40f)
        {
            _chaseTarget = target;
            _chaseSpeed = MathF.Min(chaseSpeed, MovementSpeed);
        }

        public void SetRandomDirection()
        {
            // Use a bias to avoid choosing directions that lead into walls
            for (int i = 0; i < 8; i++) // Try up to 8 directions
            {
                float angle = (float)(_random.NextDouble() * Math.PI * 2);
                Vector2 testVelocity = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * MovementSpeed;
                Vector2 testPos = Position + testVelocity * 0.5f; // Look ahead a bit
                
                // If this direction doesn't immediately hit a wall, use it
                if (!IsCollision(testPos, null))
                {
                    _velocity = testVelocity;
                    _directionChangeTimer = 1.5f + (float)_random.NextDouble() * 2f;
                    _isIdle = false;
                    return;
                }
            }
            
            // If all directions are blocked, just set idle
            SetIdle();
        }

        public void SetIdle()
        {
            // Only set idle if not in knockback
            if (_knockbackTimer <= 0f)
            {
                _velocity = Vector2.Zero;
                _isIdle = true;
                _idleTimer = 0.8f + (float)_random.NextDouble() * 1.2f;
                
                // Remember this as a good position when idle
                if (!IsCollision(Position, null))
                {
                    _lastGoodPosition = Position;
                }
            }
        }

        public void Update(float elapsed, Func<Vector2, bool> collisionCheck)
        {
            _frameCounter++;
            // Reset collision cache
            _collisionCacheFrame = Environment.TickCount;
            
            // Handle knockback first
            if (_knockbackTimer > 0f)
            {
                _knockbackTimer -= elapsed;
                Vector2 nextPosition = Position + _velocity * elapsed;

                if (!float.IsNaN(nextPosition.X) && !float.IsNaN(nextPosition.Y))
                {
                    HandleMovementWithCollision(nextPosition, elapsed, collisionCheck);
                    // Reset stuck counter during knockback
                    _stuckFrameCount = 0;
                }

                if (_knockbackTimer <= 0f)
                {
                    _velocity = Vector2.Zero;
                    SetIdle();
                }
                return;
            }

            // AI chase logic
            if (_chaseTarget.HasValue)
            {
                Vector2 direction = _chaseTarget.Value - Position;
                float distanceSq = direction.LengthSquared();
                
                // Only move if we're not extremely close already
                if (distanceSq > 4f) // Small threshold to prevent jittering
                {
                    direction.Normalize();
                    
                    // NEW: Do multiple collision checks along path to target
                    bool pathBlocked = false;
                    
                    // Check at 25%, 50%, and 75% of the way to target
                    float checkDistance = MathF.Min(distanceSq, 32f);
                    for (float fraction = 0.25f; fraction <= 1.0f; fraction += 0.25f)
                    {
                        Vector2 checkPoint = Position + direction * (checkDistance * fraction);
                        if (IsWallCollision(checkPoint))
                        {
                            pathBlocked = true;
                            break;
                        }
                    }
                    
                    if (pathBlocked)
                    {
                        // If direct path hits a wall, try to find a better approach angle
                        direction = FindBestApproachDirection(direction, elapsed, collisionCheck);
                        
                        // IMPORTANT: Even after finding a new direction, it might still lead into a wall
                        // Double-check our new direction and slow down if needed
                        Vector2 checkAhead = Position + direction * MovementSpeed * elapsed * 2;
                        if (IsWallCollision(checkAhead))
                        {
                            // If we're still heading into a wall, greatly reduce speed
                            _velocity = direction * (MovementSpeed * 0.3f);
                        }
                        else
                        {
                            // New direction looks good, proceed at normal speed
                            _velocity = direction * MathF.Min(_chaseSpeed, MovementSpeed);
                        }
                    }
                    else
                    {
                        // Keep chase speed consistent and normalized
                        float speed = MathF.Max(0f, MathF.Min(_chaseSpeed, MovementSpeed));
                        _velocity = direction * speed;
                    }
                    
                    _isIdle = false;
                }
                else
                {
                    _velocity = Vector2.Zero;
                    _isIdle = true;
                    _stuckFrameCount = 0; // Reset stuck counter when reaching target
                    
                    // Remember this as a good position
                    _lastGoodPosition = Position;
                }
            }

            // Normal AI movement logic (idle/wander)
            if (_isIdle)
            {
                _idleTimer -= elapsed;
                if (_idleTimer <= 0)
                    SetRandomDirection();
                _stuckFrameCount = 0; // Reset stuck counter while idle
            }
            else
            {
                Vector2 nextPosition = Position + _velocity * elapsed;

                if (float.IsNaN(nextPosition.X) || float.IsNaN(nextPosition.Y))
                {
                    // Invalid position detected
                    if (!_chaseTarget.HasValue)
                        SetIdle();
                    _stuckFrameCount = 0;
                    return;
                }

                Vector2 oldPosition = Position;
                bool moved = HandleMovementWithCollision(nextPosition, elapsed, collisionCheck);
                
                // Check if we're getting stuck (minimal movement)
                if (Vector2.DistanceSquared(oldPosition, Position) < 0.1f)
                {
                    if (_stuckFrameCount == 0)
                        _lastStuckPosition = Position;
                        
                    _stuckFrameCount++;
                    
                    // If stuck for several frames, try to escape
                    if (_stuckFrameCount >= STUCK_THRESHOLD)
                    {
                        // Try to find a way out
                        AttemptHallwayEscape(elapsed, collisionCheck);
                    }
                }
                else
                {
                    // If we moved successfully and not colliding, remember this position
                    if (!IsWallCollision(Position))
                    {
                        _lastGoodPosition = Position;
                        _stuckFrameCount = 0;
                    }
                    else
                    {
                        // We're moving but in contact with a wall - increment stuck count more slowly
                        _stuckFrameCount = Math.Min(_stuckFrameCount + 1, STUCK_THRESHOLD - 2);
                    }
                }
                
                // If we couldn't move and not trying to chase
                if (!moved && !_chaseTarget.HasValue)
                {
                    SetIdle();
                }
                
                _directionChangeTimer -= elapsed;
                if (_directionChangeTimer <= 0 && !_chaseTarget.HasValue)
                {
                    SetIdle();
                }
            }
        }

        /// <summary>
        /// Finds the best direction to approach a target when the direct path is blocked
        /// </summary>
        private Vector2 FindBestApproachDirection(Vector2 directDirection, float elapsed, Func<Vector2, bool> collisionCheck)
        {
            // Try several angles to find a clear path
            Vector2 bestDirection = directDirection;
            float bestDistance = float.MaxValue;
            bool foundClearPath = false;
            
            // Try 16 different angles
            for (int i = 0; i < 16; i++)
            {
                float angle = (float)(i * Math.PI / 8); // 16 directions
                Vector2 testDirection = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
                
                // Skip directions that are too different from the direct path
                if (Vector2.Dot(testDirection, directDirection) < 0.4f) 
                    continue;
                
                // Test this direction
                Vector2 testPos = Position + testDirection * MovementSpeed * elapsed * 5; // Look ahead
                
                if (!IsCollision(testPos, collisionCheck))
                {
                    // Found a clear path - check how far it is from target
                    if (_chaseTarget.HasValue)
                    {
                        float distToTarget = Vector2.DistanceSquared(testPos, _chaseTarget.Value);
                        if (distToTarget < bestDistance)
                        {
                            bestDistance = distToTarget;
                            bestDirection = testDirection;
                            foundClearPath = true;
                        }
                    }
                    else
                    {
                        return testDirection; // Just use first clear path if no target
                    }
                }
            }
            
            // If we found a clear path, use it
            if (foundClearPath)
                return bestDirection;
                
            // If no clear paths, try to move away from walls
            return CalculateWallRepulsion(directDirection);
        }
        
        /// <summary>
        /// Calculates a direction that moves away from nearby walls
        /// </summary>
        private Vector2 CalculateWallRepulsion(Vector2 desiredDirection)
        {
            Vector2 repulsion = Vector2.Zero;
            float wallSenseDistance = 16f;
            
            // Check in 8 directions for walls
            for (int i = 0; i < 8; i++)
            {
                float angle = i * MathF.PI / 4;
                Vector2 checkDir = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
                Vector2 checkPos = Position + checkDir * wallSenseDistance;
                
                if (IsWallCollision(checkPos))
                {
                    // Wall detected - add repulsion force
                    float strength = 1.0f / Math.Max(1f, Vector2.Distance(Position, checkPos));
                    repulsion -= checkDir * strength * 2f;
                }
            }
            
            // Combine repulsion with desired direction
            if (repulsion != Vector2.Zero)
            {
                repulsion.Normalize();
                Vector2 combined = desiredDirection + repulsion * 1.5f;
                
                if (combined.LengthSquared() > 0.0001f)
                {
                    combined.Normalize();
                    return combined;
                }
            }
            
            return desiredDirection;
        }

        /// <summary>
        /// Attempts to find a way out when stuck in hallways
        /// </summary>
        private void AttemptHallwayEscape(float elapsed, Func<Vector2, bool> collisionCheck)
        {
            // First, try returning to last known good position if it's not too far
            if (Vector2.DistanceSquared(Position, _lastGoodPosition) > 1f && 
                Vector2.DistanceSquared(Position, _lastGoodPosition) < 400f)
            {
                // Calculate direction to last good position
                Vector2 toLastGood = _lastGoodPosition - Position;
                if (toLastGood.LengthSquared() > 0.0001f)
                {
                    toLastGood.Normalize();
                    _velocity = toLastGood * MovementSpeed * 0.5f; // Reduced speed for careful movement
                    HandleMovementWithCollision(Position + _velocity * elapsed, elapsed, collisionCheck);
                    _stuckFrameCount = Math.Max(0, _stuckFrameCount - 2);
                    return;
                }
            }
            
            // If we're stuck for too long (over 30 frames), try a more dr  astic escape
            if (_stuckFrameCount > 30)
            {
                // NEW: Try random teleportation within a small radius to get unstuck
                // This is a last resort when all other methods fail
                for (int attempts = 0; attempts < 8; attempts++)
                {
                    float angle = (float)(_random.NextDouble() * Math.PI * 2);
                    float distance = _random.Next(8, 24);
                    Vector2 escapePos = Position + new Vector2(
                        MathF.Cos(angle) * distance,
                        MathF.Sin(angle) * distance
                    );
                    
                    // Only teleport if the new position is clear
                    if (!IsWallCollision(escapePos) && (collisionCheck == null || !collisionCheck(escapePos)))
                    {
                        SetPosition(escapePos);
                        _stuckFrameCount = 0;
                        _lastGoodPosition = escapePos;
                        return;
                    }
                }
            }
            
            // Rest of method as before...
        }

        /// <summary>
        /// Simplified collision handling with wall sliding and safety checks
        /// </summary>
        private bool HandleMovementWithCollision(Vector2 targetPos, float elapsed, Func<Vector2, bool> collisionCheck)
        {
            // Validate target position
            if (float.IsNaN(targetPos.X) || float.IsNaN(targetPos.Y))
                return false;
                
            // Check for room boundaries first
            targetPos = Vector2.Clamp(targetPos, 
                new Vector2(Bounds.Left + WALL_BUFFER, Bounds.Top + WALL_BUFFER),
                new Vector2(Bounds.Right - WALL_BUFFER, Bounds.Bottom - WALL_BUFFER));
            
            // Try moving directly first
            if (!IsCollision(targetPos, collisionCheck))
            {
                SetPosition(targetPos);
                return true;
            }
            
            // Try horizontal movement with a small vertical component
            Vector2 horizontalTarget = new Vector2(targetPos.X, Position.Y);
            
            // Add slight vertical movement away from walls
            if (!IsCollision(horizontalTarget, collisionCheck))
            {
                SetPosition(horizontalTarget);
                if (_knockbackTimer > 0f)
                    _velocity = new Vector2(_velocity.X, -_velocity.Y * BounceDamping);
                return true;
            }
            
            // Try vertical movement with a small horizontal component
            Vector2 verticalTarget = new Vector2(Position.X, targetPos.Y);
            if (!IsCollision(verticalTarget, collisionCheck))
            {
                SetPosition(verticalTarget);
                if (_knockbackTimer > 0f)
                    _velocity = new Vector2(-_velocity.X * BounceDamping, _velocity.Y);
                return true;
            }
            
            // Apply bounce effect if in knockback
            if (_knockbackTimer > 0f)
            {
                _velocity = -_velocity * BounceDamping;
                if (_velocity.LengthSquared() < 10f)
                {
                    _velocity = Vector2.Zero;
                    _knockbackTimer = 0;
                }
            }
            
            // Try diagonal movements for smoother navigation
            float diagonalDist = MathF.Min(MathF.Abs(targetPos.X - Position.X), 
                                          MathF.Abs(targetPos.Y - Position.Y)) * 0.7f;
                                          
            Vector2[] diagonals = new[] {
                new Vector2(Position.X + diagonalDist, Position.Y + diagonalDist),
                new Vector2(Position.X + diagonalDist, Position.Y - diagonalDist),
                new Vector2(Position.X - diagonalDist, Position.Y + diagonalDist),
                new Vector2(Position.X - diagonalDist, Position.Y - diagonalDist)
            };
            
            foreach (var diagonalPos in diagonals)
            {
                if (!IsCollision(diagonalPos, collisionCheck))
                {
                    SetPosition(diagonalPos);
                    return true;
                }
            }
            
            // Couldn't move in any direction
            return false;
        }

        /// <summary>
        /// Optimized collision check with caching and orientation awareness
        /// </summary>
        private bool IsCollision(Vector2 position, Func<Vector2, bool> collisionCheck)
        {
            // Use cached result if checking the same position again in the same frame
            if (position == _lastTestedPosition)
                return _lastCollisionResult;
            
            // Check tile collisions first (faster)
            if (IsWallCollision(position))
            {
                CacheCollisionResult(position, true);
                return true;
            }
            
            // Then check dynamic obstacles (other NPCs, player)
            bool isBlocked = collisionCheck?.Invoke(position) ?? false;
            CacheCollisionResult(position, isBlocked);
            return isBlocked;
        }
        
        /// <summary>
        /// Cache collision test result to avoid redundant checks
        /// </summary>
        private void CacheCollisionResult(Vector2 position, bool result)
        {
            _lastTestedPosition = position;
            _lastCollisionResult = result;
        }

        /// <summary>
        /// Improved wall collision check with proper orientation awareness
        /// </summary>
        private bool IsWallCollision(Vector2 position)
        {
            var tilemap = TilesetManager.Instance.Tilemap;
            if (tilemap == null) return false;

            // Use the orientation-aware bounds with a larger buffer
            Rectangle currentBounds = _owner.GetOrientationAwareBounds();
            
            // INCREASE buffer size substantially - force NPCs to stay away from walls
            int bufferX = Math.Max(6, currentBounds.Width / 3);  // 33% of width
            int bufferY = Math.Max(6, currentBounds.Height / 3); // 33% of height

            // Calculate offset from position to bounds
            int offsetX = currentBounds.Left - (int)_owner.Position.X;
            int offsetY = currentBounds.Top - (int)_owner.Position.Y;

            // Create bounds at candidate position with larger buffer
            Rectangle bounds = new Rectangle(
                (int)position.X + offsetX + bufferX,
                (int)position.Y + offsetY + bufferY,
                Math.Max(2, currentBounds.Width - bufferX*2),
                Math.Max(2, currentBounds.Height - bufferY*2)
            );

            // Check more points for better wall detection
            int leftTile = bounds.Left / (int)tilemap.TileWidth;
            int rightTile = (bounds.Right - 1) / (int)tilemap.TileWidth;
            int topTile = bounds.Top / (int)tilemap.TileHeight;
            int bottomTile = (bounds.Bottom - 1) / (int)tilemap.TileHeight;
            
            // Check all 8 points around the perimeter plus center
            int centerXTile = (leftTile + rightTile) / 2;
            int centerYTile = (topTile + bottomTile) / 2;
            
            // Check corners and edges (9-point check)
            return IsWallTile(leftTile, topTile, tilemap) ||      // Top-left
                   IsWallTile(centerXTile, topTile, tilemap) ||   // Top-center
                   IsWallTile(rightTile, topTile, tilemap) ||     // Top-right
                   IsWallTile(leftTile, centerYTile, tilemap) ||  // Mid-left
                   IsWallTile(centerXTile, centerYTile, tilemap) ||  // Center
                   IsWallTile(rightTile, centerYTile, tilemap) || // Mid-right
                   IsWallTile(leftTile, bottomTile, tilemap) ||   // Bottom-left
                   IsWallTile(centerXTile, bottomTile, tilemap) || // Bottom-center
                   IsWallTile(rightTile, bottomTile, tilemap);    // Bottom-right
        }

        private bool IsWallTile(int col, int row, Tilemap tilemap)
        {
            if (col < 0 || row < 0 || col >= tilemap.Columns || row >= tilemap.Rows)
                return true; // Out of bounds is considered a wall
                
            var tileTileset = tilemap.GetTileset(col, row);
            var tileId = tilemap.GetTileId(col, row);
            return tileTileset == TilesetManager.Instance.WallTileset && 
                   AutotileMapper.IsWallTile(tileId);
        }

        public Vector2 GetVelocity() => _velocity;

        public void SetVelocity(Vector2 v)
        {
            _velocity = v;
            _knockbackTimer = KnockbackDuration;
            _isIdle = false;
            _stuckFrameCount = 0; // Reset stuck counter when externally setting velocity
        }

        // Add these fields to support debugging
        private Rectangle _collisionBoundsForDebug;
        private Vector2 _collisionPosForDebug;
        private int _frameCounter = 0;

        // Add this method to expose debug visualization
        public void DrawDebug(SpriteBatch spriteBatch, Texture2D pixel)
        {
            if (pixel == null || !DebugManager.Instance.ShowCollisionBoxes) return;
            
            // Draw the collision bounds used for wall detection
            DrawRect(spriteBatch, pixel, _collisionBoundsForDebug, Color.Purple * 0.5f);
            
            // Draw the path to the chase target if any
            if (_chaseTarget.HasValue)
            {
                DrawLine(spriteBatch, pixel, Position, _chaseTarget.Value, Color.Yellow * 0.3f);
                DrawCircle(spriteBatch, pixel, _chaseTarget.Value, 4, Color.Green * 0.6f);
            }
            
            // Draw stuck status
            if (_stuckFrameCount > 0)
            {
                DrawCircle(spriteBatch, pixel, Position, 3 + _stuckFrameCount/4, Color.Red * 0.5f);
            }
        }

        // Helper methods for debug visualization
        private void DrawRect(SpriteBatch spriteBatch, Texture2D pixel, Rectangle rect, Color color)
        {
            // Draw outline
            spriteBatch.Draw(pixel, new Rectangle(rect.Left, rect.Top, rect.Width, 1), color);
            spriteBatch.Draw(pixel, new Rectangle(rect.Left, rect.Bottom - 1, rect.Width, 1), color);
            spriteBatch.Draw(pixel, new Rectangle(rect.Left, rect.Top, 1, rect.Height), color);
            spriteBatch.Draw(pixel, new Rectangle(rect.Right - 1, rect.Top, 1, rect.Height), color);
        }

        private void DrawLine(SpriteBatch spriteBatch, Texture2D pixel, Vector2 start, Vector2 end, Color color)
        {
            Vector2 edge = end - start;
            float angle = (float)Math.Atan2(edge.Y, edge.X);
            spriteBatch.Draw(
                pixel,
                start,
                null,
                color,
                angle,
                Vector2.Zero,
                new Vector2(edge.Length(), 1),
                SpriteEffects.None,
                0);
        }

        private void DrawCircle(SpriteBatch spriteBatch, Texture2D pixel, Vector2 position, float radius, Color color)
        {
            spriteBatch.Draw(
                pixel,
                new Rectangle((int)(position.X - radius), (int)(position.Y - radius), (int)(radius * 2), (int)(radius * 2)),
                color);
        }
    }
}