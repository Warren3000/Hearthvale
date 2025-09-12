using MonoGame.Extended;

namespace Hearthvale.GameCode.Collision
{
    /// <summary>
    /// Collision actor for static wall tiles in the physics-based collision system
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
            // Walls don't react to collisions, they just block movement
            // The collision response is handled by the moving character
        }

        public RectangleF CalculateInitialBounds()
        {
            return Bounds?.BoundingRectangle ?? RectangleF.Empty;
        }
    }
}