using MonoGame.Extended;
using Microsoft.Xna.Framework;
using Hearthvale.GameCode.Entities;
using Hearthvale.GameCode.Entities.NPCs;
using Hearthvale.GameCode.Managers;

namespace Hearthvale.GameCode.Collision
{
    /// <summary>
    /// Collision actor for static wall tiles
    /// </summary>
    public class WallCollisionActor(RectangleF bounds) : ICollisionActor
    {
        public IShapeF Bounds { get; set; } = bounds;

        public void OnCollision(CollisionEventArgs collisionInfo)
        {
            // Walls don't react to collisions, but they block movement
        }

        public RectangleF CalculateInitialBounds()
        {
            // The bounds are known at construction time.
            return (RectangleF)Bounds;
        }
    }
    /// <summary>
    /// Collision actor for NPCs
    /// </summary>
    public class NpcCollisionActor(NPC npc) : ICollisionActor
    {
        // Make bounds property dynamically return orientation-aware bounds
        public IShapeF Bounds
        {
            get
            {
                // Use the NPC's Bounds property directly instead of GetTightSpriteBounds
                Rectangle bounds = Npc.Bounds;
                return new RectangleF(bounds.X, bounds.Y, bounds.Width, bounds.Height);
            }
            set
            {
                // Setter required by interface, but we ignore it since we use dynamic bounds
            }
        }

        public NPC Npc { get; } = npc;

        public void OnCollision(CollisionEventArgs collisionInfo)
        {
            // Handle NPC collision response through Aether physics
            if (collisionInfo.Other is ProjectileCollisionActor projectileActor)
            {
                if (projectileActor.Projectile.OwnerId == "Player" && !Npc.IsDefeated && projectileActor.Projectile.CanCollide)
                {
                    // Notify combat manager of projectile hit
                    CombatManager.Instance?.HandleProjectileNpcCollision(projectileActor.Projectile, Npc);
                }
            }
        }

        public RectangleF CalculateInitialBounds()
        {
            // Use the NPC's Bounds property directly
            Rectangle bounds = Npc.Bounds;
            return new RectangleF(bounds.X, bounds.Y, bounds.Width, bounds.Height);
        }
    }
    /// <summary>
    /// Collision actor for projectiles
    /// </summary>
    public class ProjectileCollisionActor(Projectile projectile) : ICollisionActor
    {
        public IShapeF Bounds { get; set; } = projectile.Bounds;
        public Projectile Projectile { get; } = projectile;

        public void OnCollision(CollisionEventArgs collisionInfo)
        {
            // Forward collision to the projectile
            Projectile.OnCollision(collisionInfo);
        }

        public RectangleF CalculateInitialBounds()
        {
            // The projectile's bounds are set on creation.
            return (RectangleF)Projectile.Bounds;
        }
    }
}