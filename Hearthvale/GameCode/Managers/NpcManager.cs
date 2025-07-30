using Hearthvale.GameCode.Entities.NPCs;
using Hearthvale.GameCode.Entities.Players;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Tiled;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;
using System.Collections.Generic;


namespace Hearthvale.GameCode.Managers
{
    public class NpcManager
    {
        private readonly List<NPC> _npcs = new();
        private readonly Rectangle _bounds;
        private readonly TextureAtlas _heroAtlas;
        public IEnumerable<NPC> Npcs => _npcs;

        public NpcManager(TextureAtlas heroAtlas, Rectangle bounds)
        {
            _heroAtlas = heroAtlas;
            _bounds = bounds;
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
        public void SpawnNPC(string npcType, Vector2 position)
        {
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
            NPC npc = new NPC(animations, position, _bounds, defeatSound, npcHealth);

            npc.FacingRight = false;
            _npcs.Add(npc);
        }
        public void Update(GameTime gameTime, Player player)
        {
            foreach (var npc in _npcs)
                npc.Update(gameTime, _npcs, player);

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
        public void SpawnAllNpcTypesTest()
        {
            // List of all supported NPC types (add more as needed)
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

            // Spread them horizontally across the map, near the top
            int startX = _bounds.Left + 32;
            int y = _bounds.Top + 32;
            int spacing = 64;

            for (int i = 0; i < npcTypes.Length; i++)
            {
                Vector2 pos = new Vector2(startX + i * spacing, y);
                SpawnNPC(npcTypes[i], pos);
            }
        }
    }
}