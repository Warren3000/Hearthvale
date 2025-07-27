using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary.Graphics;
using System;

namespace Hearthvale
{
    public class Player
    {
        private AnimatedSprite _sprite;
        private Vector2 _position;
        private bool _facingRight = true;
        private string _currentAnimationName = "Mage_Idle";
        private readonly TextureAtlas _atlas;

        public float MovementSpeed { get; }
        public AnimatedSprite Sprite => _sprite;
        public Vector2 Position => _position;
        public bool FacingRight => _facingRight;
        public bool IsAttacking { get; set; }

        private int _maxHealth = 10;
        private int _currentHealth;
        public int MaxHealth => _maxHealth;
        public int CurrentHealth => _currentHealth;
        public bool IsDefeated => _currentHealth <= 0;

        public Rectangle Bounds => new Rectangle(
            (int)Position.X + 8,
            (int)Position.Y + 16,
            (int)Sprite.Width / 2,
            (int)Sprite.Height / 2
        );

        public Player(TextureAtlas atlas, Vector2 startPosition, float movementSpeed = 3.0f)
        {
            _atlas = atlas;
            _sprite = _atlas.CreateAnimatedSprite("Mage_Idle");
            _sprite.Scale = new Vector2(1f, 1f);
            _position = startPosition;
            MovementSpeed = movementSpeed;
            _currentHealth = _maxHealth;
        }

        public void Update(GameTime gameTime, KeyboardState keyboard)
        {
            // Movement
            Vector2 movement = Vector2.Zero;
            if (keyboard.IsKeyDown(Keys.A) || keyboard.IsKeyDown(Keys.Left))
            {
                movement.X -= 1;
                _facingRight = false;
            }
            if (keyboard.IsKeyDown(Keys.D) || keyboard.IsKeyDown(Keys.Right))
            {
                movement.X += 1;
                _facingRight = true;
            }
            if (keyboard.IsKeyDown(Keys.W) || keyboard.IsKeyDown(Keys.Up))
                movement.Y -= 1;
            if (keyboard.IsKeyDown(Keys.S) || keyboard.IsKeyDown(Keys.Down))
                movement.Y += 1;

            if (movement != Vector2.Zero)
            {
                movement.Normalize();
                _position += movement * MovementSpeed;
            }

            UpdateAnimation(keyboard, movement != Vector2.Zero);

            _sprite.Position = _position;
            _sprite.Effects = _facingRight ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            _sprite.Update(gameTime);
        }
        public void ClampToBounds(Rectangle bounds)
        {
            float clampedX = MathHelper.Clamp(Position.X, bounds.Left, bounds.Right - Sprite.Width);
            float clampedY = MathHelper.Clamp(Position.Y, bounds.Top, bounds.Bottom - Sprite.Height);
            SetPosition(new Vector2(clampedX, clampedY));
        }
        private void UpdateAnimation(KeyboardState keyboard, bool moving)
        {
            string desiredAnimation = moving ? "Mage_Walk" : "Mage_Idle";
            if (_currentAnimationName != desiredAnimation)
            {
                _sprite.Animation = _atlas.GetAnimation(desiredAnimation);
                _currentAnimationName = desiredAnimation;
            }
        }
        public void Move(Vector2 movement, Rectangle roomBounds, float spriteWidth, float spriteHeight)
        {
            Vector2 newPosition = Position + movement;
            float clampedX = MathHelper.Clamp(
                newPosition.X,
                roomBounds.Left,
                roomBounds.Right - spriteWidth
            );
            float clampedY = MathHelper.Clamp(
                newPosition.Y,
                roomBounds.Top,
                roomBounds.Bottom - spriteHeight
            );
            SetPosition(new Vector2(clampedX, clampedY));
        }
        public void TakeDamage(int amount)
        {
            _currentHealth = Math.Max(0, _currentHealth - amount);
            // Optionally: trigger effects, invulnerability, etc.
        }
        public void Heal(int amount)
        {
            _currentHealth = Math.Min(_maxHealth, _currentHealth + amount);
        }
        public void SetPosition(Vector2 pos)
        {
            _position = pos;
            _sprite.Position = pos;
        }

        public Rectangle GetAttackArea()
        {
            int width = 32;
            int height = (int)_sprite.Height;
            int offsetX = _facingRight ? (int)_sprite.Width : -width;
            int x = (int)_position.X + offsetX;
            int y = (int)_position.Y;
            return new Rectangle(x, y, width, height);
        }
    }
}