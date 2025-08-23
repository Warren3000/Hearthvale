using Microsoft.Xna.Framework;
using MonoGameLibrary.Graphics;
using System.Collections.Generic;

namespace Hearthvale.GameCode.Entities.Components
{
    internal class NpcAnimationComponent
    {
        private readonly AnimatedSprite _sprite;
        private readonly Dictionary<string, Animation> _animations;
        private string _currentAnimationName;
        private Color _originalColor;
        private float _flashTimer;
        private static readonly Color HitFlashColor = Color.Red;
        private static readonly float FlashDuration = 0.15f;

        public AnimatedSprite Sprite => _sprite;

        public NpcAnimationComponent(AnimatedSprite sprite, Dictionary<string, Animation> animations)
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

        
    }
}

