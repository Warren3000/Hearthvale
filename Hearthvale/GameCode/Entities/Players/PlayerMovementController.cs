using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Hearthvale.GameCode.Entities.NPCs;

namespace Hearthvale.GameCode.Entities.Players
{
    public class PlayerMovementController
    {
        private readonly Player _player;

        public bool IsMoving() => _player._knockbackVelocity != Vector2.Zero;
        public bool IsKnockedBack => _player.IsKnockedBack;

        public PlayerMovementController(Player player)
        {
            _player = player;
        }

        public void Update(GameTime gameTime, IEnumerable<NPC> npcs)
        {
            // Knockback is now handled in Character.UpdateKnockback
        }

        public void SetVelocity(Vector2 velocity)
        {
            _player.SetKnockback(velocity);
        }
    }
}