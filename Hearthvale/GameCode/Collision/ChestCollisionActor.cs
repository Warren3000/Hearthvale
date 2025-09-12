using System;
using Microsoft.Xna.Framework;
using MonoGame.Extended;
using Hearthvale.GameCode.Managers.Dungeon;

namespace Hearthvale.GameCode.Collision
{
    /// <summary>
    /// Collision actor representing a static (or mostly static) chest (loot container).
    /// Bounds are kept in sync from the DungeonLoot's current tight/visual bounds.
    /// </summary>
    public class ChestCollisionActor : ICollisionActor
    {
        private Rectangle _initialBounds; // store original in case the world asks for initial shape

        public IShapeF Bounds { get; set; }
        public DungeonLoot Loot { get; }

        public ChestCollisionActor(DungeonLoot loot, Rectangle bounds)
        {
            Loot = loot ?? throw new ArgumentNullException(nameof(loot));
            _initialBounds = bounds;
            Bounds = new RectangleF(bounds.X, bounds.Y, bounds.Width, bounds.Height);
        }

        /// <summary>
        /// Required by ICollisionActor. Returns the initial bounds used when creating the physics body.
        /// </summary>
        public RectangleF CalculateInitialBounds()
        {
            // Use stored initial bounds (safer than current loot bounds if they haven't been synced yet)
            return new RectangleF(_initialBounds.X, _initialBounds.Y, _initialBounds.Width, _initialBounds.Height);
        }

        /// <summary>
        /// Sync collision bounds from the loot's current Bounds rectangle.
        /// Call this if the chest animates or its tight bounds change.
        /// </summary>
        public void SyncFromLoot()
        {
            var b = Loot.Bounds;
            Bounds = new RectangleF(b.X, b.Y, b.Width, b.Height);
        }

        public void OnCollision(CollisionEventArgs collisionInfo)
        {
            // Static chest: no reaction needed. Add logic here if you want interaction triggers.
        }
    }
}