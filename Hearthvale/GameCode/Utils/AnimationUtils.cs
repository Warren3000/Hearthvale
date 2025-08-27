using MonoGame.Extended.Animations;
using MonoGameLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hearthvale.GameCode.Utils
{
    public static class AnimationUtils
    {
        /// <summary>
        /// Returns a new Animation with its Delay multiplied by the given factor.
        /// If factor > 1, animation is slower; if factor < 1, animation is faster.
        /// </summary>
        public static Animation WithDelayFactor(Animation animation, float factor)
        {
            if (animation == null || factor <= 0f)
                return animation;

            animation.Delay = TimeSpan.FromMilliseconds(animation.Delay.TotalMilliseconds * factor);
            return animation;
        }

        /// <summary>
        /// Applies the delay factor to all animations in a dictionary.
        /// </summary>
        public static void ApplyDelayFactorToAll(IDictionary<string, Animation> animations, float factor)
        {
            if (animations == null) return;
            foreach (var key in animations.Keys.ToList())
            {
                animations[key] = WithDelayFactor(animations[key], factor);
            }
        }
    }
}