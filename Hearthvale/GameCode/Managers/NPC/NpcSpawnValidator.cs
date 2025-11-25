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
        private const float SpawnBoundsHalfWidth = 12f;
        private const float SpawnBoundsHalfHeight = 18f;

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

        public float DefaultMinDistanceFromPlayer => _defaultMinDistanceFromPlayer;
        public float DefaultMinDistanceBetweenNpcs => _defaultMinDistanceBetweenNpcs;

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
            return !IsAreaBlocked(npcBounds);
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

        public bool IsAreaBlocked(RectangleF bounds)
        {
            if (_collisionWorld == null)
            {
                return false;
            }

            var actors = _collisionWorld.GetActorsInBounds(bounds);
            foreach (var actor in actors)
            {
                if (IsBlockingActor(actor))
                {
                    RectangleF actorBounds;
                    if (actor.Bounds is RectangleF rectF)
                    {
                        actorBounds = rectF;
                    }
                    else if (actor.Bounds != null)
                    {
                        var rect = actor.Bounds.BoundingRectangle;
                        actorBounds = new RectangleF(rect.X, rect.Y, rect.Width, rect.Height);
                    }
                    else
                    {
                        actorBounds = RectangleF.Empty;
                    }

                    if (!actorBounds.IsEmpty && actorBounds.Intersects(bounds))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static Vector2 GetSpawnCenter(Vector2 position) => position;

        private static bool IsBlockingActor(ICollisionActor actor)
        {
            switch (actor)
            {
                case WallCollisionActor:
                case ChestCollisionActor:
                    return true;
                case PlayerCollisionActor playerActor:
                    return playerActor.Player is { IsDefeated: false };
                case NpcCollisionActor npcActor:
                    return npcActor.Npc is { IsDefeated: false };
                default:
                    return false;
            }
        }
    }
}