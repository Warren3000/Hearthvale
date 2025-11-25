using Hearthvale.GameCode.Data.Atlases;
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
        private readonly INpcAtlasCatalog _npcAtlasCatalog;
        private bool _disposed = false;

        public IEnumerable<NPC> Npcs => _npcs;
        public IEnumerable<Character> Characters => _npcs;
        public CollisionWorldManager CollisionManager => _collisionManager;

        public NpcManager(
            TextureAtlas heroAtlas,
            Rectangle bounds,
            Tilemap tilemap,
            WeaponManager weaponManager,
            INpcAtlasCatalog npcAtlasCatalog,
            TextureAtlas fallbackNpcAtlas,
            TextureAtlas weaponAtlas,
            TextureAtlas arrowAtlas)
        {
            _bounds = bounds;
            _collisionManager = new CollisionWorldManager(bounds, tilemap);
            _spawnValidator = new NpcSpawnValidator(bounds, _collisionManager.CollisionWorld);
            _updateCoordinator = new NpcUpdateCoordinator(_collisionManager);
            _npcAtlasCatalog = npcAtlasCatalog ?? NullNpcAtlasCatalog.Instance;
            _npcSpawner = new NpcSpawner(heroAtlas, fallbackNpcAtlas, weaponAtlas, arrowAtlas, weaponManager, _npcAtlasCatalog);
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

        public NPC SpawnNPC(string npcType, Vector2 position, Player player = null, float minDistanceFromPlayer = 48f)
        {
            Vector2 desiredPosition = _spawnValidator.GetValidSpawnPosition(position);

            if (!_spawnValidator.IsValidSpawnPosition(npcType, desiredPosition, player, _npcs, minDistanceFromPlayer))
            {
                System.Diagnostics.Debug.WriteLine($"Cannot spawn {npcType} at {desiredPosition} - validation failed");
                return null;
            }

            var npc = _npcSpawner.CreateNPC(npcType, desiredPosition, _bounds, null);
            if (npc == null)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to create NPC of type {npcType}");
                return null;
            }

            npc.SetPosition(desiredPosition);

            if (!TryResolveSpawnPosition(npc, player, desiredPosition, minDistanceFromPlayer, out var resolvedPosition))
            {
                npc.Dispose();
                System.Diagnostics.Debug.WriteLine($"Cannot spawn {npcType} near {desiredPosition} - no free space available");
                return null;
            }

            npc.SetPosition(resolvedPosition);
            _collisionManager.RegisterNpc(npc);
            _npcs.Add(npc);

            System.Diagnostics.Debug.WriteLine($"Successfully spawned {npcType} at {resolvedPosition}");
            return npc;
        }

        /// <summary>
        /// Spawns a specific NPC type around the player at a safe distance
        /// </summary>
        public NPC SpawnNpcAroundPlayer(string npcType, Player player, float minSpawnDistance = -1f, float maxSpawnDistance = -1f)
        {
            if (player?.EquippedWeapon == null)
            {
                System.Diagnostics.Debug.WriteLine("Cannot spawn NPC: Player or weapon is null");
                return null;
            }

            // Calculate spawn position around player
            float minDistance;
            float maxDistance;

            if (minSpawnDistance > 0)
            {
                minDistance = minSpawnDistance;
                maxDistance = maxSpawnDistance > minDistance ? maxSpawnDistance : minDistance + 32f;
            }
            else
            {
                float swingRadius = player.EquippedWeapon.Length;
                minDistance = swingRadius + 16f;
                maxDistance = minDistance + 16f;
            }

            var random = new Random();
            double angle = random.NextDouble() * Math.PI * 2;
            float distance = minDistance + (float)random.NextDouble() * (maxDistance - minDistance);
            
            Vector2 direction = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
            Vector2 spawnPos = player.Position + direction * distance;

            // Clamp to bounds
            spawnPos.X = MathHelper.Clamp(spawnPos.X, _bounds.Left, _bounds.Right - 32);
            spawnPos.Y = MathHelper.Clamp(spawnPos.Y, _bounds.Top, _bounds.Bottom - 32);

            // Spawn the NPC with player distance validation
            return SpawnNPC(npcType, spawnPos, player);
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
            var npcTypes = NpcTypes.GetAllTypes();
            var pendingTypes = new Queue<string>(npcTypes);
            int initialCount = _npcs.Count;

            var spawner = new TestNpcSpawner(
                _collisionManager.CollisionWorld,
                48f,
                32f,
                _spawnValidator.CreateNpcBounds,
                _bounds);

            const int maxAttempts = 3;
            int attempts = 0;

            while (pendingTypes.Count > 0 && attempts < maxAttempts)
            {
                var spawnPositions = spawner.GetValidSpawnPositions(player, _npcs, attempts);

                foreach (var pos in spawnPositions)
                {
                    if (pendingTypes.Count == 0)
                    {
                        break;
                    }

                    if (!spawner.CanSpawnAt(pos, player, _npcs))
                    {
                        continue;
                    }

                    int beforeSpawn = _npcs.Count;
                    var nextType = pendingTypes.Peek();
                    SpawnNPC(nextType, pos);

                    if (_npcs.Count > beforeSpawn)
                    {
                        pendingTypes.Dequeue();
                    }
                }

                attempts++;
            }

            if (pendingTypes.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine($"Test NPC spawner missing {pendingTypes.Count} types after {attempts} attempts: {string.Join(", ", pendingTypes)}");
            }

            int totalSpawned = _npcs.Count - initialCount;
            System.Diagnostics.Debug.WriteLine($"Successfully spawned {totalSpawned} NPCs out of {npcTypes.Length} types using Aether collision detection after {attempts} attempt(s)");
        }

        public bool EnsureCharacterSpawnClear(Character character)
        {
            if (character == null)
            {
                return false;
            }

            Vector2 initialPosition = character.Position;
            var initialBounds = character.GetSpriteBoundsAt(initialPosition);
            var boundsRect = new RectangleF(initialBounds.X, initialBounds.Y, initialBounds.Width, initialBounds.Height);

            if (!_spawnValidator.IsAreaBlocked(boundsRect))
            {
                return true;
            }

            if (TryFindNonBlockingPosition(initialPosition, pos =>
            {
                var rect = character.GetSpriteBoundsAt(pos);
                return new RectangleF(rect.X, rect.Y, rect.Width, rect.Height);
            }, out var resolvedPosition))
            {
                character.SetPosition(resolvedPosition);

                if (character is NPC npc)
                {
                    _collisionManager.UpdateNpcPosition(npc);
                }
                else
                {
                    _collisionManager.UpdatePlayerPosition(character);
                }

                return true;
            }

            return false;
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

        public void ReevaluateEntities(Player player, IReadOnlyList<Rectangle> obstacles)
        {
            if (obstacles == null)
            {
                return;
            }

            foreach (var npc in _npcs)
            {
                npc.UpdateObstacles(obstacles, _npcs, player);
            }
        }

        private bool TryResolveSpawnPosition(NPC npc, Player player, Vector2 desiredPosition, float minDistanceFromPlayer, out Vector2 resolvedPosition)
        {
            foreach (var candidate in EnumerateCandidatePositions(desiredPosition))
            {
                npc.SetPosition(candidate);

                var rect = npc.GetSpriteBoundsAt(candidate);
                var bounds = new RectangleF(rect.X, rect.Y, rect.Width, rect.Height);

                if (_spawnValidator.IsAreaBlocked(bounds))
                {
                    continue;
                }

                if (!MeetsDistanceConstraints(bounds, player, minDistanceFromPlayer))
                {
                    continue;
                }

                resolvedPosition = candidate;
                return true;
            }

            npc.SetPosition(desiredPosition);
            resolvedPosition = default;
            return false;
        }

        private bool MeetsDistanceConstraints(RectangleF spawnBounds, Player player, float minDistanceFromPlayer)
        {
            Vector2 center = new Vector2(spawnBounds.Center.X, spawnBounds.Center.Y);

            if (player != null)
            {
                var playerCenter = new Vector2(player.Bounds.Center.X, player.Bounds.Center.Y);
                float playerSpacing = MathF.Max(minDistanceFromPlayer, _spawnValidator.DefaultMinDistanceFromPlayer);
                if (Vector2.Distance(center, playerCenter) < playerSpacing)
                {
                    return false;
                }
            }

            float minNpcDistance = _spawnValidator.DefaultMinDistanceBetweenNpcs;
            foreach (var existing in _npcs)
            {
                if (existing == null || existing.IsDefeated)
                {
                    continue;
                }

                var npcCenter = new Vector2(existing.Bounds.Center.X, existing.Bounds.Center.Y);
                if (Vector2.Distance(center, npcCenter) < minNpcDistance)
                {
                    return false;
                }
            }

            return true;
        }

        private bool TryFindNonBlockingPosition(Vector2 desiredPosition, Func<Vector2, RectangleF> boundsFactory, out Vector2 resolvedPosition)
        {
            foreach (var candidate in EnumerateCandidatePositions(desiredPosition))
            {
                var bounds = boundsFactory(candidate);
                if (!_spawnValidator.IsAreaBlocked(bounds))
                {
                    resolvedPosition = candidate;
                    return true;
                }
            }

            resolvedPosition = default;
            return false;
        }

        private IEnumerable<Vector2> EnumerateCandidatePositions(Vector2 origin)
        {
            float tileSize = _collisionManager.Tilemap?.TileWidth ?? 32f;
            float step = MathF.Max(8f, tileSize * 0.5f);
            const int maxRings = 6;

            var seen = new HashSet<(int x, int y)>();

            Vector2 first = _spawnValidator.GetValidSpawnPosition(origin);
            if (AddCandidate(first))
            {
                yield return first;
            }

            Vector2[] directions = new[]
            {
                new Vector2(1f, 0f),
                new Vector2(-1f, 0f),
                new Vector2(0f, 1f),
                new Vector2(0f, -1f),
                new Vector2(1f, 1f),
                new Vector2(1f, -1f),
                new Vector2(-1f, 1f),
                new Vector2(-1f, -1f)
            };

            for (int ring = 1; ring <= maxRings; ring++)
            {
                float distance = step * ring;
                foreach (var direction in directions)
                {
                    Vector2 candidate = origin + direction * distance;
                    candidate = _spawnValidator.GetValidSpawnPosition(candidate);
                    if (AddCandidate(candidate))
                    {
                        yield return candidate;
                    }
                }
            }

            bool AddCandidate(Vector2 value)
            {
                var key = ((int)MathF.Round(value.X), (int)MathF.Round(value.Y));
                return seen.Add(key);
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
        private readonly Func<Vector2, RectangleF> _boundsFactory;
        private readonly Rectangle _searchBounds;
        private readonly Random _random = new();

        public TestNpcSpawner(
            CollisionWorld collisionWorld,
            float minDistanceFromPlayer,
            float minDistanceBetweenNpcs,
            Func<Vector2, RectangleF> boundsFactory,
            Rectangle searchBounds)
        {
            _collisionWorld = collisionWorld;
            _minDistanceFromPlayer = minDistanceFromPlayer;
            _minDistanceBetweenNpcs = minDistanceBetweenNpcs;
            _boundsFactory = boundsFactory ?? throw new ArgumentNullException(nameof(boundsFactory));
            _searchBounds = searchBounds;
        }

        public List<Vector2> GetValidSpawnPositions(Player player, List<NPC> existingNpcs, int attempt = 0)
        {
            var validPositions = new List<Vector2>();

            float spacing = MathF.Max(_minDistanceBetweenNpcs * 2f, 80f);
            float offsetX = (_random.NextSingle() * spacing * 0.5f) + (attempt * 23f % spacing);
            float offsetY = (_random.NextSingle() * spacing * 0.5f) + (attempt * 37f % spacing);

            for (float x = _searchBounds.Left + offsetX; x < _searchBounds.Right; x += spacing)
            {
                for (float y = _searchBounds.Top + offsetY; y < _searchBounds.Bottom; y += spacing)
                {
                    Vector2 pos = new Vector2(x, y);
                    var testBounds = _boundsFactory(pos);

                    if (!IsPositionBlocked(testBounds)
                        && IsValidDistanceFromPlayer(testBounds, player)
                        && IsValidDistanceFromExistingNpcs(testBounds, existingNpcs))
                    {
                        validPositions.Add(pos);
                    }
                }
            }

            if (validPositions.Count == 0)
            {
                validPositions.AddRange(SampleRandomPositions(player, existingNpcs, attempt));
            }

            // Shuffle positions for random distribution
            return validPositions.OrderBy(_ => _random.Next()).ToList();
        }

        private IEnumerable<Vector2> SampleRandomPositions(Player player, List<NPC> existingNpcs, int attempt)
        {
            var samples = new List<Vector2>();
            int sampleCount = 32 + attempt * 8;

            for (int i = 0; i < sampleCount; i++)
            {
                float x = _random.Next(_searchBounds.Left, _searchBounds.Right);
                float y = _random.Next(_searchBounds.Top, _searchBounds.Bottom);
                Vector2 pos = new Vector2(x, y);
                var bounds = _boundsFactory(pos);

                if (!IsPositionBlocked(bounds)
                    && IsValidDistanceFromPlayer(bounds, player)
                    && IsValidDistanceFromExistingNpcs(bounds, existingNpcs))
                {
                    samples.Add(pos);
                }
            }

            return samples;
        }

        public bool CanSpawnAt(Vector2 position, Player player, List<NPC> existingNpcs)
        {
            var spawnBounds = _boundsFactory(position);

            if (!IsValidDistanceFromPlayer(spawnBounds, player))
                return false;

            Vector2 spawnCenter = new Vector2(spawnBounds.Center.X, spawnBounds.Center.Y);
            return existingNpcs.All(npc =>
            {
                var npcCenter = new Vector2(npc.Bounds.Center.X, npc.Bounds.Center.Y);
                return Vector2.Distance(spawnCenter, npcCenter) >= _minDistanceBetweenNpcs;
            });
        }

        private bool IsPositionBlocked(RectangleF bounds)
        {
            return _collisionWorld.GetActorsInBounds(bounds)
                .Any(IsBlockingActor);
        }

        private bool IsValidDistanceFromPlayer(RectangleF spawnBounds, Player player)
        {
            if (player == null)
            {
                return true;
            }

            var spawnCenter = new Vector2(spawnBounds.Center.X, spawnBounds.Center.Y);
            var playerCenter = new Vector2(player.Bounds.Center.X, player.Bounds.Center.Y);
            return Vector2.Distance(spawnCenter, playerCenter) >= _minDistanceFromPlayer;
        }

        private bool IsValidDistanceFromExistingNpcs(RectangleF spawnBounds, IEnumerable<NPC> existingNpcs)
        {
            if (existingNpcs == null)
            {
                return true;
            }

            var spawnCenter = new Vector2(spawnBounds.Center.X, spawnBounds.Center.Y);
            foreach (var npc in existingNpcs)
            {
                var npcCenter = new Vector2(npc.Bounds.Center.X, npc.Bounds.Center.Y);
                if (Vector2.Distance(spawnCenter, npcCenter) < _minDistanceBetweenNpcs)
                {
                    return false;
                }
            }

            return true;
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
    }
}