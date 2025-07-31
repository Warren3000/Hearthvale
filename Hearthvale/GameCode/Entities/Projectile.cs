using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Graphics;
using System.Collections.Generic;
using System;

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

        public Projectile(TextureRegion textureRegion, Vector2 position, Vector2 velocity, int damage)
        {
            var animation = new Animation(new List<TextureRegion> { textureRegion }, TimeSpan.FromSeconds(1));
            Sprite = new AnimatedSprite(animation);
            Position = position;
            Velocity = velocity;
            Damage = damage;
            Sprite.Rotation = (float)Math.Atan2(Velocity.Y, Velocity.X);
        }

        public void Update(GameTime gameTime)
        {
            Position += Velocity * (float)gameTime.ElapsedGameTime.TotalSeconds;
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