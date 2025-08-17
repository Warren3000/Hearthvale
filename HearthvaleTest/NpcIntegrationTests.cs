using Hearthvale.GameCode.Entities.NPCs;
using Hearthvale.GameCode.Entities.Players;
using Hearthvale.GameCode.Managers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Graphics;
using Xunit;
using System.Collections.Generic;
using System.Linq;

namespace HearthvaleTest
{
    public class NpcIntegrationTests
    {
        [Fact]
        public void NpcSeparation_InGameScenario_NoBottomBias()
        {
            // Arrange - Create a realistic game scenario
            var gameTime = new GameTime(TimeSpan.Zero, TimeSpan.FromMilliseconds(16));
            var obstacles = new List<Rectangle>
            {
                new Rectangle(50, 50, 32, 32), // Static obstacle
                new Rectangle(150, 150, 32, 32)
            };

            var npcs = CreateClusteredNpcs(new Vector2(200, 200), 5);
            var player = CreateTestPlayer();
            player.MovementComponent.SetPosition(new Vector2(195, 195)); // Near NPC cluster

            // Track movement directions over multiple updates
            var movementDirections = new List<Vector2>();

            // Act - Simulate several game updates
            for (int frame = 0; frame < 30; frame++)
            {
                var initialPositions = npcs.Select(n => n.Position).ToArray();

                foreach (var npc in npcs)
                {
                    npc.Update(gameTime, npcs, player, obstacles);
                }

                // Track any movements that occurred
                for (int i = 0; i < npcs.Count; i++)
                {
                    var movement = npcs[i].Position - initialPositions[i];
                    if (movement.LengthSquared() > 0.01f) // Significant movement
                    {
                        movementDirections.Add(Vector2.Normalize(movement));
                    }
                }
            }

            // Assert - Check distribution of movement directions
            if (movementDirections.Count > 0)
            {
                var downwardBias = movementDirections.Count(dir => dir.Y > 0.7f); // Strongly downward
                var horizontalMovement = movementDirections.Count(dir => Math.Abs(dir.X) > 0.7f); // Strongly horizontal

                // Should not have overwhelming downward bias
                var biasRatio = downwardBias / (float)movementDirections.Count;
                Assert.True(biasRatio < 0.6f,
                    $"Too much downward bias: {biasRatio:P1} of movements were downward");

                // Should have reasonable horizontal distribution
                Assert.True(horizontalMovement > 0, "Expected some horizontal movement in separation");
            }
        }

        private List<NPC> CreateClusteredNpcs(Vector2 center, int count)
        {
            var npcs = new List<NPC>();
            var random = new System.Random(42); // Fixed seed for reproducible tests

            for (int i = 0; i < count; i++)
            {
                var npc = CreateTestNpc($"TestNPC{i}");

                // Position NPCs in a tight cluster
                var offset = new Vector2(
                    (random.NextSingle() - 0.5f) * 20,
                    (random.NextSingle() - 0.5f) * 20
                );
                npc.SetPosition(center + offset);
                npcs.Add(npc);
            }

            return npcs;
        }

        private NPC CreateTestNpc(string name)
        {
            var graphicsDevice = CreateGraphicsDevice();
            var dummyTexture = new Texture2D(graphicsDevice, 32, 32);
            var dummyTextureRegion = new TextureRegion(dummyTexture, 0, 0, 32, 32);
            var dummyAnim = new Animation(new List<TextureRegion> { dummyTextureRegion }, TimeSpan.FromSeconds(0.1));

            var animations = new Dictionary<string, Animation>
            {
                { "Idle", dummyAnim },
                { "Walk", dummyAnim },
                { "Attack", dummyAnim },
                { "Hit", dummyAnim },
                { "Defeated", dummyAnim }
            };

            var bounds = new Rectangle(0, 0, 800, 600);
            var defeatSound = CreateDummySoundEffect(graphicsDevice);

            return new NPC(name, animations, Vector2.Zero, bounds, defeatSound, 100);
        }

        private Player CreateTestPlayer()
        {
            var graphicsDevice = CreateGraphicsDevice();
            var dummyTexture = new Texture2D(graphicsDevice, 32, 32);
            var atlas = new TextureAtlas(dummyTexture);

            var dummyAnim = new Animation(new List<TextureRegion>
            {
                new TextureRegion(dummyTexture, 0, 0, 32, 32)
            }, TimeSpan.FromSeconds(0.1));

            atlas.AddAnimation("Mage_Idle", dummyAnim);
            atlas.AddAnimation("Mage_Walk", dummyAnim);

            return new Player(atlas, Vector2.Zero, null, null, null, 100f);
        }

        private GraphicsDevice CreateGraphicsDevice()
        {
            return new GraphicsDevice(GraphicsAdapter.DefaultAdapter, GraphicsProfile.Reach,
                new PresentationParameters());
        }

        private Microsoft.Xna.Framework.Audio.SoundEffect CreateDummySoundEffect(GraphicsDevice graphicsDevice)
        {
            byte[] audioData = new byte[4];
            return new Microsoft.Xna.Framework.Audio.SoundEffect(audioData, 44100, Microsoft.Xna.Framework.Audio.AudioChannels.Mono);
        }
    }
}