using Hearthvale.GameCode.Entities;
using Hearthvale.GameCode.Entities.NPCs;
using Hearthvale.GameCode.Entities.Players;
using Hearthvale.GameCode.Collision;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Tiled;
using MonoGameLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using MonoGame.Extended;

namespace Hearthvale.GameCode.Managers
{
    /// <summary>
    /// Manages NPC lifecycle and coordinates with specialized managers
    /// </summary>
    public class NpcManager : IDisposable
    {
        private readonly List<NPC> _npcs = new();
        private readonly Rectangle _bounds;
        private readonly NpcSpawner _npcSpawner;
        private readonly CollisionWorldManager _collisionManager;
        private readonly NpcSpawnValidator _spawnValidator;
        private readonly NpcUpdateCoordinator _updateCoordinator;
        private bool _disposed = false;

        public IEnumerable<NPC> Npcs => _npcs;
        public IEnumerable<Character> Characters => _npcs;
        public CollisionWorldManager CollisionManager => _collisionManager;

        public NpcManager(
            TextureAtlas heroAtlas,
            Rectangle bounds,
            Tilemap tilemap,
            WeaponManager weaponManager,
            TextureAtlas weaponAtlas,
            TextureAtlas arrowAtlas)
        {
            _bounds = bounds;
            _collisionManager = new CollisionWorldManager(bounds, tilemap);
            _spawnValidator = new NpcSpawnValidator(bounds, _collisionManager);
            _updateCoordinator = new NpcUpdateCoordinator(_collisionManager);
            _npcSpawner = new NpcSpawner(heroAtlas, weaponAtlas, arrowAtlas, weaponManager);
        }

        public void LoadNPCs(IEnumerable<TiledMapObject> npcObjects)
        {
            if (npcObjects == null) return;

            foreach (var obj in npcObjects.Where(o => o.Type == "NPC"))
            {
                string npcType = obj.Name.ToLower();
                Vector2 position = new Vector2(obj.Position.X, obj.Position.Y);
                SpawnNPC(npcType, position);
            }
        }

        public void SpawnNPC(string npcType, Vector2 position, Player player = null, float minDistanceFromPlayer = 48f)
        {
            if (!_spawnValidator.IsValidSpawnPosition(npcType, position, player, _npcs, minDistanceFromPlayer))
            {
                System.Diagnostics.Debug.WriteLine($"Cannot spawn {npcType} at {position} - validation failed");
                return;
            }

            Vector2 spawnPos = _spawnValidator.GetValidSpawnPosition(position);
            var npc = _npcSpawner.CreateNPC(npcType, spawnPos, _bounds, null);

            if (npc == null)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to create NPC of type {npcType}");
                return;
            }

            // Set NPC tilemap for collision detection
            npc.SetTilemap(_collisionManager.Tilemap);

            _collisionManager.RegisterNpc(npc);
            _npcs.Add(npc);

            System.Diagnostics.Debug.WriteLine($"Successfully spawned {npcType} at {spawnPos}");
        }

        /// <summary>
        /// Spawns a random NPC around the player at a safe distance
        /// </summary>
        public void SpawnRandomNpcAroundPlayer(Player player)
        {
            if (player?.EquippedWeapon == null)
            {
                System.Diagnostics.Debug.WriteLine("Cannot spawn NPC: Player or weapon is null");
                return;
            }

            // Calculate spawn position around player
            float swingRadius = player.EquippedWeapon.Length;
            float minDistance = swingRadius + 16f;
            var random = new Random();
            double angle = random.NextDouble() * Math.PI * 2;
            Vector2 direction = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
            Vector2 spawnPos = player.Position + direction * minDistance;

            // Clamp to bounds
            spawnPos.X = MathHelper.Clamp(spawnPos.X, _bounds.Left, _bounds.Right - 32);
            spawnPos.Y = MathHelper.Clamp(spawnPos.Y, _bounds.Top, _bounds.Bottom - 32);

            // Get available NPC types and pick one randomly
            var npcTypes = NpcTypes.GetAllTypes();
            string selectedType = npcTypes[random.Next(npcTypes.Length)];

            // Spawn the NPC with player distance validation
            SpawnNPC(selectedType, spawnPos, player);

            System.Diagnostics.Debug.WriteLine($"Spawned {selectedType} at {spawnPos}");
        }

        public void SpawnAllNpcTypesTest(Player player = null)
        {
            var spawner = new TestNpcSpawner(_collisionManager.CollisionWorld, 48f, 32f);
            var spawnPositions = spawner.GetValidSpawnPositions(player, _npcs);

            var npcTypes = NpcTypes.GetAllTypes();
            int spawned = 0;

            foreach (var pos in spawnPositions)
            {
                if (spawned >= npcTypes.Length) break;

                if (spawner.CanSpawnAt(pos, player, _npcs))
                {
                    SpawnNPC(npcTypes[spawned], pos);
                    spawned++;
                }
            }

            System.Diagnostics.Debug.WriteLine($"Successfully spawned {spawned} NPCs out of {npcTypes.Length} types using Aether collision detection");
        }

