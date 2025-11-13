using Hearthvale.GameCode.Entities;
using Hearthvale.GameCode.Entities.NPCs;
using Hearthvale.GameCode.Managers;
using MonoGame.Extended;
using Microsoft.Xna.Framework;

namespace Hearthvale.GameCode.Collision
{
    /// <summary>
    /// Collision actor for player characters
    /// </summary>
    public class CollisionActor : ICollisionActor
    {
        public Character Player { get; }
        public IShapeF Bounds { get; set; }

        public CollisionActor(Character player)
        {
            Player = player;
            UpdateBounds();
        }

        public void UpdateBounds()
        {
            if (Player != null)
            {
                var bounds = Player.Bounds;
                Bounds = new RectangleF(bounds.X, bounds.Y, bounds.Width, bounds.Height);
            }
        }

        public void OnCollision(CollisionEventArgs collisionInfo)
        {
            // Handle player collision response
            // This could trigger knockback, sound effects, etc.
        }

        public RectangleF CalculateInitialBounds()
        {
            if (Player != null)
            {
                var bounds = Player.Bounds;
                return new RectangleF(bounds.X, bounds.Y, bounds.Width, bounds.Height);
            }
            return RectangleF.Empty;
        }
    }

    /// <summary>
    /// Collision actor for NPC characters
    /// </summary>
    public class NpcCollisionActor : IDynamicCollisionActor
    {
        public NPC Npc { get; }
        private RectangleF _boundsCache;

        public IShapeF Bounds
        {
            get => _boundsCache;
            set => _boundsCache = value switch
            {
                RectangleF rect => rect,
                null => RectangleF.Empty,
                _ => value.BoundingRectangle
            };
        }

        public NpcCollisionActor(NPC npc)
        {
            Npc = npc;
            _boundsCache = GetCurrentBounds();
        }

        public void OnCollision(CollisionEventArgs collisionInfo)
        {
            // Handle NPC collision response
            // This could trigger AI reactions, damage, etc.
        }

        public RectangleF CalculateInitialBounds()
        {
            return GetCurrentBounds();
        }

        public RectangleF GetCurrentBounds()
        {
            if (Npc == null)
            {
                return RectangleF.Empty;
            }

            var bounds = Npc.GetSpriteBoundsAt(Npc.Position);
            var rectF = new RectangleF(bounds.X, bounds.Y, bounds.Width, bounds.Height);
            _boundsCache = rectF;
            return rectF;
        }
    }

    ///// <summary>
    ///// Collision actor for chest/loot objects
    ///// </summary>
    //public class ChestCollisionActor : ICollisionActor
    //{
    //    public DungeonLoot Loot { get; }
    //    public IShapeF Bounds { get; set; }

    //    public ChestCollisionActor(DungeonLoot loot, Rectangle bounds)
    //    {
    //        Loot = loot;
    //        Bounds = new RectangleF(bounds.X, bounds.Y, bounds.Width, bounds.Height);
    //    }

    //    public void SyncFromLoot()
    //    {
    //        if (Loot != null)
    //        {
    //            var bounds = Loot.Bounds;
    //            Bounds = new RectangleF(bounds.X, bounds.Y, bounds.Width, bounds.Height);
    //        }
    //    }

    //    public void OnCollision(CollisionEventArgs collisionInfo)
    //    {
    //        // Chests don't react to collisions, they just block movement
    //        // Interaction is handled separately through UI/input systems
    //    }

    //    public RectangleF CalculateInitialBounds()
    //    {
    //        return Bounds?.BoundingRectangle ?? RectangleF.Empty;
    //    }
    //}

    /// <summary>
    /// Collision actor for projectiles
    /// </summary>
    public class ProjectileCollisionActor : ICollisionActor
    {
        public Projectile Projectile { get; }
        public IShapeF Bounds { get; set; }

        public ProjectileCollisionActor(Projectile projectile)
        {
            Projectile = projectile;
            UpdateBounds();
        }

        public void UpdateBounds()
        {
            if (Projectile != null)
            {
                var bounds = Projectile.BoundingBox;
                Bounds = new RectangleF(bounds.X, bounds.Y, bounds.Width, bounds.Height);
            }
        }

        public void OnCollision(CollisionEventArgs collisionInfo)
        {
            // Handle projectile collision - typically destroys the projectile
            // and applies damage/effects to the target
        }

        public RectangleF CalculateInitialBounds()
        {
            if (Projectile != null)
            {
                var bounds = Projectile.BoundingBox;
                return new RectangleF(bounds.X, bounds.Y, bounds.Width, bounds.Height);
            }
            return RectangleF.Empty;
        }
    }
}