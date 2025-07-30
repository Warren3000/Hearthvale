using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Hearthvale.GameCode.Entities.Players
{
    public class PlayerMovementController
    {
        private readonly Player _player;
        private Vector2 _velocity = Vector2.Zero;

        public PlayerMovementController(Player player)
        {
            _player = player;
        }

        public Vector2 Update(GameTime gameTime, KeyboardState keyboard)
        {
            Vector2 movement = Vector2.Zero;
            if (keyboard.IsKeyDown(Keys.A) || keyboard.IsKeyDown(Keys.Left))
                movement.X -= 1;
            if (keyboard.IsKeyDown(Keys.D) || keyboard.IsKeyDown(Keys.Right))
                movement.X += 1;
            if (keyboard.IsKeyDown(Keys.W) || keyboard.IsKeyDown(Keys.Up))
                movement.Y -= 1;
            if (keyboard.IsKeyDown(Keys.S) || keyboard.IsKeyDown(Keys.Down))
                movement.Y += 1;

            // Apply knockback velocity if present
            if (_velocity != Vector2.Zero)
            {
                _player.SetPosition(_player.Position + _velocity * _player.MovementSpeed);
                // Dampen velocity for next frame (simple friction)
                _velocity *= 0.85f;
                if (_velocity.LengthSquared() < 0.01f)
                    _velocity = Vector2.Zero;
            }
            else if (movement != Vector2.Zero)
            {
                movement.Normalize();
                _player.SetPosition(_player.Position + movement * _player.MovementSpeed);
                _player.SetLastMovementDirection(movement);
                if (movement.X != 0)
                    _player.SetFacingRight(movement.X > 0);
            }

            return movement;
        }

        public void SetVelocity(Vector2 velocity)
        {
            _velocity = velocity;
        }
    }
}