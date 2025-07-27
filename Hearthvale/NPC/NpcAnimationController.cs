using MonoGameLibrary.Graphics;
using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace Hearthvale
{
    internal class NpcAnimationController
    {
        private readonly AnimatedSprite _sprite;
        private readonly Dictionary<string, Animation> _animations;
        private string _currentAnimationName;
        private Color _originalColor;
        private float _flashTimer;
        private static readonly Color HitFlashColor = Color.Red;
        private static readonly float FlashDuration = 0.15f;

        public AnimatedSprite Sprite => _sprite;

        public NpcAnimationController(AnimatedSprite sprite, Dictionary<string, Animation> animations)
        {
            _sprite = sprite;
            _animations = animations;
            _originalColor = sprite.Color;
        }


        public void SetAnimation(string name)
        {
            if (_currentAnimationName == name) return;
            if (_animations.ContainsKey(name) && _animations[name] != null)
            {
                _sprite.Animation = _animations[name];
                _currentAnimationName = name;
            }
        }

        public void Flash()
        {
            _sprite.Color = HitFlashColor;
            _flashTimer = FlashDuration;
        }

        public void UpdateFlash(float elapsed)
        {
            if (_flashTimer > 0)
            {
                _flashTimer -= elapsed;
                if (_flashTimer <= 0)
                    _sprite.Color = _originalColor;
            }
        }
    }
}

