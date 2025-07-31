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
        public Rectangle BoundingBox => new Rectangle((int)Position.X, (int)Position.Y, (int)Sprite.Width, (int)Sprite.Height);

        private float _gracePeriod = 0.05f; // 50ms grace period before collision is active
        private float _timer;

        public bool CanCollide => _timer >= _gracePeriod;

        public Projectile(TextureRegion texture, Vector2 position, Vector2 velocity, int damage)
        {
            Sprite = new AnimatedSprite(new Animation(new() { texture }, System.TimeSpan.FromSeconds(1)));
            Position = position;
            Velocity = velocity;
            Damage = damage;
        }

        public void Update(GameTime gameTime)
        {
            float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _timer += elapsed;
            Position += Velocity * elapsed;
            Sprite.Position = Position;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            Sprite.Draw(spriteBatch, Position);
        }
    }
}