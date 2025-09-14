using Hearthvale.GameCode.Entities.Animations;
using Hearthvale.GameCode.Entities.Components;
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
                SetAnimationDirectionalSafe("Attack", "Idle_Down");
            }
            else if (isStunned)
            {
                SetAnimationDirectionalSafe("Hit", "Idle_Down");
            }
            else
            {
                switch (direction)
                {
                    case Hearthvale.GameCode.Utils.CardinalDirection.North:
                        animName = _movingForAnim ? "Run_Up" : "Idle_Up";
                        break;
                    case Hearthvale.GameCode.Utils.CardinalDirection.South:
                        animName = _movingForAnim ? "Run_Down" : "Idle_Down";
                        break;
                    case Hearthvale.GameCode.Utils.CardinalDirection.East:
                        animName = _movingForAnim ? "Run_Side" : "Idle_Side";
                        if (_baseAnimationComponent.Sprite != null)
                            _baseAnimationComponent.Sprite.Effects = Microsoft.Xna.Framework.Graphics.SpriteEffects.None;
                        break;
                    case Hearthvale.GameCode.Utils.CardinalDirection.West:
                        animName = _movingForAnim ? "Run_Side" : "Idle_Side";
                        if (_baseAnimationComponent.Sprite != null)
                            _baseAnimationComponent.Sprite.Effects = Microsoft.Xna.Framework.Graphics.SpriteEffects.FlipHorizontally;
                        break;
                    default:
                        animName = _movingForAnim ? "Run_Down" : "Idle_Down";
                        if (_baseAnimationComponent.Sprite != null)
                            _baseAnimationComponent.Sprite.Effects = Microsoft.Xna.Framework.Graphics.SpriteEffects.None;
                        break;
                }
                SetAnimationDirectionalSafe(animName, "Idle_Down");
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

                // Apply sprite effects for facing direction
                _baseAnimationComponent.Sprite.Effects = _owner.FacingRight
                    ? SpriteEffects.None
                    : SpriteEffects.FlipHorizontally;
            }
        }

        /// <summary>
        /// Safely sets animation with directional fallback
        /// </summary>
        private void SetAnimationDirectionalSafe(string primaryAnimation, string fallbackAnimation)
        {
            if (_baseAnimationComponent != null)
            {
                // Try to set the primary animation first
                string currentAnim = _baseAnimationComponent.Sprite?.Animation?.ToString() ?? "";
                _baseAnimationComponent.SetAnimation(primaryAnimation);

                // Check if the animation actually changed (meaning it exists)
                string newAnim = _baseAnimationComponent.Sprite?.Animation?.ToString() ?? "";

                // If animation didn't change and we're not already on the primary, try fallback
                if (currentAnim == newAnim && currentAnim != primaryAnimation)
                {
                    _baseAnimationComponent.SetAnimation(fallbackAnimation);
                }
            }
        }
    }
}