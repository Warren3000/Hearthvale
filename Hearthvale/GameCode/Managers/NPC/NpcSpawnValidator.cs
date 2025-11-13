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
        private readonly CollisionWorld _collisionWorld;
        private readonly float _defaultMinDistanceFromPlayer;
        private readonly float _defaultMinDistanceBetweenNpcs;
        private const float SpawnBoundsHalfWidth = 6f;
        private const float SpawnBoundsHalfHeight = 6f;

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
                Vector2 spawnCenter = GetSpawnCenter(position);
                if (Vector2.Distance(spawnCenter, playerCenter) < minDistanceFromPlayer.Value)
                    return false;
            }

            // Check distance from other NPCs using Bounds center
            if (existingNpcs != null)
            {
                Vector2 spawnCenter = GetSpawnCenter(position);
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
                        if (actor.Bounds.BoundingRectangle.Intersects(npcBounds))
                            return false;
                    }
                }
            }
            
            return true;
        }

        public Vector2 GetValidSpawnPosition(Vector2 position)
        {
            float clampedX = MathHelper.Clamp(position.X, _bounds.Left + SpawnBoundsHalfWidth, _bounds.Right - SpawnBoundsHalfWidth);
            float clampedY = MathHelper.Clamp(position.Y, _bounds.Top + SpawnBoundsHalfHeight, _bounds.Bottom - SpawnBoundsHalfHeight);
            return new Vector2(clampedX, clampedY);
        }

        public RectangleF CreateNpcBounds(Vector2 position)
        {
            float left = position.X - SpawnBoundsHalfWidth;
            float top = position.Y - SpawnBoundsHalfHeight;
            float width = SpawnBoundsHalfWidth * 2f;
            float height = SpawnBoundsHalfHeight * 2f;
            return new RectangleF(left, top, width, height);
        }

        private static Vector2 GetSpawnCenter(Vector2 position) => position;
    }
}