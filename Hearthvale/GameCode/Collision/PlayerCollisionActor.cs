using MonoGame.Extended;
using Microsoft.Xna.Framework;
using Hearthvale.GameCode.Managers;
using Hearthvale.GameCode.Entities;


namespace Hearthvale.GameCode.Collision
{
    /// <summary>
    /// Collision actor for the player character
    /// </summary>
    public class PlayerCollisionActor : IDynamicCollisionActor
    {
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

        public Character Player { get; }

        public PlayerCollisionActor(Character player)
        {
            Player = player;
            _boundsCache = GetCurrentBounds();
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
        public RectangleF CalculateInitialBounds()
        {
            return GetCurrentBounds();
        }

        public RectangleF GetCurrentBounds()
        {
            if (Player == null)
            {
                return RectangleF.Empty;
            }

            var bounds = Player.GetCollisionBoundsAt(Player.Position);
            var rectF = new RectangleF(bounds.X, bounds.Y, bounds.Width, bounds.Height);
            _boundsCache = rectF;
            return rectF;
        }
    }
}