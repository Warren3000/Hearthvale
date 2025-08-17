using MonoGame.Extended;
using Microsoft.Xna.Framework;
using Hearthvale.GameCode.Managers;
using Hearthvale.GameCode.Entities;


namespace Hearthvale.GameCode.Collision
{
    /// <summary>
    /// Collision actor for the player character
    /// </summary>
    public class PlayerCollisionActor : ICollisionActor
    {
        // Make bounds property dynamically return orientation-aware bounds
        public IShapeF Bounds 
        { 
            get 
            {
                // Always return current orientation-aware bounds
                Rectangle orientedBounds = Player.GetOrientationAwareBounds();
                return new RectangleF(orientedBounds.X, orientedBounds.Y, 
                                     orientedBounds.Width, orientedBounds.Height);
            }
            set 
            {
                // Setter required by interface, but we ignore it since we use dynamic bounds
            } 
        }
        
        public Character Player { get; }

        public PlayerCollisionActor(Character player)
        {
            Player = player;
            // No need to store initial bounds - we'll get them dynamically
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