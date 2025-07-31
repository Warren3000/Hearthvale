using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Graphics;

namespace Hearthvale.GameCode.Entities
{
    public class Projectile
    {
        public AnimatedSprite Sprite { get; }
        public Vector2 Position { get; set; }
        public Vector2 Velocity { get; set; }
        public int Damage { get; }
        public bool IsActive { get; set; } = true;
        public Rectangle BoundingBox => new Rectangle((int)Position.X, (int)Position.Y, Sprite.Region.Width, Sprite.Region.Height);
        private float _gracePeriod = 0.05f; // 50ms grace period before collision is active
        private float _timer;
        public bool CanCollide => _timer >= _gracePeriod;

        public Projectile(Animation animation, Vector2 position, Vector2 velocity, int damage)
        {
            Sprite = new AnimatedSprite(animation);
            Position = position;
            Velocity = velocity;
            Damage = damage;
            Sprite.Origin = new Vector2(Sprite.Region.Width / 2f, Sprite.Region.Height / 2f);
        }

        public void Update(GameTime gameTime)
        {
            if (!IsActive) return;

            _timer += (float)gameTime.ElapsedGameTime.TotalSeconds;
            Position += Velocity * (float)gameTime.ElapsedGameTime.TotalSeconds;
            Sprite.Rotation = (float)System.Math.Atan2(Velocity.Y, Velocity.X);
            Sprite.Update(gameTime);
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (IsActive)
            {
                Sprite.Draw(spriteBatch, Position);
            }
        }
    }
}