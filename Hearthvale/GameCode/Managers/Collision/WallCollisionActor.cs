using MonoGame.Extended;

namespace Hearthvale.GameCode.Collision
{
    /// <summary>
    /// Collision actor for static wall tiles in the physics-based collision system
    /// </summary>
    public class WallCollisionActor : ICollisionActor
    {
        private readonly RectangleF _staticBounds;
        
        public IShapeF Bounds 
        { 
            get => _staticBounds;
            set 
            {
                // Ignore physics system bounds updates for walls - they should remain static
                // This ensures walls maintain exact tile alignment
            }
        }

        public WallCollisionActor(RectangleF bounds)
        {
            _staticBounds = bounds;
        }

        public void OnCollision(CollisionEventArgs collisionInfo)
        {
            // Walls don't react to collisions, they just block movement
            // The collision response is handled by the moving character
        }

        public RectangleF CalculateInitialBounds()
        {
            return _staticBounds;
        }
    }
}