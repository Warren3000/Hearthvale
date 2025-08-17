using Hearthvale.GameCode.Entities.NPCs;
using Hearthvale.GameCode.Utils;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Graphics;
using System.Collections.Generic;

namespace Hearthvale.GameCode.Entities.Players
{
    internal class PlayerAnimationComponent : NpcAnimationController
    {
        private readonly Player _player;

        public PlayerAnimationComponent(Player player, AnimatedSprite sprite, Dictionary<string, Animation> animations)
            : base(sprite, animations)
        {
            _player = player;
        }

        public void UpdateAnimation(bool isMoving)
        {
            // Get current cardinal direction from movement component
            var direction = _player.MovementComponent.FacingDirection;

            string animationName = isMoving ? "Mage_Walk" : "Mage_Idle";

            // Set sprite effects based on facing direction
            if (direction == CardinalDirection.West)
            {
                this.Sprite.Effects = SpriteEffects.FlipHorizontally;
            }
            else
            {
                this.Sprite.Effects = SpriteEffects.None;
            }

            SetAnimation(animationName);
        }
    }
}
