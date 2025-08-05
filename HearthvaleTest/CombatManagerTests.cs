using Hearthvale.GameCode.Managers;
using Hearthvale.GameCode.Entities;
using Hearthvale.GameCode.Entities.NPCs;
using Hearthvale.GameCode.Entities.Players;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Graphics;
using Xunit;
using System.Collections.Generic;
using System.Linq;
using Hearthvale.GameCode.UI;

namespace HearthvaleTest
{
    public class CombatManagerTests
    {
        [Fact]
        public void CanAttack_InitiallyTrue_And_RespectsCooldown()
        {
            var (combatManager, _, _, _, _, _) = CreateTestCombatManager();
            Assert.True(combatManager.CanAttack());
            combatManager.StartCooldown();
            Assert.False(combatManager.CanAttack());
        }

        [Fact]
        public void CanAttack_ResetsAfterCooldown()
        {
            var (combatManager, _, _, _, _, _) = CreateTestCombatManager();
            Assert.True(combatManager.CanAttack());
            combatManager.StartCooldown();
            Assert.False(combatManager.CanAttack());
            // Simulate enough time passing for cooldown to expire
            combatManager.Update(new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(1)));
            Assert.True(combatManager.CanAttack());
        }

        [Fact]
        public void RegisterProjectile_AddsProjectile()
        {
            var (combatManager, _, _, _, _, _) = CreateTestCombatManager();
            var dummyTexture = new Texture2D(GraphicsDevice(), 1, 1);
            var region = new TextureRegion(dummyTexture, 0, 0, 1, 1);
            var anim = new Animation(new List<TextureRegion> { region }, System.TimeSpan.FromSeconds(0.1));
            var proj = new Projectile(anim, Vector2.Zero, Vector2.One, 5);
            combatManager.RegisterProjectile(proj);
            // No direct way to check, but no exception = pass
        }

