using Hearthvale.GameCode.Entities.NPCs;
using Microsoft.Xna.Framework;
using MonoGameLibrary.Graphics;
using Xunit;
using System.Collections.Generic;
using MonoGame.Extended.Tiled;

namespace HearthvaleTest
{
    public class NpcTests
    {
        [Fact]
        public void NPC_TakeDamage_DecreasesHealth_AndDefeats()
        {
            var npc = CreateTestNpc(10);
            npc.TakeDamage(5);
            Assert.Equal(5, npc.Health);
            Assert.False(npc.IsDefeated);
            npc.Update(new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.5)), new List<NPC> { npc }, npc, new List<Rectangle>());
            npc.TakeDamage(5);
            Assert.Equal(0, npc.Health);
            Assert.True(npc.IsDefeated);
        }

        [Fact]
        public void NPC_Heal_IncreasesHealth()
        {
            var npc = CreateTestNpc(100);
            npc.TakeDamage(20);
            npc.Update(new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.5)), new List<NPC> { npc }, npc, new List<Rectangle>());
            npc.Heal(10);
            Assert.Equal(90, npc.Health);
        }

        [Fact]
        public void NPC_TakeDamage_CannotGoBelowZero()
        {
            var npc = CreateTestNpc(100);
            npc.TakeDamage(200);
            Assert.Equal(0, npc.Health);
        }

        [Fact]
        public void NPC_AnimationPlays_WhenDamaged()
        {
            var npc = CreateTestNpc(100);
            npc.TakeDamage(10);
            Assert.NotNull(npc.Sprite.Animation);
            Assert.True(npc.Sprite.Animation != null && npc.Sprite.Animation.Frames.Count > 0);
        }

        private NPC CreateTestNpc(int maxHealth)
        {
            var graphicsDevice = new Microsoft.Xna.Framework.Graphics.GraphicsDevice(
                Microsoft.Xna.Framework.Graphics.GraphicsAdapter.DefaultAdapter,
                Microsoft.Xna.Framework.Graphics.GraphicsProfile.Reach,
                new Microsoft.Xna.Framework.Graphics.PresentationParameters()
            );
            var dummyTexture = new Microsoft.Xna.Framework.Graphics.Texture2D(graphicsDevice, 1, 1);
            var dummyTextureRegion = new TextureRegion(dummyTexture, 0, 0, 1, 1);
            var dummyAnim = new Animation(new List<TextureRegion> { dummyTextureRegion }, System.TimeSpan.FromSeconds(0.1));
            var animations = new Dictionary<string, Animation>
    {
        { "Idle", dummyAnim }
    };
            Microsoft.Xna.Framework.Audio.SoundEffect dummySound = null;

            // Create a dummy tileset and tilemap for testing
            var tileset = new Tileset(dummyTextureRegion, 1, 1);
            var tilemap = new Tilemap(tileset, 1, 1);

            return new NPC("test", animations, new Vector2(0, 0), new Rectangle(0, 0, 100, 100), dummySound, maxHealth);
        }
    }
}