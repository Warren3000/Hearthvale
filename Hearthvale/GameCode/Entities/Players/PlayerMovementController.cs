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

        public PlayerMovementController(Player player)
        {
            _player = player;
        }

        public Vector2 Update(GameTime gameTime, KeyboardState keyboard, IEnumerable<NPC> npcs)
        {
            float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Handle knockback first
            if (_knockbackTimer > 0)
            {
                _knockbackTimer -= elapsed;
                _player.SetPosition(_player.Position + _velocity * elapsed);
                return _velocity; // Return knockback velocity and skip player input
            }

            Vector2 direction = Vector2.Zero;
            if (keyboard.IsKeyDown(Keys.W)) direction.Y--;
            if (keyboard.IsKeyDown(Keys.S)) direction.Y++;
            if (keyboard.IsKeyDown(Keys.A)) direction.X--;
            if (keyboard.IsKeyDown(Keys.D)) direction.X++;

            if (direction != Vector2.Zero)
            {
                direction.Normalize();
                _player.SetLastMovementDirection(direction);
                // Update facing direction based on the dominant axis of movement
                if (System.Math.Abs(direction.X) > System.Math.Abs(direction.Y))
                {
                    _player.SetFacingRight(direction.X > 0);
                }
            }

            _velocity = direction * _player.MovementSpeed * 100f * elapsed;

            // --- Collision Check ---
            Vector2 newPosition = _player.Position + _velocity;
            Rectangle candidateBounds = new Rectangle(
                (int)newPosition.X + 8,
                (int)newPosition.Y + 16,
                (int)_player.Sprite.Width / 2,
                (int)_player.Sprite.Height / 2
            );

            bool collision = false;
            foreach (var npc in npcs)
            {
                if (!npc.IsDefeated && candidateBounds.Intersects(npc.Bounds))
                {
                    collision = true;
                    break;
                }
            }

            if (!collision)
            {
                _player.SetPosition(newPosition);
            }
            // --- End Collision Check ---

            return _velocity;
        }

        public void SetVelocity(Vector2 velocity)
        {
            _velocity = velocity;
            _knockbackTimer = KnockbackDuration;
        }
    }
}