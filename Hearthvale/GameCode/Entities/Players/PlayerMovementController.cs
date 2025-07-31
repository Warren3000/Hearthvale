using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using Hearthvale.GameCode.Entities.NPCs;

namespace Hearthvale.GameCode.Entities.Players
{
    public class PlayerMovementController
    {
        private readonly Player _player;
        private Vector2 _velocity;
        private float _knockbackTimer;
        private const float KnockbackDuration = 0.2f;
        public bool IsMoving() => _velocity != Vector2.Zero;
        public bool IsKnockedBack => _knockbackTimer > 0;

        public PlayerMovementController(Player player)
        {
            _player = player;
        }
        public void Update(GameTime gameTime, IEnumerable<NPC> npcs)
        {
            float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Handle knockback first
            if (_knockbackTimer > 0)
            {
                _knockbackTimer -= elapsed;
                _player.SetPosition(_player.Position + _velocity * elapsed);
                if (_knockbackTimer <= 0)
                {
                    _velocity = Vector2.Zero;
                }
            }
        }

        public void SetVelocity(Vector2 velocity)
        {
            _velocity = velocity;
            _knockbackTimer = KnockbackDuration;
        }
    }
}