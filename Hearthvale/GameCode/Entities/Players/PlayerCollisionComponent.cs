using Hearthvale.GameCode.Entities.NPCs;
using Hearthvale.GameCode.Entities.Players;
using Hearthvale.GameCode.Managers;
using Hearthvale.GameCode.Utils;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Hearthvale.GameCode.Entities.Components
{
    public class PlayerCollisionComponent
    {
        private readonly Player _player;
        

        public PlayerCollisionComponent(Player player)
        {
            _player = player;
        }

        public bool IsPositionBlocked(Vector2 position, IEnumerable<Rectangle> obstacles)
        {
            // Check if candidate position would collide with any obstacle
            Rectangle candidateBounds = new Rectangle(
                (int)position.X + 8,
                (int)position.Y + 16,
                (int)_player.Bounds.Width / 2,
                (int)_player.Bounds.Height / 2
            );

            foreach (var obstacle in obstacles)
            {
                if (candidateBounds.Intersects(obstacle))
                    return true;
            }

            // Tilemap-based wall checks removed (migrated to physics collision system).
            // Any wall collisions should now be handled via the unified CollisionWorld / actors.

            return false;
        }
    }
}