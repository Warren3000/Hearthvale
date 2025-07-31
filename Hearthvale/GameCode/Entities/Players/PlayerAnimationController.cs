using MonoGameLibrary.Graphics;
using Microsoft.Xna.Framework;
using Hearthvale.GameCode.Entities.Players;
using System.Collections.Generic;
using Hearthvale.GameCode.Entities.NPCs;

namespace Hearthvale.GameCode.Entities.Players
{
    internal class PlayerAnimationController : NpcAnimationController
    {
        private readonly Player _player;

        public PlayerAnimationController(Player player, AnimatedSprite sprite, Dictionary<string, Animation> animations)
            : base(sprite, animations)
        {
            _player = player;
        }

        public void UpdateAnimation(bool moving)
        {
            string desiredAnimation = moving ? "Mage_Walk" : "Mage_Idle";
            SetAnimation(desiredAnimation);
        }
    }
}
