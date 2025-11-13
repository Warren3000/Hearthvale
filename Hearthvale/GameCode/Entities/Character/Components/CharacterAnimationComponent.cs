using Hearthvale.GameCode.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Graphics;
using System;
using System.Collections.Generic;

namespace Hearthvale.GameCode.Entities.Components
{
    /// <summary>
    /// Manages character animations, sprites, and visual effects
    /// </summary>
    public class CharacterAnimationComponent
    {
        // Track last animated direction and movement state
        private CardinalDirection? _lastAnimatedDirection = null;
        private bool? _lastWasMoving = null;
        private bool _lastWasAttacking = false;
        private readonly Character _character;
        private AnimatedSprite _sprite;
        private string _currentAnimationName;
        private bool _isFlashing;
        private float _flashTimer;
        private const float FlashDuration = 0.2f;
        private Color _originalColor = Color.White;
        private Dictionary<string, Animation> _animations;

        public AnimatedSprite Sprite => _sprite;
        public string CurrentAnimationName => _currentAnimationName;
        public bool IsFlashing => _isFlashing;

        public CharacterAnimationComponent(Character character, AnimatedSprite sprite)
        {
            _character = character;
            _sprite = sprite;
            _animations = new Dictionary<string, Animation>();

            if (sprite != null)
            {
                _originalColor = sprite.Color;
            }
        }

        public void SetSprite(AnimatedSprite sprite)
        {
            _sprite = sprite;
            if (sprite != null)
            {
                _originalColor = sprite.Color;
                _sprite.Position = _character.Position;
            }
        }

        public void AddAnimation(string name, Animation animation)
        {
            _animations[name] = animation;
        }

        public bool SetAnimation(string animationName)
        {
            if (_sprite == null || string.IsNullOrEmpty(animationName))
                return false;

            if (_currentAnimationName == animationName && _sprite.Animation != null)
            {
                return true;
            }

            if (_animations.TryGetValue(animationName, out var animation))
            {
                _currentAnimationName = animationName;
                _sprite.Animation = animation;
                _sprite.IsLooping = true;
                return true;
            }

            return false;
        }

        public void Flash()
        {
            _isFlashing = true;
            _flashTimer = FlashDuration;

            if (_sprite != null)
            {
                _sprite.Color = Color.Red;
            }
        }

        public void ApplyAlphaForDefeated()
        {
            if (_character.IsDefeated && _sprite != null)
            {
                _sprite.Color = Color.White * 0.5f;
            }
        }

        public void Update(float deltaTime)
        {
            if (_isFlashing)
            {
                _flashTimer -= deltaTime;

                if (_flashTimer <= 0)
                {
                    _isFlashing = false;
                    if (_sprite != null)
                    {
                        _sprite.Color = _originalColor;
                    }
                }
            }

            if (_sprite != null)
            {
                _sprite.Update(new GameTime(new TimeSpan(), TimeSpan.FromSeconds(deltaTime)));
                // Atlas now top-left aligned: keep sprite position locked to logical character position
                _sprite.Position = _character.Position;
            }
        }
        public void UpdateAnimation(bool isMoving)
        {
            // Get current cardinal direction from movement component
            var direction = _character.MovementComponent.FacingDirection;
            bool isAttacking = _character.IsAttacking;
            bool shouldLoop = !isAttacking;

            if (_lastAnimatedDirection == direction && _lastWasMoving == isMoving && _lastWasAttacking == isAttacking)
            {
                return;
            }

            string animationName;

            if (isAttacking)
            {
                animationName = direction switch
                {
                    CardinalDirection.North => "Attack_01_Up",
                    CardinalDirection.South => "Attack_01_Down",
                    CardinalDirection.East => "Attack_01_Right",
                    CardinalDirection.West => "Attack_01_Left",
                    _ => "Attack_01_Down"
                };
            }
            else
            {
                animationName = direction switch
                {
                    CardinalDirection.North => isMoving ? "Run_Up" : "Idle_Up",
                    CardinalDirection.South => isMoving ? "Run_Down" : "Idle_Down",
                    CardinalDirection.East => isMoving ? "Run_Right" : "Idle_Right",
                    CardinalDirection.West => isMoving ? "Run_Left" : "Idle_Left",
                    _ => isMoving ? "Run_Down" : "Idle_Down"
                };
            }

            bool mirroredFallback = false;
            if (!string.IsNullOrEmpty(animationName))
            {
                if (!SetAnimation(animationName))
                {
                    if (TryResolveMirroredFallback(animationName, out var fallback, out var shouldMirror)
                        && SetAnimation(fallback))
                    {
                        mirroredFallback = shouldMirror;
                    }
                    else
                    {
                        mirroredFallback = false;
                    }
                }

                if (_sprite != null)
                {
                    _sprite.Effects = mirroredFallback ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
                    _sprite.IsLooping = shouldLoop;
                }
            }

            _lastAnimatedDirection = direction;
            _lastWasMoving = isMoving;
            _lastWasAttacking = isAttacking;
        }

        private static bool TryResolveMirroredFallback(string animationName, out string fallbackName, out bool shouldMirror)
        {
            fallbackName = null;
            shouldMirror = false;

            const string leftSuffix = "_Left";
            const string rightSuffix = "_Right";

            if (animationName.EndsWith(leftSuffix, StringComparison.OrdinalIgnoreCase))
            {
                fallbackName = animationName[..^leftSuffix.Length] + rightSuffix;
                shouldMirror = true;
                return true;
            }

            if (animationName.EndsWith(rightSuffix, StringComparison.OrdinalIgnoreCase))
            {
                fallbackName = animationName[..^rightSuffix.Length] + leftSuffix;
                shouldMirror = true;
                return true;
            }

            return false;
        }

        public TimeSpan GetAnimationDuration(string animationName)
        {
            if (string.IsNullOrWhiteSpace(animationName))
            {
                return TimeSpan.Zero;
            }

            if (_animations.TryGetValue(animationName, out var animation))
            {
                var frameCount = animation.Frames?.Count ?? 0;
                if (frameCount == 0)
                {
                    return TimeSpan.Zero;
                }

                return TimeSpan.FromTicks(animation.Delay.Ticks * frameCount);
            }

            return TimeSpan.Zero;
        }
    }
}