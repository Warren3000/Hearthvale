using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Hearthvale.GameCode.Entities.NPCs;

namespace Hearthvale.GameCode.Entities.Players
{
    public class PlayerMovementComponent
    {
        private readonly Player _player;

        public bool IsMoving() => _player.GetKnockbackVelocity() != Vector2.Zero;
        public bool IsKnockedBack => _player.IsKnockedBack;


        public PlayerMovementComponent(Player player)
        {
            _player = player;
            DefaultMovementSpeed();
        }

        public void Update(GameTime gameTime, IEnumerable<NPC> npcs)
        {
            // Knockback is now handled in Character.UpdateKnockback
        }

        public void SetVelocity(Vector2 velocity)
        {
            _player.SetKnockback(velocity);
        }
        public float GetMovementSpeed()
        {
            return _player.MovementSpeed;
        }
        public float DefaultMovementSpeed()
        {
            return _player.MovementSpeed;
        }
    }
}