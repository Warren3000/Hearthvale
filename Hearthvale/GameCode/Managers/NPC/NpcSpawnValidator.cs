using Hearthvale.GameCode.Entities.NPCs;
using Hearthvale.GameCode.Entities.Players;
using Hearthvale.GameCode.Collision;
using Microsoft.Xna.Framework;
using MonoGame.Extended;
using System.Collections.Generic;
using System.Linq;

namespace Hearthvale.GameCode.Managers
{
    /// <summary>
    /// Validates NPC spawn positions and constraints
    /// </summary>
    public class NpcSpawnValidator
    {
        private readonly Rectangle _bounds;
        private readonly CollisionWorldManager _collisionManager;
        private readonly float _defaultMinDistanceFromPlayer;
        private readonly float _defaultMinDistanceBetweenNpcs;

        public NpcSpawnValidator(
            Rectangle bounds,
            CollisionWorldManager collisionManager,
            float defaultMinDistanceFromPlayer = 48f,
            float defaultMinDistanceBetweenNpcs = 32f)
        {
            _bounds = bounds;
            _collisionManager = collisionManager;
            _defaultMinDistanceFromPlayer = defaultMinDistanceFromPlayer;
            _defaultMinDistanceBetweenNpcs = defaultMinDistanceBetweenNpcs;
        }

        public bool IsValidSpawnPosition(
            string npcType,
            Vector2 position,
            Player player = null,
            IEnumerable<NPC> existingNpcs = null,
            float? minDistanceFromPlayer = null,
            float? minDistanceBetweenNpcs = null)
        {
            if (string.IsNullOrEmpty(npcType))
                return false;

            minDistanceFromPlayer ??= _defaultMinDistanceFromPlayer;
            minDistanceBetweenNpcs ??= _defaultMinDistanceBetweenNpcs;

            // Check distance from player
            if (player != null && Vector2.Distance(position, player.Position) < minDistanceFromPlayer.Value)
                return false;

            // Check distance from other NPCs
            if (existingNpcs != null)
            {
                foreach (var npc in existingNpcs)
                {
                    if (Vector2.Distance(position, npc.Position) < minDistanceBetweenNpcs.Value)
                        return false;
                }
            }

            // Check if position is within bounds
            var validPosition = GetValidSpawnPosition(position);
            if (validPosition != position)
                position = validPosition;

            // Check collision
            var npcBounds = CreateNpcBounds(position);
            return !_collisionManager.IsPositionBlocked(npcBounds);
        }

        public Vector2 GetValidSpawnPosition(Vector2 position)
        {
            float clampedX = MathHelper.Clamp(position.X, _bounds.Left, _bounds.Right - 32);
            float clampedY = MathHelper.Clamp(position.Y, _bounds.Top, _bounds.Bottom - 32);
            return new Vector2(clampedX, clampedY);
        }

        public RectangleF CreateNpcBounds(Vector2 position)
        {
            return new RectangleF(position.X + 8, position.Y + 16, 16, 16);
        }
    }
}