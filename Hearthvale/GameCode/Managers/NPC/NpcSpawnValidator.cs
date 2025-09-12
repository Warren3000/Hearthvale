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
        private readonly CollisionWorld _collisionWorld; // Changed from CollisionWorldManager
        private readonly float _defaultMinDistanceFromPlayer;
        private readonly float _defaultMinDistanceBetweenNpcs;

        public NpcSpawnValidator(
            Rectangle bounds,
            CollisionWorld collisionWorld, // Changed parameter type
            float defaultMinDistanceFromPlayer = 48f,
            float defaultMinDistanceBetweenNpcs = 32f)
        {
            _bounds = bounds;
            _collisionWorld = collisionWorld; // Updated assignment
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

            // Check distance from player using Bounds center
            if (player != null)
            {
                Vector2 playerCenter = new Vector2(player.Bounds.Center.X, player.Bounds.Center.Y);
                Vector2 spawnCenter = new Vector2(position.X + 16, position.Y + 16); // Assume 32x32 NPC
                if (Vector2.Distance(spawnCenter, playerCenter) < minDistanceFromPlayer.Value)
                    return false;
            }

            // Check distance from other NPCs using Bounds center
            if (existingNpcs != null)
            {
                Vector2 spawnCenter = new Vector2(position.X + 16, position.Y + 16);
                foreach (var npc in existingNpcs)
                {
                    Vector2 npcCenter = new Vector2(npc.Bounds.Center.X, npc.Bounds.Center.Y);
                    if (Vector2.Distance(spawnCenter, npcCenter) < minDistanceBetweenNpcs.Value)
                        return false;
                }
            }

            // Rest of the validation logic
            var validPosition = GetValidSpawnPosition(position);
            if (validPosition != position)
                position = validPosition;

            var npcBounds = CreateNpcBounds(position);
            
            // Use CollisionWorld instead of CollisionWorldManager
            if (_collisionWorld != null)
            {
                var candidates = _collisionWorld.GetActorsInBounds(npcBounds);
                foreach (var actor in candidates)
                {
                    // Check for blocking actors (walls, chests, other characters)
                    if (actor is WallCollisionActor || actor is ChestCollisionActor ||
                        actor is PlayerCollisionActor || actor is NpcCollisionActor)
                    {
                        if (actor.Bounds.BoundingRectangle.Intersects(new Rectangle((int)npcBounds.X, (int)npcBounds.Y, (int)npcBounds.Width, (int)npcBounds.Height)))
                            return false;
                    }
                }
            }
            
            return true;
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