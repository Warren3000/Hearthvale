using MonoGame.Extended;
using Microsoft.Xna.Framework;
using Hearthvale.GameCode.Managers;
using Hearthvale.GameCode.Entities;


namespace Hearthvale.GameCode.Collision
{
    /// <summary>
    /// Collision actor for the player character
    /// </summary>
    public class PlayerCollisionActor(Character player) : ICollisionActor
    {
        // Make bounds property dynamically return orientation-aware bounds
        public IShapeF Bounds 
        { 
            get 
            {
                // Always return current tight sprite bounds
                Rectangle tightBounds = Player.GetTightSpriteBounds();
                return new RectangleF(tightBounds.X, tightBounds.Y, 
                                     tightBounds.Width, tightBounds.Height);
            }
            set 
            {
                // Setter required by interface, but we ignore it since we use dynamic bounds
            } 
        }

        public Character Player { get; } = player;

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
        public RectangleF CalculateInitialBounds()
        {
            // The projectile's bounds are set on creation.
            return (RectangleF)Player.Bounds;
        }
    }
}