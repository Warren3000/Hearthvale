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
    public class WallCollisionActor : ICollisionActor
    {
        public IShapeF Bounds { get; set; }

        public WallCollisionActor(RectangleF bounds)
        {
            Bounds = bounds;
        }

        public void OnCollision(CollisionEventArgs collisionInfo)
        {
            // Walls don't react to collisions, but they block movement
        }
    }
    /// <summary>
    /// Collision actor for NPCs
    /// </summary>
    public class NpcCollisionActor : ICollisionActor
    {
        // Make bounds property dynamically return orientation-aware bounds
        public IShapeF Bounds
        {
            get
            {
                // Always return current orientation-aware bounds
                Rectangle orientedBounds = Npc.GetOrientationAwareBounds();
                return new RectangleF(orientedBounds.X, orientedBounds.Y,
                                     orientedBounds.Width, orientedBounds.Height);
            }
            set
            {
                // Setter required by interface, but we ignore it since we use dynamic bounds
            }
        }

        public NPC Npc { get; }

        public NpcCollisionActor(NPC npc)
        {
            Npc = npc;
        }

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
    }
    /// <summary>
    /// Collision actor for projectiles
    /// </summary>
    public class ProjectileCollisionActor : ICollisionActor
    {
        public IShapeF Bounds { get; set; }
        public Projectile Projectile { get; }

        public ProjectileCollisionActor(Projectile projectile)
        {
            Projectile = projectile;
            Bounds = projectile.Bounds;
        }

        public void OnCollision(CollisionEventArgs collisionInfo)
        {
            // Forward collision to the projectile
            Projectile.OnCollision(collisionInfo);
        }
    }
}