using Hearthvale.GameCode.Entities;
using Hearthvale.GameCode.Entities.Characters;
using Hearthvale.GameCode.Entities.NPCs;
using Hearthvale.GameCode.Entities.Players;
using Hearthvale.GameCode.Utils;
using Hearthvale.GameCode.Collision;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.Tiled;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hearthvale.GameCode.Managers
{
    public class NpcManager : IDisposable
    {
        private readonly List<NPC> _npcs = new();
        private readonly Rectangle _bounds;
        private readonly TextureAtlas _heroAtlas;
        private Tilemap _tilemap;
        private int _wallTileId;
        private Tileset _wallTileSet;
        private WeaponManager _weaponManager;
        private readonly TextureAtlas _weaponAtlas;
        private readonly TextureAtlas _arrowAtlas;

        // Use Aether.Physics2D CollisionWorld
        private CollisionWorld _collisionWorld;
        private bool _disposed = false;

        public IEnumerable<NPC> Npcs => _npcs;
        public IEnumerable<Character> Characters => _npcs;
        public CollisionWorld CollisionWorld => _collisionWorld;

        public NpcManager(
            TextureAtlas heroAtlas,
            Rectangle bounds,
            Tilemap tilemap,
            WeaponManager weaponManager,
            TextureAtlas weaponAtlas,
            TextureAtlas arrowAtlas
        )
        {
            _heroAtlas = heroAtlas;
            _bounds = bounds;
            _tilemap = tilemap;
            _weaponManager = weaponManager;
            _weaponAtlas = weaponAtlas;
            _arrowAtlas = arrowAtlas;

            // Initialize Aether.Physics2D collision system
            _collisionWorld = new CollisionWorld(new RectangleF(bounds.X, bounds.Y, bounds.Width, bounds.Height));

            // Initialize wall colliders from tilemap
            InitializeWallColliders();
        }

        /// <summary>
        /// Initialize wall colliders from the tilemap using Aether.Physics2D
        /// </summary>
        private void InitializeWallColliders()
        {
            if (_tilemap == null) return;

            // Create collision actors for wall tiles
            for (int row = 0; row < _tilemap.Rows; row++)
            {
                for (int col = 0; col < _tilemap.Columns; col++)
                {
                    var tileTileset = _tilemap.GetTileset(col, row);
                    var tileId = _tilemap.GetTileId(col, row);

                    if (tileTileset == TilesetManager.Instance.WallTileset && AutotileMapper.IsWallTile(tileId))
                    {
                        var wallBounds = new RectangleF(
                            col * _tilemap.TileWidth,
                            row * _tilemap.TileHeight,
                            _tilemap.TileWidth,
                            _tilemap.TileHeight
                        );

                        var wallCollider = new WallCollisionActor(wallBounds);
                        _collisionWorld.AddActor(wallCollider);
                    }
                }
            }
        }

        /// <summary>
        /// Registers the player with the collision system
        /// </summary>
        public void RegisterPlayer(Character player)
        {
            if (player != null)
            {
                var playerBounds = new RectangleF(
                    player.Position.X + 8,
                    player.Position.Y + 16,
                    16, // Player collision width
                    16  // Player collision height
                );

                var playerCollider = new PlayerCollisionActor(player, playerBounds);
                _collisionWorld.AddActor(playerCollider);
            }
        }

        /// <summary>
        /// Updates the player's position in the collision system
        /// </summary>
        public void UpdatePlayerPosition(Character player)
        {
            if (player != null)
            {
                var playerActor = _collisionWorld.GetActorsOfType<PlayerCollisionActor>()
                    .FirstOrDefault(actor => actor.Player == player);

                if (playerActor != null)
                {
                    _collisionWorld.UpdateActorPosition(playerActor, player.Position);
                }
            }
        }

        /// <summary>
        /// Registers a projectile with the collision system
        /// </summary>
        public void RegisterProjectile(Projectile projectile)
        {
            if (projectile != null)
            {
                projectile.SetCollisionWorld(_collisionWorld);
                var projectileCollider = new ProjectileCollisionActor(projectile);
                _collisionWorld.AddActor(projectileCollider);
            }
        }

        /// <summary>
        /// Removes a projectile from the collision system
        /// </summary>
        public void UnregisterProjectile(Projectile projectile)
        {
            if (projectile != null)
            {
                var colliderToRemove = _collisionWorld.GetActorsOfType<ProjectileCollisionActor>()
                    .FirstOrDefault(c => c.Projectile == projectile);
                if (colliderToRemove != null)
                {
                    _collisionWorld.RemoveActor(colliderToRemove);
                }
            }
        }

        public void LoadNPCs(IEnumerable<TiledMapObject> npcObjects)
        {
            foreach (var obj in npcObjects)
            {
                if (obj.Type == "NPC")
                {
                    string npcType = obj.Name.ToLower();
                    Vector2 position = new Vector2(obj.Position.X, obj.Position.Y);
                    SpawnNPC(npcType, position);
                }
            }
        }

        public void SpawnNPC(string npcType, Vector2 position, Player player = null, float minDistanceFromPlayer = 48f)
        {
            // Check distance from player if provided
            if (player != null && Vector2.Distance(position, player.Position) < minDistanceFromPlayer)
            {
                System.Diagnostics.Debug.WriteLine($"Cannot spawn {npcType} at {position} - too close to player");
                return;
            }

            // Clamp spawn position to map bounds
            float clampedX = MathHelper.Clamp(position.X, _bounds.Left, _bounds.Right - 32);
            float clampedY = MathHelper.Clamp(position.Y, _bounds.Top, _bounds.Bottom - 32);
            Vector2 spawnPos = new Vector2(clampedX, clampedY);

            // Use Aether collision detection for spawn position validation
            var npcBounds = new RectangleF(
                spawnPos.X + 8,
                spawnPos.Y + 16,
                16, // NPC width / 2
                16  // NPC height / 2
            );

            if (IsPositionBlocked(npcBounds))
            {
                System.Diagnostics.Debug.WriteLine($"Cannot spawn {npcType} at {spawnPos} - position blocked by collision");
                return;
            }

            string animationPrefix = npcType switch
            {
                "merchant" => "Merchant",
                "mage" => "Mage",
                "archer" => "Archer",
                "blacksmith" => "Blacksmith",
                "knight" => "Knight",
                "heavyknight" => "HeavyKnight",
                "fatnun" => "FatNun",
                _ => "Merchant"
            };

            var animations = new Dictionary<string, Animation>();

            // Check for combined Idle+Walk animation first
            string combinedKey = $"{animationPrefix}_Idle+Walk";
            if (_heroAtlas.HasAnimation(combinedKey))
            {
                var combinedAnim = _heroAtlas.GetAnimation(combinedKey);
                animations["Idle"] = combinedAnim;
                animations["Walk"] = combinedAnim;
            }
            else
            {
                animations["Idle"] = _heroAtlas.GetAnimation($"{animationPrefix}_Idle");
                animations["Walk"] = _heroAtlas.GetAnimation($"{animationPrefix}_Walk");
            }

            // Only add "Defeated" if it exists in the atlas
            string defeatedKey = $"{animationPrefix}_Defeated";
            if (_heroAtlas.HasAnimation(defeatedKey))
            {
                animations["Defeated"] = _heroAtlas.GetAnimation(defeatedKey);
            }

            SoundEffect defeatSound = Core.Content.Load<SoundEffect>("audio/npc_defeat");

            int npcHealth = npcType switch
            {
                "merchant" => 8,
                "knight" => 20,
                "mage" => 12,
                _ => 10
            };

            NPC npc = new NPC(npcType, animations, spawnPos, _bounds, defeatSound, npcHealth, _tilemap, _wallTileSet);

            // Set up collision properties for the NPC
            npc.Tilemap = _tilemap;
            npc.FacingRight = false;

            // Add NPC to Aether collision world
            var npcCollider = new NpcCollisionActor(npc, npcBounds);
            _collisionWorld.AddActor(npcCollider);

            _npcs.Add(npc);
            _weaponManager.EquipWeapon(npc, new Weapon("Dagger", DataManager.Instance.GetWeaponStats("Dagger"), _weaponAtlas, _arrowAtlas));

            System.Diagnostics.Debug.WriteLine($"Successfully spawned {npcType} at {spawnPos}");
        }

        /// <summary>
        /// Check if a position is blocked using Aether collision detection
        /// </summary>
        private bool IsPositionBlocked(RectangleF bounds)
        {
            return _collisionWorld.GetActorsInBounds(bounds)
                .Any(actor => actor is WallCollisionActor || actor is NpcCollisionActor);
        }

        /// <summary>
        /// Check if movement to a new position would cause collision using Aether
        /// </summary>
        public bool CanMoveTo(Vector2 currentPosition, Vector2 newPosition, RectangleF entityBounds)
        {
            var newBounds = new RectangleF(
                newPosition.X + entityBounds.X - currentPosition.X,
                newPosition.Y + entityBounds.Y - currentPosition.Y,
                entityBounds.Width,
                entityBounds.Height
            );

            return !IsPositionBlocked(newBounds);
        }

        public void Update(GameTime gameTime, Character player, List<Rectangle> rectangles)
        {
            // Update Aether collision world
            _collisionWorld.Update(gameTime);

            // Update player position in collision system
            UpdatePlayerPosition(player);

            // Update NPCs with Aether collision detection
            foreach (var npc in _npcs)
            {
                UpdateNpcWithCollision(npc, gameTime, player);
            }

            // Remove NPCs that are ready to be removed
            for (int i = _npcs.Count - 1; i >= 0; i--)
            {
                if (_npcs[i].IsReadyToRemove)
                {
                    // Remove from Aether collision world
                    var actorsToRemove = _collisionWorld.GetActorsOfType<NpcCollisionActor>()
                        .Where(actor => actor.Npc == _npcs[i])
                        .ToList();

                    foreach (var actor in actorsToRemove)
                    {
                        _collisionWorld.RemoveActor(actor);
                    }

                    _npcs.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Update NPC with Aether collision detection
        /// </summary>
        private void UpdateNpcWithCollision(NPC npc, GameTime gameTime, Character player)
        {
            Vector2 oldPosition = npc.Position;

            // Use Aether collision world for obstacle detection - convert IShapeF to Rectangle properly
            var npcBounds = new RectangleF(npc.Position.X + 8, npc.Position.Y + 16, 16, 16);
            var nearbyWalls = _collisionWorld.GetActorsInBounds(npcBounds)
                .OfType<WallCollisionActor>()
                .Select(w => ConvertShapeToRectangle(w.Bounds))
                .ToList();

            // Standard NPC update with Aether-based obstacles
            npc.Update(gameTime, _npcs, player, nearbyWalls);

            // Update Aether collision actor position if NPC moved
            if (npc.Position != oldPosition)
            {
                var npcActor = _collisionWorld.GetActorsOfType<NpcCollisionActor>()
                    .FirstOrDefault(actor => actor.Npc == npc);

                if (npcActor != null)
                {
                    _collisionWorld.UpdateActorPosition(npcActor, npc.Position);
                }
            }
        }

        /// <summary>
        /// Converts an IShapeF to a Rectangle for legacy compatibility
        /// </summary>
        private Rectangle ConvertShapeToRectangle(IShapeF shape)
        {
            if (shape is RectangleF rect)
            {
                return new Rectangle((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height);
            }
            
            // For other shape types, get the bounding rectangle
            var bounds = shape.BoundingRectangle;
            return new Rectangle((int)bounds.X, (int)bounds.Y, (int)bounds.Width, (int)bounds.Height);
        }

        public void Draw(SpriteBatch spriteBatch, GameUIManager uiManager)
        {
            foreach (var npc in _npcs)
            {
                npc.Draw(spriteBatch);
                // Draw health bar above NPC
                Vector2 barOffset = new Vector2(npc.Sprite.Width / 4, -10);
                Vector2 barSize = new Vector2(npc.Sprite.Width / 2, 6);
                uiManager.DrawNpcHealthBar(spriteBatch, npc, barOffset, barSize);
            }
        }

        public void SpawnAllNpcTypesTest(Player player = null)
        {
            var npcTypes = new[]
            {
                "merchant", "mage", "archer", "blacksmith",
                "knight", "heavyknight", "fatnun"
            };

            int mapWidth = _tilemap.Columns;
            int mapHeight = _tilemap.Rows;
            int tileWidth = (int)_tilemap.TileWidth;
            int tileHeight = (int)_tilemap.TileHeight;

            const float MIN_DISTANCE_FROM_PLAYER = 64f;
            const float MIN_DISTANCE_BETWEEN_NPCS = 32f;

            var validSpawnPositions = new List<Vector2>();

            // Find valid spawn positions using Aether collision detection
            for (int row = 1; row < mapHeight - 1; row++)
            {
                for (int col = 1; col < mapWidth - 1; col++)
                {
                    Vector2 pos = new Vector2(col * tileWidth, row * tileHeight);
                    var testBounds = new RectangleF(pos.X + 8, pos.Y + 16, 16, 16);

                    // Check if position is free using Aether collision system
                    if (!IsPositionBlocked(testBounds))
                    {
                        // Check distance from player
                        if (player == null || Vector2.Distance(pos, player.Position) >= MIN_DISTANCE_FROM_PLAYER)
                        {
                            validSpawnPositions.Add(pos);
                        }
                    }
                }
            }

            // Shuffle positions for random distribution
            var random = new Random();
            validSpawnPositions = validSpawnPositions.OrderBy(x => random.Next()).ToList();

            int spawned = 0;
            foreach (var pos in validSpawnPositions)
            {
                if (spawned >= npcTypes.Length) break;

                bool canSpawn = true;

                // Double-check distance constraints
                if (player != null && Vector2.Distance(pos, player.Position) < MIN_DISTANCE_FROM_PLAYER)
                    canSpawn = false;

                // Check distance from existing NPCs
                foreach (var existingNpc in _npcs)
                {
                    if (Vector2.Distance(pos, existingNpc.Position) < MIN_DISTANCE_BETWEEN_NPCS)
                    {
                        canSpawn = false;
                        break;
                    }
                }

                if (canSpawn)
                {
                    SpawnNPC(npcTypes[spawned], pos);
                    spawned++;
                }
            }

            System.Diagnostics.Debug.WriteLine($"Successfully spawned {spawned} NPCs out of {npcTypes.Length} types using Aether collision detection");
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