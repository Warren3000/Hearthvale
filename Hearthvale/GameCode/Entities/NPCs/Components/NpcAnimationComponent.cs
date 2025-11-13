using Hearthvale.GameCode.Entities.Animations;
using Hearthvale.GameCode.Entities.Components;
using Hearthvale.GameCode.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Graphics;
using System;

namespace Hearthvale.GameCode.Entities.NPCs.Components
{
    /// <summary>
    /// Manages NPC-specific animation logic including death fade-out
    /// </summary>
    public class NpcAnimationComponent
    {
        private readonly NPC _owner;
        private readonly CharacterAnimationComponent _baseAnimationComponent;
        private readonly MovementAnimationDriver _animDriver = new();

        private float _fadeOutTimer = 0f;
        private float _fadeOpacity = 1f;
        private const float FADE_OUT_DURATION = 1.5f;
        private bool _hasCompletedFadeOut = false;

        // Animation movement tracking
        private Vector2 _lastAnimPosition;
        private bool _movingForAnim;

        public float FadeOpacity => _fadeOpacity;
        public bool HasCompletedFadeOut => _hasCompletedFadeOut;

        public NpcAnimationComponent(NPC owner, CharacterAnimationComponent baseAnimationComponent)
        {
            _owner = owner;
            _baseAnimationComponent = baseAnimationComponent;
            _lastAnimPosition = owner.Position;
        }

        public void Update(GameTime gameTime)
        {
            float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Update health controller and get stun state
            bool isStunned = _owner.HealthComponent.Update(elapsed);

            // Update flash effects
            _baseAnimationComponent.Update(elapsed);

            // Handle defeated state with fade-out
            if (_owner.IsDefeated)
            {
                HandleDefeatedAnimation(elapsed);
                return;
            }


            // Compute movement delta for animation
            _movingForAnim = Vector2.DistanceSquared(_owner.MovementComponent.Position, _lastAnimPosition) > 0.01f;
            _lastAnimPosition = _owner.MovementComponent.Position;

            // Determine animation based on current state
            var direction = _owner.MovementComponent.FacingDirection;
            string animName = null;
            if (_owner.IsAttacking)
            {
                PlayDirectionalAnimation("Attack", direction);
            }
            else if (isStunned)
            {
                PlayDirectionalAnimation("Hit", direction);
            }
            else
            {
                switch (direction)
                {
                    case CardinalDirection.North:
                        animName = _movingForAnim ? "Run_Up" : "Idle_Up";
                        break;
                    case CardinalDirection.South:
                        animName = _movingForAnim ? "Run_Down" : "Idle_Down";
                        break;
                    case CardinalDirection.East:
                        animName = _movingForAnim ? "Run_Right" : "Idle_Right";
                        break;
                    case CardinalDirection.West:
                        animName = _movingForAnim ? "Run_Left" : "Idle_Left";
                        break;
                    default:
                        animName = _movingForAnim ? "Run_Down" : "Idle_Down";
                        break;
                }
                SetAnimationDirectionalSafe(animName, ComposeDirectionalName("Idle", direction));
            }

            UpdateSpritePosition();
        }

        private void HandleDefeatedAnimation(float elapsed)
        {
            // Set defeated animation if available
            SetAnimationDirectionalSafe("Defeated", "Idle");

            // Update fade-out timer
            _fadeOutTimer += elapsed;

            // Calculate fade opacity
            _fadeOpacity = MathF.Max(0f, 1f - (_fadeOutTimer / FADE_OUT_DURATION));

            // Apply visual effects for defeated state
            if (_baseAnimationComponent.Sprite != null)
            {
                _baseAnimationComponent.Sprite.Color = Color.White * _fadeOpacity;
            }

            // Hide weapon when fully faded
            if (_fadeOpacity <= 0f)
            {
                if (_owner.WeaponComponent?.EquippedWeapon != null)
                {
                    _owner.WeaponComponent.UnequipWeapon();
                }
                
                // Mark as completed so NPC can be removed
                _hasCompletedFadeOut = true;
            }
        }

        private void UpdateSpritePosition()
        {
            // Update the animated sprite
            if (_baseAnimationComponent.Sprite != null)
            {
                _baseAnimationComponent.Sprite.Position = _owner.Position;
            }
        }

        private void PlayDirectionalAnimation(string baseName, CardinalDirection direction)
        {
            string primary = ComposeDirectionalName(baseName, direction);
            string directionalFallback = ComposeDirectionalName("Idle", direction);
            SetAnimationDirectionalSafe(primary, directionalFallback);

            if (_baseAnimationComponent?.Sprite != null)
            {
                string currentName = _baseAnimationComponent.CurrentAnimationName;
                bool shouldLoop = string.IsNullOrEmpty(currentName) || !currentName.Contains("Attack", StringComparison.OrdinalIgnoreCase);
                _baseAnimationComponent.Sprite.IsLooping = shouldLoop;
            }
        }

        /// <summary>
        /// Safely sets animation with directional fallback
        /// </summary>
        private void SetAnimationDirectionalSafe(string primaryAnimation, string fallbackAnimation)
        {
            if (_baseAnimationComponent == null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(primaryAnimation) && _baseAnimationComponent.SetAnimation(primaryAnimation))
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(fallbackAnimation) && !string.Equals(primaryAnimation, fallbackAnimation, StringComparison.OrdinalIgnoreCase) && _baseAnimationComponent.SetAnimation(fallbackAnimation))
            {
                return;
            }

            _baseAnimationComponent.SetAnimation("Idle_Down");
        }

        private static string ComposeDirectionalName(string baseName, CardinalDirection direction)
        {
            if (string.IsNullOrWhiteSpace(baseName))
            {
                return baseName;
            }

            string suffix = direction switch
            {
                CardinalDirection.North => "Up",
                CardinalDirection.East => "Right",
                CardinalDirection.South => "Down",
                CardinalDirection.West => "Left",
                _ => "Down"
            };

            return $"{baseName}_{suffix}";
        }
    }
}