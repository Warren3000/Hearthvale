using Hearthvale.GameCode.Entities;
using Hearthvale.GameCode.Entities.NPCs;
using Hearthvale.GameCode.Managers;
using Hearthvale.GameCode.Utils;
using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MonoGame.Extended.Tiled;
using MonoGameLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hearthvale.GameCode.Collision
{
    /// <summary>
    /// Manages the collision world and all collision-related operations
    /// </summary>
    public class CollisionWorldManager : IDisposable
    {
        private readonly CollisionWorld _collisionWorld;
        private readonly Tilemap _tilemap;
        private bool _disposed = false;

        public Tilemap Tilemap => _tilemap;
        public CollisionWorld CollisionWorld => _collisionWorld;

        public CollisionWorldManager(Rectangle bounds, Tilemap tilemap)
        {
            _collisionWorld = new CollisionWorld(new RectangleF(bounds.X, bounds.Y, bounds.Width, bounds.Height));
            _tilemap = tilemap;
            InitializeWallColliders();
        }

        public void Update(GameTime gameTime)
        {
            _collisionWorld.Update(gameTime);
        }

        public void RegisterPlayer(Character player)
        {
            if (player == null) return;

            var playerBounds = CreateEntityBounds(player.Position);
            var playerCollider = new PlayerCollisionActor(player);
            _collisionWorld.AddActor(playerCollider);

            // ensure character movement queries this world (for chests)
            player.CollisionComponent?.SetCollisionWorld(_collisionWorld);
        }

        public void UpdatePlayerPosition(Character player)
        {
            if (player == null) return;

            var playerActor = _collisionWorld.GetActorsOfType<PlayerCollisionActor>()
                .FirstOrDefault(actor => actor.Player == player);

            if (playerActor != null)
            {
                _collisionWorld.UpdateActorPosition(playerActor, player.Position);
            }
        }

        public void RegisterNpc(NPC npc)
        {
            if (npc == null) return;

            var npcCollider = new NpcCollisionActor(npc);
            _collisionWorld.AddActor(npcCollider);

            // ensure NPC movement queries this world (for chests)
            npc.CollisionComponent?.SetCollisionWorld(_collisionWorld);
        }

        public void UpdateNpcPosition(NPC npc)
        {
            var npcActor = _collisionWorld.GetActorsOfType<NpcCollisionActor>()
                .FirstOrDefault(actor => actor.Npc == npc);

            if (npcActor != null)
            {
                _collisionWorld.UpdateActorPosition(npcActor, npc.Position);
            }
        }

        public void UnregisterNpc(NPC npc)
        {
            var actorsToRemove = _collisionWorld.GetActorsOfType<NpcCollisionActor>()
                .Where(actor => actor.Npc == npc)
                .ToList();

            foreach (var actor in actorsToRemove)
            {
                _collisionWorld.RemoveActor(actor);
            }
        }

        public void RegisterProjectile(Projectile projectile)
        {
            if (projectile == null) return;

            projectile.SetCollisionWorld(_collisionWorld);
            var projectileCollider = new ProjectileCollisionActor(projectile);
            _collisionWorld.AddActor(projectileCollider);
        }

        public void UnregisterProjectile(Projectile projectile)
        {
            if (projectile == null) return;

            var colliderToRemove = _collisionWorld.GetActorsOfType<ProjectileCollisionActor>()
                .FirstOrDefault(c => c.Projectile == projectile);

            if (colliderToRemove != null)
            {
                _collisionWorld.RemoveActor(colliderToRemove);
            }
        }

        // ---------- Chest support ----------

        public void RegisterChest(DungeonLoot loot)
        {
            if (loot == null) return;

            // Avoid duplicate registration
            if (_collisionWorld.GetActorsOfType<ChestCollisionActor>().Any(c => c.Loot == loot))
                return;

            var rect = loot.Bounds;
            if (rect.Width <= 0 || rect.Height <= 0)
                return;

            var spawnArea = new RectangleF(rect.X, rect.Y, rect.Width, rect.Height);
            ResolveDynamicOverlap(spawnArea);

            var chestActor = new ChestCollisionActor(loot, rect);

            // IMPORTANT: seed bounds so a physics body is created
            chestActor.Bounds = new RectangleF(rect.X, rect.Y, rect.Width, rect.Height);

            _collisionWorld.AddActor(chestActor);

            // Ensure body starts at the correct place (center-based in Aether)
            var center = new Vector2(rect.Center.X, rect.Center.Y);
            _collisionWorld.UpdateActorPosition(chestActor, center);
        }

        public void RegisterChests(IEnumerable<DungeonLoot> loots)
        {
            if (loots == null) return;
            foreach (var l in loots)
                RegisterChest(l);
        }

        public void SyncChestPositions()
        {
            foreach (var chest in _collisionWorld.GetActorsOfType<ChestCollisionActor>())
            {
                // Keep actor state in sync with loot
                chest.SyncFromLoot();

                // Move the physics body to the loot's current center
                var rect = chest.Loot.Bounds;
                if (rect.Width > 0 && rect.Height > 0)
                {
                    var center = new Vector2(rect.Center.X, rect.Center.Y);
                    _collisionWorld.UpdateActorPosition(chest, center);
                }
            }
        }

        public void UnregisterChest(DungeonLoot loot)
        {
            var toRemove = _collisionWorld.GetActorsOfType<ChestCollisionActor>()
                .FirstOrDefault(c => c.Loot == loot);
            if (toRemove != null)
                _collisionWorld.RemoveActor(toRemove);
        }

        // ----------------------------------------

        public bool IsPositionBlocked(RectangleF bounds)
        {
            return _collisionWorld.GetActorsInBounds(bounds)
                .Any(IsBlockingActor);
        }

        public bool CanMoveTo(Vector2 currentPosition, Vector2 newPosition, RectangleF entityBounds)
        {
            var newBounds = new RectangleF(
                newPosition.X + entityBounds.X - currentPosition.X,
                newPosition.Y + entityBounds.Y - currentPosition.Y,
                entityBounds.Width,
                entityBounds.Height);

            return !IsPositionBlocked(newBounds);
        }

        public List<Rectangle> GetNearbyWalls(Vector2 position)
        {
            var bounds = CreateEntityBounds(position);
            return _collisionWorld.GetActorsInBounds(bounds)
                .OfType<WallCollisionActor>()
                .Select(w => ConvertShapeToRectangle(w.Bounds))
                .ToList();
        }

        private void InitializeWallColliders()
        {
            if (_tilemap == null) return;

            for (int row = 0; row < _tilemap.Rows; row++)
            {
                for (int col = 0; col < _tilemap.Columns; col++)
                {
                    if (IsWallTile(col, row))
                    {
                        CreateWallCollider(col, row);
                    }
                }
            }
        }

        private bool IsWallTile(int col, int row)
        {
            var tileTileset = _tilemap.GetTileset(col, row);
            var tileId = _tilemap.GetTileId(col, row);
            return tileTileset == TilesetManager.Instance.WallTileset && AutotileMapper.IsWallTile(tileId);
        }

        private void CreateWallCollider(int col, int row)
        {
            var wallBounds = new RectangleF(
                col * _tilemap.TileWidth,
                row * _tilemap.TileHeight,
                _tilemap.TileWidth,
                _tilemap.TileHeight);

            var wallCollider = new WallCollisionActor(wallBounds);
            _collisionWorld.AddActor(wallCollider);
        }

        private RectangleF CreateEntityBounds(Vector2 position)
        {
            return new RectangleF(position.X + 8, position.Y + 16, 16, 16);
        }

        private Rectangle ConvertShapeToRectangle(IShapeF shape)
        {
            if (shape is RectangleF rect)
            {
                return new Rectangle((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height);
            }

            var bounds = shape.BoundingRectangle;
            return new Rectangle((int)bounds.X, (int)bounds.Y, (int)bounds.Width, (int)bounds.Height);
        }

        private void ResolveDynamicOverlap(RectangleF obstacleBounds)
        {
            var overlapping = _collisionWorld.GetActorsInBounds(obstacleBounds);
            foreach (var actor in overlapping)
            {
                switch (actor)
                {
                    case PlayerCollisionActor playerActor when playerActor.Player is { IsDefeated: false }:
                        PushCharacterOutOfBounds(playerActor.Player, obstacleBounds);
                        break;
                    case NpcCollisionActor npcActor when npcActor.Npc is { IsDefeated: false }:
                        PushCharacterOutOfBounds(npcActor.Npc, obstacleBounds);
                        break;
                }
            }
        }

        private void PushCharacterOutOfBounds(Character character, RectangleF obstacleBounds)
        {
            if (character == null)
            {
                return;
            }

            var currentRect = new RectangleF(character.Bounds.X, character.Bounds.Y, character.Bounds.Width, character.Bounds.Height);
            if (!currentRect.Intersects(obstacleBounds))
            {
                return;
            }

            Vector2 separation = ComputeSeparationVector(currentRect, obstacleBounds);
            if (separation == Vector2.Zero)
            {
                return;
            }

            Vector2 originalPosition = character.Position;
            Vector2 targetPosition = originalPosition + separation;

            bool moved = character.CollisionComponent?.TryMove(targetPosition) ?? false;
            if (!moved)
            {
                character.SetPosition(targetPosition);
            }

            character.CollisionComponent?.CancelKnockbackAlong(separation);

            if (character is NPC npc)
            {
                UpdateNpcPosition(npc);
            }
            else
            {
                UpdatePlayerPosition(character);
            }
        }

        private static Vector2 ComputeSeparationVector(RectangleF dynamicBounds, RectangleF obstacleBounds)
        {
            if (!dynamicBounds.Intersects(obstacleBounds))
            {
                return Vector2.Zero;
            }

            var intersection = RectangleF.Intersection(dynamicBounds, obstacleBounds);
            if (intersection.Width <= 0f || intersection.Height <= 0f)
            {
                return Vector2.Zero;
            }

            const float Padding = 1f;

            if (intersection.Width < intersection.Height)
            {
                float direction = dynamicBounds.Center.X <= obstacleBounds.Center.X ? -1f : 1f;
                float amount = intersection.Width + Padding;
                return new Vector2(direction * amount, 0f);
            }
            else
            {
                float direction = dynamicBounds.Center.Y <= obstacleBounds.Center.Y ? -1f : 1f;
                float amount = intersection.Height + Padding;
                return new Vector2(0f, direction * amount);
            }
        }

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

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _collisionWorld?.Dispose();
                _disposed = true;
            }
        }
    }
}