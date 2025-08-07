using MonoGame.Extended;
using Microsoft.Xna.Framework;
using Hearthvale.GameCode.Entities.Characters;
using Hearthvale.GameCode.Managers;

namespace Hearthvale.GameCode.Collision
{
    /// <summary>
    /// Collision actor for the player character
    /// </summary>
    public class PlayerCollisionActor : ICollisionActor
    {
        public IShapeF Bounds { get; set; }
        public Character Player { get; }

        public PlayerCollisionActor(Character player, RectangleF bounds)
        {
            Player = player;
            Bounds = bounds;
        }

        public void OnCollision(CollisionEventArgs collisionInfo)
        {
            // Handle player collision response through Aether physics
            if (collisionInfo.Other is ProjectileCollisionActor projectileActor)
            {
                if (projectileActor.Projectile.OwnerId != "Player" && !Player.IsDefeated)
                {
                    // Notify combat manager of projectile hit
                    CombatManager.Instance?.HandleProjectilePlayerCollision(projectileActor.Projectile, Player);
                }
            }
        }
    }
}