using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGameLibrary.Graphics;
using Hearthvale.GameCode.Collision;
using Microsoft.Xna.Framework;
using Hearthvale.GameCode.Managers;
using System.Linq;

namespace Hearthvale.GameCode.Entities
{
    public class Projectile : ICollisionActor
    {
        public AnimatedSprite Sprite { get; }
        public Vector2 Position { get; set; }
        public Vector2 Velocity { get; set; }
        public int Damage { get; }
        public bool IsActive { get; set; } = true;
        public Rectangle BoundingBox => new Rectangle((int)Position.X, (int)Position.Y, Sprite.Region.Width, Sprite.Region.Height);

        // Aether collision properties
        public IShapeF Bounds { get; set; }

        private float _gracePeriod = 0.05f;
        private float _timer;
        public bool CanCollide => _timer >= _gracePeriod;

        public string OwnerId { get; }
        public ProjectileType Type { get; }

        private CollisionWorld _collisionWorld;

        public Projectile(Animation animation, Vector2 position, Vector2 velocity, int damage, string ownerId = "Player", ProjectileType type = ProjectileType.Arrow)
        {
            Sprite = new AnimatedSprite(animation);
            Position = position;
            Velocity = velocity;
            Damage = damage;
            OwnerId = ownerId;
            Type = type;

            Sprite.Origin = new Vector2(Sprite.Region.Width / 2f, Sprite.Region.Height / 2f);

            UpdateCollisionBounds();
        }

        public void SetCollisionWorld(CollisionWorld collisionWorld)
        {
            _collisionWorld = collisionWorld;
        }

        public void Update(GameTime gameTime)
        {
            if (!IsActive) return;

            _timer += (float)gameTime.ElapsedGameTime.TotalSeconds;
            Position += Velocity * (float)gameTime.ElapsedGameTime.TotalSeconds;
            Sprite.Rotation = (float)System.Math.Atan2(Velocity.Y, Velocity.X);
            Sprite.Update(gameTime);

            UpdateCollisionBounds();

            // Update position in collision world if registered
            if (_collisionWorld != null)
            {
                var projectileActor = _collisionWorld.GetActorsOfType<ProjectileCollisionActor>()
                    .FirstOrDefault(actor => actor.Projectile == this);

                if (projectileActor != null)
                {
                    _collisionWorld.UpdateActorPosition(projectileActor, Position);
                }
            }
        }

        private void UpdateCollisionBounds()
        {
            Bounds = new RectangleF(
                Position.X - Sprite.Region.Width / 2f,
                Position.Y - Sprite.Region.Height / 2f,
                Sprite.Region.Width,
                Sprite.Region.Height
            );
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (IsActive)
            {
                Sprite.Draw(spriteBatch, Position);
            }
        }

        public void OnCollision(CollisionEventArgs collisionInfo)
        {
            if (!CanCollide) return;

            var otherActor = collisionInfo.Other;

            if (otherActor is WallCollisionActor)
            {
                // Hit a wall - projectile should be destroyed
                IsActive = false;
            }
            else if (otherActor is NpcCollisionActor npcActor && OwnerId == "Player")
            {
                // Player projectile hit an NPC - handled by NpcCollisionActor.OnCollision
                IsActive = false;
            }
            else if (otherActor is PlayerCollisionActor && OwnerId != "Player")
            {
                // Enemy projectile hit player - handled by PlayerCollisionActor.OnCollision
                IsActive = false;
            }
        }
    }

    public enum ProjectileType
    {
        Arrow,
        Fireball,
        Magic,
        Bullet
    }
}