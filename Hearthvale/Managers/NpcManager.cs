using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Tiled;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;
using System.Collections.Generic;


namespace Hearthvale.Managers
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
                _ => "Merchant"
            };

            var animations = new Dictionary<string, Animation>
            {
                ["Idle"] = _heroAtlas.GetAnimation($"{animationPrefix}_Idle"),
                ["Walk"] = _heroAtlas.GetAnimation($"{animationPrefix}_Walk")
            };

            // Only add "Defeated" if it exists in the atlas
            string defeatedKey = $"{animationPrefix}_Defeated";
            if (_heroAtlas.HasAnimation(defeatedKey))
            {
                animations["Defeated"] = _heroAtlas.GetAnimation(defeatedKey);
            }

            SoundEffect defeatSound = Core.Content.Load<SoundEffect>("audio/npc_defeat");
            NPC npc = new NPC(animations, position, _bounds, defeatSound);

            npc.FacingRight = false;
            _npcs.Add(npc);
        }
        public void Update(GameTime gameTime)
        {
            foreach (var npc in _npcs)
                npc.Update(gameTime);

            // Remove NPCs that are ready to be removed (e.g., after defeat animation)
            _npcs.RemoveAll(npc => npc.IsReadyToRemove);
        }
        public void Draw(SpriteBatch spriteBatch)
        {
            foreach (var npc in _npcs)
            {
                npc.Draw(spriteBatch);
            }
        }

    }
}