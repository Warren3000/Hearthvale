using Hearthvale.GameCode.Collision;
using Microsoft.Xna.Framework;
using MonoGame.Extended;

namespace Hearthvale.GameCode.Managers
{
    /// <summary>
    /// Collision actor wrapping a DungeonLoot chest.
    /// Bounds kept in sync each frame by the chest.
    /// </summary>
    public sealed class ChestCollisionActor : ICollisionActor
    {
        private readonly DungeonLoot _loot;
        public IShapeF Bounds { get; set; }

        public ChestCollisionActor(DungeonLoot loot, Rectangle initialBounds)
        {
            _loot = loot;
            Bounds = new RectangleF(initialBounds.X, initialBounds.Y, initialBounds.Width, initialBounds.Height);
        }

        public RectangleF CalculateInitialBounds()
        {
            if (Bounds is RectangleF r) return r;
            return new RectangleF(_loot.Bounds.X, _loot.Bounds.Y, _loot.Bounds.Width, _loot.Bounds.Height);
        }

        public void OnCollision(CollisionEventArgs collisionInfo)
        {
            // Optional: could trigger auto-open on player collision, etc.
        }

        public void UpdateFrom(Rectangle tightWorldRect)
        {
            Bounds = new RectangleF(tightWorldRect.X, tightWorldRect.Y, tightWorldRect.Width, tightWorldRect.Height);
        }
    }
}