        [Fact]
        public void RegisterProjectile_StoresProjectile()
        {
            var (combatManager, _, _, _, spriteBatch, _) = CreateTestCombatManager();
            var dummyTexture = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
            var region = new TextureRegion(dummyTexture, 0, 0, 1, 1);
            var anim = new Animation(new List<TextureRegion> { region }, System.TimeSpan.FromSeconds(0.1));
            var proj = new Projectile(anim, Vector2.Zero, Vector2.One, 5);
            combatManager.RegisterProjectile(proj);
            combatManager.Update(new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.1)));
            spriteBatch.Begin();
            combatManager.DrawProjectiles(spriteBatch);
            spriteBatch.End();
        }

        [Fact]
        public void TryDamagePlayer_DamagesPlayer_WhenNotOnCooldown()
        {
            var (combatManager, player, npcManager, _, _, _) = CreateTestCombatManager();
            var npc = npcManager.Npcs.First();
            int initialHealth = player.Health;
            combatManager.TryDamagePlayer(10, npc.Position);
            Assert.True(player.Health < initialHealth);
        }

        [Fact]
        public void TryDamagePlayer_RespectsDamageCooldown()
        {
            var (combatManager, player, npcManager, _, _, _) = CreateTestCombatManager();
            var npc = npcManager.Npcs.First();
            int initialHealth = player.Health;
            combatManager.TryDamagePlayer(10, npc.Position);
            Assert.True(player.Health < initialHealth);
            int afterFirstHit = player.Health;
            // Try to damage again immediately, should not take damage due to cooldown
            combatManager.TryDamagePlayer(10, npc.Position);
            Assert.Equal(afterFirstHit, player.Health);
            // Simulate enough time passing for damage cooldown to expire
            combatManager.Update(new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(2)));
            combatManager.TryDamagePlayer(10, npc.Position);
            Assert.True(player.Health < afterFirstHit);
        }

        [Fact]
        public void SetPlayer_UpdatesPlayerReference()
        {
            var (combatManager, _, npcManager, _, _, _) = CreateTestCombatManager();
            var npc = npcManager.Npcs.First();
            var dummyTexture = new Texture2D(GraphicsDevice(), 1, 1);
            var atlas = new TextureAtlas(dummyTexture);
            // Add required animations for Player
            var anim = new Animation(new List<TextureRegion> { new TextureRegion(dummyTexture, 0, 0, 1, 1) }, System.TimeSpan.FromSeconds(0.1));
            atlas.AddAnimation("Mage_Idle", anim);
            atlas.AddAnimation("Mage_Walk", anim);
            var newPlayer = new Player(atlas, Vector2.Zero, null, null, null, 100f);
            combatManager.SetPlayer(newPlayer);
            // Try to damage the new player and check health is reduced
            int initialHealth = newPlayer.Health;
            combatManager.TryDamagePlayer(5, npc.Position);
            Assert.True(newPlayer.Health < initialHealth);
        }

        [Fact]
        public void Update_ProcessesProjectilesAndCooldowns()
        {
            var (combatManager, player, _, _, _, _) = CreateTestCombatManager();
            var dummyTexture = new Texture2D(GraphicsDevice(), 1, 1);
            var region = new TextureRegion(dummyTexture, 0, 0, 1, 1);
            var anim = new Animation(new List<TextureRegion> { region }, System.TimeSpan.FromSeconds(0.1));
            var proj = new Projectile(anim, Vector2.Zero, Vector2.One, 5);
            combatManager.RegisterProjectile(proj);
            // Simulate several updates
            for (int i = 0; i < 10; i++)
            {
                combatManager.Update(new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.1)));
            }
            // No exception = pass, projectiles should be processed and possibly removed if out of bounds
        }

        [Fact]
        public void Update_ProjectileHitsNpc_NpcTakesDamage()
        {
            // Arrange
            var (combatManager, player, npcManager, _, _, _) = CreateTestCombatManager();
            var npc = npcManager.Npcs.First();
            int initialNpcHealth = npc.Health;
            var dummyTexture = new Texture2D(GraphicsDevice(), 1, 1);
            var region = new TextureRegion(dummyTexture, 0, 0, 1, 1);
            var anim = new Animation(new List<TextureRegion> { region }, System.TimeSpan.FromSeconds(0.1));
            // Position the projectile to hit the NPC immediately
            var projectile = new Projectile(anim, npc.Position, Vector2.Zero, 5);
            combatManager.RegisterProjectile(projectile);

            // Act
            combatManager.Update(new GameTime());

            // Assert
            Assert.True(npc.Health < initialNpcHealth);
        }

        private (CombatManager, Player, NpcManager, ScoreManager, SpriteBatch, CombatEffectsManager) CreateTestCombatManager()
        {
            var graphicsDevice = GraphicsDevice();
            var spriteBatch = new SpriteBatch(graphicsDevice);
            var dummyTexture = new Texture2D(graphicsDevice, 1, 1);
            var atlas = new TextureAtlas(dummyTexture);
            atlas.AddRegion("Mage_Idle", 0, 0, 1, 1);
            var anim = new Animation(new List<TextureRegion> { new TextureRegion(dummyTexture, 0, 0, 1, 1) }, System.TimeSpan.FromSeconds(0.1));
            atlas.AddAnimation("Mage_Idle", anim);
            atlas.AddAnimation("Mage_Walk", anim);
            var player = new Player(atlas, Vector2.Zero, null, null, null, 100f);

            // Create a dummy tileset and tilemap for testing
            var tileset = new Tileset(new TextureRegion(dummyTexture, 0, 0, 1, 1), 1, 1);
            var tilemap = new Tilemap(tileset, 1, 1);
            int wallTileId = 0;

            var npcList = new List<NPC>();
            var weaponAtlas = new TextureAtlas(dummyTexture);
            var arrowAtlas = new TextureAtlas(dummyTexture);
            var weaponManager = new WeaponManager(atlas, weaponAtlas, new Rectangle(0, 0, 100, 100), npcList);

            var npcManager = new NpcManager(
                atlas,
                new Rectangle(0, 0, 100, 100),
                tilemap,
                wallTileId,
                weaponManager,
                weaponAtlas,
                arrowAtlas
            );

            var dummyFont = new SpriteFont(
                dummyTexture,
                new List<Rectangle>(),
                new List<Rectangle>(),
                new List<char>(),
                1,
                0,
                new List<Vector3>(),
                null
            );
            var position = Vector2.Zero;
            var origin = Vector2.Zero;
            ScoreManager.Initialize(dummyFont, position, origin);
            var camera = new Camera2D(graphicsDevice.Viewport);
            CombatEffectsManager.Initialize(camera);

            // Create dummy sound effects for testing
            var hitSound = CreateDummySoundEffect(graphicsDevice);
            var defeatSound = CreateDummySoundEffect(graphicsDevice);

            CombatManager.Initialize(npcManager, player, ScoreManager.Instance, spriteBatch, CombatEffectsManager.Instance, hitSound, defeatSound, new Rectangle(0, 0, 100, 100));
            return (CombatManager.Instance, player, npcManager, ScoreManager.Instance, spriteBatch, CombatEffectsManager.Instance);
        }

        private GraphicsDevice GraphicsDevice()
        {
            return new GraphicsDevice(GraphicsAdapter.DefaultAdapter, GraphicsProfile.Reach, new PresentationParameters());
        }
        /// <summary>
        /// Creates a dummy sound effect for testing purposes.
        /// </summary>
        private SoundEffect CreateDummySoundEffect(GraphicsDevice graphicsDevice)
        {
            // Create a minimal sound effect with a single sample
            byte[] audioData = new byte[4]; // Minimal audio data
            return new SoundEffect(audioData, 44100, AudioChannels.Mono);
        }
    }
}
