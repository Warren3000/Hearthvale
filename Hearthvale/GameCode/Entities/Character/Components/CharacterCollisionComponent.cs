using Hearthvale.GameCode.Entities.NPCs;
using Hearthvale.GameCode.Managers;
using Hearthvale.GameCode.Utils;
using Microsoft.Xna.Framework;
using MonoGameLibrary.Graphics;
using System.Collections.Generic;
using System.Linq;
using Hearthvale.GameCode.Collision;
using MonoGame.Extended;
using System;

namespace Hearthvale.GameCode.Entities.Components
{
    /// <summary>
    /// Handles character collision detection and movement using the unified physics system.
    /// Legacy tile-based collision has been replaced with physics world collision actors.
    /// </summary>
    public class CharacterCollisionComponent
    {
        private readonly Character _character;
        private Vector2 _knockbackVelocity;
        private float _knockbackTimer;
        private const float KnockbackDuration = 0.2f;
        private const float BounceDamping = 0.5f;

        // Physics-based collision system
        private CollisionWorld _collisionWorld;

        public bool IsKnockedBack => _knockbackTimer > 0;

        public CharacterCollisionComponent(Character character)
        {
            _character = character;
        }

        public void SetCollisionWorld(CollisionWorld collisionWorld)
        {
            _collisionWorld = collisionWorld;
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

                float dampingFactor = 1.0f - (elapsed * 2.0f);
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

        private void ApplyBounceEffect()
        {
            if (_knockbackTimer > 0)
            {
                _knockbackVelocity = -_knockbackVelocity * BounceDamping;
                if (_knockbackVelocity.LengthSquared() < 25f)
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
                //Log.Warning(LogArea.Collision, "Invalid position detected during knockback - resetting");
                _knockbackVelocity = Vector2.Zero;
                _knockbackTimer = 0;
                return false;
            }
            return true;
        }

        /// <summary>
        /// Main movement method that attempts to move the character to the target position.
        /// Uses physics-based collision detection with wall sliding for smooth movement.
        /// </summary>
        public bool TryMove(Vector2 nextPosition, IEnumerable<Character> otherCharacters = null)
        {
            return TryMoveWithWallSliding(nextPosition);
        }

        private bool TryMoveWithWallSliding(Vector2 nextPosition)
        {
            Vector2 currentPos = _character.Position;
            Vector2 movement = nextPosition - currentPos;

            if (movement.LengthSquared() < 0.001f)
                return true;

            // Small moves: single check + slide
            const float MinSweepDistance = 6f;
            if (movement.Length() <= MinSweepDistance)
            {
                Rectangle nextBoundsSmall = GetBoundsAtPosition(nextPosition);
                if (!IsPositionBlocked(nextBoundsSmall))
                {
                    _character.SetPosition(nextPosition);
                    return true;
                }

                if (TrySlideHorizontally(nextPosition, currentPos) ||
                    TrySlideVertically(nextPosition, currentPos))
                {
                    return true;
                }

                if (IsKnockedBack)
                    ApplyBounceEffect();

                return FindSafePosition(currentPos, nextPosition);
            }

            // Broad-phase: fetch candidate blockers for the whole path once
            List<ICollisionActor> candidates = null;
            if (_collisionWorld != null)
            {
                Rectangle startBounds = GetBoundsAtPosition(currentPos);
                Rectangle endBounds = GetBoundsAtPosition(nextPosition);
                var sweptLeft = Math.Min(startBounds.Left, endBounds.Left);
                var sweptTop = Math.Min(startBounds.Top, endBounds.Top);
                var sweptRight = Math.Max(startBounds.Right, endBounds.Right);
                var sweptBottom = Math.Max(startBounds.Bottom, endBounds.Bottom);
                var sweptRectF = new RectangleF(sweptLeft, sweptTop, sweptRight - sweptLeft, sweptBottom - sweptTop);

                candidates = _collisionWorld.GetActorsInBounds(sweptRectF).ToList();
            }

            // Swept/path collision with capped steps to avoid heavy per-pixel checks
            const float StepPixels = 8f;
            int steps = Math.Max(1, Math.Min(12, (int)Math.Ceiling(movement.Length() / StepPixels)));
            Vector2 lastValidPos = currentPos;

            for (int i = 1; i <= steps; i++)
            {
                float t = (float)i / steps;
                Vector2 testPos = currentPos + movement * t;
                Rectangle testBounds = GetBoundsAtPosition(testPos);

                if (IsPositionBlocked(testBounds, candidates))
                {
                    if (TrySlideHorizontally(testPos, lastValidPos) ||
                        TrySlideVertically(testPos, lastValidPos))
                    {
                        return true;
                    }

                    if (IsKnockedBack)
                        ApplyBounceEffect();

                    return FindSafePosition(lastValidPos, testPos);
                }

                lastValidPos = testPos;
            }

            _character.SetPosition(nextPosition);
            return true;
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

        private bool FindSafePosition(Vector2 currentPos, Vector2 targetPos)
        {
            Vector2 direction = targetPos - currentPos;
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

            float nudgeDistance = 1.0f;
            Vector2[] nudgeDirections = new Vector2[]
            {
                new Vector2(nudgeDistance, 0),
                new Vector2(-nudgeDistance, 0),
                new Vector2(0, nudgeDistance),
                new Vector2(0, -nudgeDistance),
                new Vector2(nudgeDistance, nudgeDistance),
                new Vector2(-nudgeDistance, nudgeDistance),
                new Vector2(nudgeDistance, -nudgeDistance),
                new Vector2(-nudgeDistance, -nudgeDistance)
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
            // Use the same simple centering logic as the base Character.GetTightSpriteBounds method
            // Get current bounds to determine size
            Rectangle currentBounds = _character.Bounds;
            int width = currentBounds.Width;
            int height = currentBounds.Height;
            
            // Center the bounds at the target position using the same logic as Character.GetTightSpriteBounds
            int left = (int)Math.Round(position.X - width / 2f);
            int top = (int)Math.Round(position.Y - height / 2f);
            
            return new Rectangle(left, top, width, height);
        }

        /// <summary>
        /// Physics-based collision detection using pre-fetched candidates for performance.
        /// This is the primary collision detection method.
        /// </summary>
        private bool IsPositionBlocked(Rectangle bounds, List<ICollisionActor> candidates = null)
        {
            // Physics-based collision detection
            if (_collisionWorld != null)
            {
                var testCandidates = candidates;
                if (testCandidates == null)
                {
                    var rectF = new RectangleF(bounds.X, bounds.Y, bounds.Width, bounds.Height);
                    testCandidates = _collisionWorld.GetActorsInBounds(rectF).ToList();
                }

                foreach (var actor in testCandidates)
                {
                    // Ignore self
                    if (IsOwnCollisionActor(actor))
                        continue;

                    // Check for blocking actors
                    if (IsBlockingActor(actor))
                    {
                        if (actor.Bounds.BoundingRectangle.Intersects(bounds))
                            return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Determines if the collision actor belongs to this character (to avoid self-collision)
        /// </summary>
        private bool IsOwnCollisionActor(ICollisionActor actor)
        {
            return (actor is PlayerCollisionActor pca && ReferenceEquals(pca.Player, _character)) ||
                   (actor is NpcCollisionActor nca && ReferenceEquals(nca.Npc, _character));
        }

        /// <summary>
        /// Determines if the collision actor should block movement
        /// </summary>
        private bool IsBlockingActor(ICollisionActor actor)
        {
            return actor is WallCollisionActor || 
                   actor is ChestCollisionActor ||
                   actor is PlayerCollisionActor || 
                   actor is NpcCollisionActor;
        }

        /// <summary>
        /// Validates that the collision system is properly initialized
        /// </summary>
        public bool IsCollisionSystemReady()
        {
            if (_collisionWorld == null)
            {
                //Log.Warn(LogArea., "CollisionWorld not set on CharacterCollisionComponent. Call SetCollisionWorld() during initialization.");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Gets debug information about the current collision state
        /// </summary>
        public string GetCollisionDebugInfo()
        {
            var info = new System.Text.StringBuilder();
            info.AppendLine($"Collision System Ready: {IsCollisionSystemReady()}");
            info.AppendLine($"Knocked Back: {IsKnockedBack}");
            
            if (_collisionWorld != null)
            {
                var actorCount = _collisionWorld.Actors.Count();
                info.AppendLine($"Total Collision Actors: {actorCount}");
                
                var wallActors = _collisionWorld.Actors.OfType<WallCollisionActor>().Count();
                var chestActors = _collisionWorld.Actors.OfType<ChestCollisionActor>().Count();
                var characterActors = _collisionWorld.Actors.OfType<PlayerCollisionActor>().Count() + 
                                     _collisionWorld.Actors.OfType<NpcCollisionActor>().Count();
                
                info.AppendLine($"Wall Actors: {wallActors}");
                info.AppendLine($"Chest Actors: {chestActors}");
                info.AppendLine($"Character Actors: {characterActors}");
            }
            
            return info.ToString();
        }
    }
}