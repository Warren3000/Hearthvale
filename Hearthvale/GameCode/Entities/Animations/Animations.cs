using System;
using Microsoft.Xna.Framework;
using MonoGameLibrary.Graphics;

namespace Hearthvale.GameCode.Entities.Animations
{
    /// <summary>
    /// Drives animated sprite frames once per frame and switches between moving/idle
    /// only when the movement state changes (edge-trigger). Prevents per-frame resets
    /// that freeze animations on a single frame.
    /// </summary>
    public sealed class MovementAnimationDriver
    {
        private bool _wasMoving;
        private bool _firstTick = true;

        /// <summary>
        /// Advance animation exactly once per frame and switch animations only on state change.
        /// </summary>
        /// <param name="gameTime">Current frame GameTime.</param>
        /// <param name="isMoving">True if owner should be in Walk, false for Idle.</param>
        /// <param name="applyAnimation">
        /// Callback that applies the correct animation for the current state.
        /// For example:
        /// - moving=true: set "Walk" (or "Walk_{dir}" for NPC)
        /// - moving=false: set "Idle"
        /// </param>
        /// <param name="sprite">The AnimatedSprite to advance.</param>
        public void Tick(GameTime gameTime, bool isMoving, Action<bool> applyAnimation, AnimatedSprite sprite)
        {
            if (applyAnimation == null || sprite == null)
                return;

            // On first tick, apply the animation once using current state.
            if (_firstTick)
            {
                applyAnimation(isMoving);
                _wasMoving = isMoving;
                _firstTick = false;
            }
            else if (isMoving != _wasMoving)
            {
                // Edge-trigger state change
                applyAnimation(isMoving);
                _wasMoving = isMoving;
            }

            // Always advance frames once per frame.
            sprite.Update(gameTime);
        }

        public void Reset()
        {
            _wasMoving = false;
            _firstTick = true;
        }
    }
}