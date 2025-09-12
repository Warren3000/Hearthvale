//using Hearthvale.GameCode.Entities;
//using Hearthvale.GameCode.Entities.NPCs;
//using Hearthvale.GameCode.Managers;
//using MonoGame.Extended;
//using Microsoft.Xna.Framework;

//namespace Hearthvale.GameCode.Collision
//{
//    /// <summary>
//    /// Collision actor for player characters
//    /// </summary>
//    public class PlayerCollisionActor : ICollisionActor
//    {
//        public Character Player { get; }
//        public IShapeF Bounds { get; set; }

//        public PlayerCollisionActor(Character player)
//        {
//            Player = player;
//            UpdateBounds();
//        }

//        public void UpdateBounds()
//        {
//            if (Player != null)
//            {
//                var bounds = Player.Bounds;
//                Bounds = new RectangleF(bounds.X, bounds.Y, bounds.Width, bounds.Height);
//            }
//        }

//        public void OnCollision(CollisionEventArgs collisionInfo)
//        {
//            // Handle player collision response
//            // This could trigger knockback, sound effects, etc.
//        }

//        public RectangleF CalculateInitialBounds()
//        {
//            if (Player != null)
//            {
//                var bounds = Player.Bounds;
//                return new RectangleF(bounds.X, bounds.Y, bounds.Width, bounds.Height);
//            }
//            return RectangleF.Empty;
//        }
//    }

//    /// <summary>
//    /// Collision actor for NPC characters
//    /// </summary>
//    public class NpcCollisionActor : ICollisionActor
//    {
//        public NPC Npc { get; }
//        public IShapeF Bounds { get; set; }

//        public NpcCollisionActor(NPC npc)
//        {
//            Npc = npc;
//            UpdateBounds();
//        }

//        public void UpdateBounds()
//        {
//            if (Npc != null)
//            {
//                var bounds = Npc.Bounds;
//                Bounds = new RectangleF(bounds.X, bounds.Y, bounds.Width, bounds.Height);
//            }
//        }

//        public void OnCollision(CollisionEventArgs collisionInfo)
//        {
//            // Handle NPC collision response through Aether physics
//            if (collisionInfo.Other is ProjectileCollisionActor projectileActor)
//            {
//                if (projectileActor.Projectile.OwnerId == "Player" && !Npc.IsDefeated && projectileActor.Projectile.CanCollide)
//                {
//                    // Notify combat manager of projectile hit
//                    CombatManager.Instance?.HandleProjectileNpcCollision(projectileActor.Projectile, Npc);
//                }



//        public RectangleF CalculateInitialBounds()
//        {
//            if (Npc != null)
//            {
//                var bounds = Npc.Bounds;
//                return new RectangleF(bounds.X, bounds.Y, bounds.Width, bounds.Height);
//            }
//            return RectangleF.Empty;
//        }
//    }

//    /// <summary>
//    /// Collision actor for chest/loot objects
//    /// </summary>
//    public class ChestCollisionActor : ICollisionActor
//    {
//        public DungeonLoot Loot { get; }
//        public IShapeF Bounds { get; set; }

//        public ChestCollisionActor(DungeonLoot loot, Rectangle bounds)
//        {
//            Loot = loot;
//            Bounds = new RectangleF(bounds.X, bounds.Y, bounds.Width, bounds.Height);
//        }

//        public void SyncFromLoot()
//        {
//            if (Loot != null)
//            {
//                var bounds = Loot.Bounds;
//                Bounds = new RectangleF(bounds.X, bounds.Y, bounds.Width, bounds.Height);
//            }
//        }

//        public void OnCollision(CollisionEventArgs collisionInfo)
//        {
//            // Chests don't react to collisions, they just block movement
//            // Interaction is handled separately through UI/input systems
//        }

//        public RectangleF CalculateInitialBounds()
//        {
//            return Bounds?.BoundingRectangle ?? RectangleF.Empty;
//        }
//    }

//    /// <summary>
//    /// Collision actor for projectiles
//    /// </summary>
//    public class ProjectileCollisionActor : ICollisionActor
//    {
//        public Projectile Projectile { get; }
//        public IShapeF Bounds { get; set; }

//        public ProjectileCollisionActor(Projectile projectile)
//        {
//            Projectile = projectile;
//            UpdateBounds();
//        }

//        public void UpdateBounds()
//        {
//            if (Projectile != null)
//            {
//                var bounds = Projectile.BoundingBox;
//                Bounds = new RectangleF(bounds.X, bounds.Y, bounds.Width, bounds.Height);
//            }
//        }

//        public void OnCollision(CollisionEventArgs collisionInfo)
//        {
//            // Handle projectile collision - typically destroys the projectile
//            // and applies damage/effects to the target
//        }

//        public RectangleF CalculateInitialBounds()
//        {
//            if (Projectile != null)
//            {
//                var bounds = Projectile.BoundingBox;
//                return new RectangleF(bounds.X, bounds.Y, bounds.Width, bounds.Height);
//            }
//            return RectangleF.Empty;
//        }
//    }
//}