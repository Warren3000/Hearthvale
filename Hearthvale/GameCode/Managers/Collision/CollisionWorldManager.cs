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
            var playerCollider = new PlayerCollisionActor(player, playerBounds);
            _collisionWorld.AddActor(playerCollider);
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

            var npcBounds = CreateEntityBounds(npc.Position);
            var npcCollider = new NpcCollisionActor(npc, npcBounds);
            _collisionWorld.AddActor(npcCollider);
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

        public bool IsPositionBlocked(RectangleF bounds)
        {
            return _collisionWorld.GetActorsInBounds(bounds)
                .Any(actor => actor is WallCollisionActor || actor is NpcCollisionActor);
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