        public void Update(GameTime gameTime, Character player, List<Rectangle> rectangles)
        {
            _collisionManager.Update(gameTime);

            if (player != null)
            {
                _collisionManager.UpdatePlayerPosition(player);
            }

            // Update NPCs
            foreach (var npc in _npcs.ToList())
            {
                if (!npc.IsDefeated)
                {
                    // Update obstacles for each NPC before updating it
                    npc.UpdateObstacles(rectangles, _npcs, player);

                    // Let the coordinator handle the update flow
                    _updateCoordinator.UpdateNpc(npc, gameTime, player, _npcs);
                }
            }

            RemoveDefeatedNpcs();
        }

        public void Draw(SpriteBatch spriteBatch, GameUIManager uiManager)
        {
            foreach (var npc in _npcs)
            {
                npc.Draw(spriteBatch);
                DrawNpcHealthBar(spriteBatch, uiManager, npc);
            }
        }

        #region Delegation Methods

        public void RegisterPlayer(Character player) => _collisionManager.RegisterPlayer(player);
        public void RegisterProjectile(Projectile projectile) => _collisionManager.RegisterProjectile(projectile);
        public void UnregisterProjectile(Projectile projectile) => _collisionManager.UnregisterProjectile(projectile);
        public bool CanMoveTo(Vector2 currentPosition, Vector2 newPosition, RectangleF entityBounds) =>
            _collisionManager.CanMoveTo(currentPosition, newPosition, entityBounds);

        #endregion

        private void RemoveDefeatedNpcs()
        {
            for (int i = _npcs.Count - 1; i >= 0; i--)
            {
                if (_npcs[i].IsReadyToRemove)
                {
                    _collisionManager.UnregisterNpc(_npcs[i]);
                    _npcs.RemoveAt(i);
                }
            }
        }

        private void DrawNpcHealthBar(SpriteBatch spriteBatch, GameUIManager uiManager, NPC npc)
        {
            Vector2 barOffset = new Vector2(npc.Sprite.Width / 4, -10);
            Vector2 barSize = new Vector2(npc.Sprite.Width / 2, 6);
            uiManager.DrawNpcHealthBar(spriteBatch, npc, barOffset, barSize);
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
                _collisionManager?.Dispose();
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Handles test spawning logic for development
    /// </summary>
    internal class TestNpcSpawner
    {
        private readonly CollisionWorld _collisionWorld;
        private readonly float _minDistanceFromPlayer;
        private readonly float _minDistanceBetweenNpcs;

        public TestNpcSpawner(CollisionWorld collisionWorld, float minDistanceFromPlayer, float minDistanceBetweenNpcs)
        {
            _collisionWorld = collisionWorld;
            _minDistanceFromPlayer = minDistanceFromPlayer;
            _minDistanceBetweenNpcs = minDistanceBetweenNpcs;
        }

        public List<Vector2> GetValidSpawnPositions(Player player, List<NPC> existingNpcs)
        {
            var validPositions = new List<Vector2>();

            // Generate positions in a grid pattern for testing
            for (int x = 100; x < 1000; x += 100)
            {
                for (int y = 100; y < 800; y += 100)
                {
                    Vector2 pos = new Vector2(x, y);
                    var testBounds = new RectangleF(pos.X + 8, pos.Y + 16, 16, 16);

                    if (!IsPositionBlocked(testBounds) && IsValidDistanceFromPlayer(pos, player))
                    {
                        validPositions.Add(pos);
                    }
                }
            }

            // Shuffle positions for random distribution
            var random = new Random();
            return validPositions.OrderBy(x => random.Next()).ToList();
        }

        public bool CanSpawnAt(Vector2 position, Player player, List<NPC> existingNpcs)
        {
            if (!IsValidDistanceFromPlayer(position, player))
                return false;

            return existingNpcs.All(npc => Vector2.Distance(position, npc.Position) >= _minDistanceBetweenNpcs);
        }

        private bool IsPositionBlocked(RectangleF bounds)
        {
            return _collisionWorld.GetActorsInBounds(bounds)
                .Any(actor => actor is WallCollisionActor || actor is NpcCollisionActor);
        }

        private bool IsValidDistanceFromPlayer(Vector2 position, Player player)
        {
            return player == null || Vector2.Distance(position, player.Position) >= _minDistanceFromPlayer;
        }
    }
}