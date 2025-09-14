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

        public void SetAnimation(string animationName)
        {
            if (_sprite == null || string.IsNullOrEmpty(animationName))
                return;

            if (_currentAnimationName == animationName)
                return;

            _currentAnimationName = animationName;

            if (_animations.TryGetValue(animationName, out var animation))
            {
                _sprite.Animation = animation;
            }
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

                // Update sprite effects based on facing direction
                _sprite.Effects = _character.FacingRight ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            }
        }
        public void UpdateAnimation(bool isMoving)
        {
            // Get current cardinal direction from movement component
            var direction = _character.MovementComponent.FacingDirection;
            string animationName = null;

            // Only update animation if direction or movement state changed
            if (_lastAnimatedDirection != direction || _lastWasMoving != isMoving)
            {
                switch (direction)
                {
                    case CardinalDirection.North:
                        animationName = isMoving ? "Run_Up" : "Idle_Up";
                        break;
                    case CardinalDirection.South:
                        animationName = isMoving ? "Run_Down" : "Idle_Down";
                        break;
                    case CardinalDirection.East:
                        animationName = isMoving ? "Run_Side" : "Idle_Side";
                        this.Sprite.Effects = SpriteEffects.None;
                        break;
                    case CardinalDirection.West:
                        animationName = isMoving ? "Run_Side" : "Idle_Side";
                        this.Sprite.Effects = SpriteEffects.FlipHorizontally;
                        break;
                    default:
                        animationName = isMoving ? "Run_Down" : "Idle_Down";
                        this.Sprite.Effects = SpriteEffects.None;
                        break;
                }

                SetAnimation(animationName);
                _lastAnimatedDirection = direction;
                _lastWasMoving = isMoving;
            }
        }
    }
}