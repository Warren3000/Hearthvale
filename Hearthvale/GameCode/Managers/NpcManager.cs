using Hearthvale.GameCode.Entities;
using Hearthvale.GameCode.Entities.Characters;
using Hearthvale.GameCode.Entities.NPCs;
using Hearthvale.GameCode.Entities.Players;
using Hearthvale.GameCode.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Tiled;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hearthvale.GameCode.Managers
{
    public class NpcManager
    {
        private readonly List<NPC> _npcs = new();
        private readonly Rectangle _bounds;
        private readonly TextureAtlas _heroAtlas;
        private Tilemap _tilemap;
        private int _wallTileId;
        private Tileset _wallTileSet;
        private WeaponManager _weaponManager; // Add this line
                                              // Add these fields to the NpcManager class
        private readonly TextureAtlas _weaponAtlas;
        private readonly TextureAtlas _arrowAtlas;

        public IEnumerable<NPC> Npcs => _npcs;
        public IEnumerable<Character> Characters => _npcs;

         public NpcManager(
            TextureAtlas heroAtlas,
            Rectangle bounds,
            Tilemap tilemap,
            WeaponManager weaponManager,
            TextureAtlas weaponAtlas,      // Add this parameter
            TextureAtlas arrowAtlas        // Add this parameter
        )
        {
            _heroAtlas = heroAtlas;
            _bounds = bounds;
            _tilemap = tilemap;
            _weaponManager = weaponManager;
            _weaponAtlas = weaponAtlas;    // Initialize the field
            _arrowAtlas = arrowAtlas;      // Initialize the field
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
            float clampedX = MathHelper.Clamp(position.X, _bounds.Left, _bounds.Right - 32); // 32: typical sprite width
            float clampedY = MathHelper.Clamp(position.Y, _bounds.Top, _bounds.Bottom - 32); // 32: typical sprite height
            Vector2 spawnPos = new Vector2(clampedX, clampedY);
            Rectangle npcRect = new Rectangle(
                (int)spawnPos.X + 8,
                (int)spawnPos.Y + 16,
                32 / 2, // Replace 32 with actual NPC sprite width if needed
                32 / 2
            );
            if (IsRectOverlappingWall(npcRect))
            {
                System.Diagnostics.Debug.WriteLine($"Cannot spawn {npcType} at {spawnPos} - would overlap wall");
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
                // ... other types
                _ => 10
            };
            NPC npc = new NPC(npcType, animations, spawnPos, _bounds, defeatSound, npcHealth, _tilemap, _wallTileSet);

            // Set up collision properties for the NPC
            npc.Tilemap = _tilemap;

            npc.FacingRight = false;
            _npcs.Add(npc);
            _weaponManager.EquipWeapon(npc, new Weapon("Dagger", DataManager.Instance.GetWeaponStats("Dagger"), _weaponAtlas, _arrowAtlas));
            
            System.Diagnostics.Debug.WriteLine($"Successfully spawned {npcType} at {spawnPos}");
        }

        public void Update(GameTime gameTime, Character player, List<Rectangle> rectangles)
        {
            foreach (var npc in _npcs)
                npc.Update(gameTime, _npcs, player, rectangles);

            // Remove NPCs that are ready to be removed (e.g., after defeat animation)
            _npcs.RemoveAll(npc => npc.IsReadyToRemove);
        }

        public void Draw(SpriteBatch spriteBatch, GameUIManager uiManager)
        {
            foreach (var npc in _npcs)
            {
                npc.Draw(spriteBatch);
                // Draw health bar above NPC
                Vector2 barOffset = new Vector2(npc.Sprite.Width / 4, -10); // Adjust as needed
                Vector2 barSize = new Vector2(npc.Sprite.Width / 2, 6);
                uiManager.DrawNpcHealthBar(spriteBatch, npc, barOffset, barSize);
            }
        }

        public void SpawnAllNpcTypesTest(Player player = null)
        {
            var npcTypes = new[]
            {
                "merchant",
                "mage",
                "archer",
                "blacksmith",
                "knight",
                "heavyknight",
                "fatnun"
            };

            int mapWidth = _tilemap.Columns;
            int mapHeight = _tilemap.Rows;
            int tileWidth = (int)_tilemap.TileWidth;
            int tileHeight = (int)_tilemap.TileHeight;
            
            const float MIN_DISTANCE_FROM_PLAYER = 64f; // Minimum distance in pixels
            
            // Collect all valid spawn positions first
            var validSpawnPositions = new List<Vector2>();

            for (int row = 1; row < mapHeight - 1; row++)
            {
                for (int col = 1; col < mapWidth - 1; col++)
                {
                    int tileId = _tilemap.GetTileId(col, row);
                    if (!AutotileMapper.IsWallTile(tileId))
                    {
                        Vector2 pos = new Vector2(col * tileWidth, row * tileHeight);

                        // Calculate the NPC's bounding box at this position
                        Rectangle npcRect = new Rectangle(
                            (int)pos.X + 8,
                            (int)pos.Y + 16,
                            32 / 2, // Replace 32 with your actual NPC sprite width if different
                            32 / 2  // Replace 32 with your actual NPC sprite height if different
                        );

                        // Skip if this rectangle overlaps any wall tile
                        if (IsRectOverlappingWall(npcRect))
                            continue;

                        // Check distance from player if player is provided
                        if (player == null || Vector2.Distance(pos, player.Position) >= MIN_DISTANCE_FROM_PLAYER)
                        {
                            validSpawnPositions.Add(pos);
                        }
                    }
                }
            }

            // Shuffle the positions to get random distribution
            var random = new Random();
            validSpawnPositions = validSpawnPositions.OrderBy(x => random.Next()).ToList();
            
            int spawned = 0;
            foreach (var pos in validSpawnPositions)
            {
                if (spawned >= npcTypes.Length) break;
                
                // Double-check distance from player and other NPCs
                bool canSpawn = true;
                
                if (player != null && Vector2.Distance(pos, player.Position) < MIN_DISTANCE_FROM_PLAYER)
                {
                    canSpawn = false;
                }
                
                // Check distance from existing NPCs
                const float MIN_DISTANCE_BETWEEN_NPCS = 32f;
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
            
            System.Diagnostics.Debug.WriteLine($"Successfully spawned {spawned} NPCs out of {npcTypes.Length} types");
        }
        private bool IsRectOverlappingWall(Rectangle rect)
        {
            int leftTile = rect.Left / (int)_tilemap.TileWidth;
            int rightTile = (rect.Right - 1) / (int)_tilemap.TileWidth;
            int topTile = rect.Top / (int)_tilemap.TileHeight;
            int bottomTile = (rect.Bottom - 1) / (int)_tilemap.TileHeight;

            for (int col = leftTile; col <= rightTile; col++)
            {
                for (int row = topTile; row <= bottomTile; row++)
                {
                    if (AutotileMapper.IsWallTile(_tilemap.GetTileId(col, row)))
                        return true;
                }
            }
            return false;
        }


    }
}