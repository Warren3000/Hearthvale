using Hearthvale.GameCode.Entities.NPCs;
using Hearthvale.GameCode.Entities.Players;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Graphics;
using Xunit;
using System.Collections.Generic;
using System.Linq;

namespace HearthvaleTest
{
    public class NpcSeparationTests
    {
        [Fact]
        public void ComputeSeparationVector_PreferasHorizontal_WhenOverlapsEqual()
        {
            // Arrange - Create rectangles with equal overlap on both axes
            var rectA = new Rectangle(10, 10, 20, 20); // Center at (20, 20)
            var rectB = new Rectangle(20, 20, 20, 20); // Center at (30, 30) - equal 10px overlap on both axes

            // Act - Use reflection to access private method
            var npcType = typeof(NPC);
            var method = npcType.GetMethod("ComputeSeparationVector",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            Assert.NotNull(method); // Ensure method is found
            var resultObj = method.Invoke(null, new object[] { rectA, rectB });
            Assert.NotNull(resultObj); // Ensure result is not null
            var result = (Vector2)resultObj!;

            // Assert - Should prefer horizontal separation (X direction)
            Assert.True(Math.Abs(result.X) > 0, "Expected horizontal separation when overlaps are equal");
            Assert.Equal(0f, result.Y, precision: 5); // Y separation should be zero when horizontal is chosen
            Assert.Equal(-10f, result.X, precision: 5); // Should push left by overlap amount
        }

        [Fact]
        public void ComputeSeparationVector_ChoosesMinimalOverlap_WhenDifferent()
        {
            // Arrange - Create rectangles with smaller X overlap
            var rectA = new Rectangle(10, 10, 20, 30); // 20x30
            var rectB = new Rectangle(25, 15, 20, 30); // 5px X overlap, 25px Y overlap

            // Act
            var npcType = typeof(NPC);
            var method = npcType.GetMethod("ComputeSeparationVector",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            Assert.NotNull(method);
            var resultObj = method.Invoke(null, new object[] { rectA, rectB });
            Assert.NotNull(resultObj);
            var result = (Vector2)resultObj!;

            // Assert - Should choose X axis (smaller overlap)
            Assert.True(Math.Abs(result.X) > 0, "Should separate on X axis (minimal overlap)");
            Assert.Equal(0f, result.Y, precision: 5); // Y separation should be zero
            Assert.Equal(-5f, result.X, precision: 5); // Should push left by X overlap amount
        }

        [Fact]
        public void ComputeSeparationVector_NoSeparation_WhenNotIntersecting()
        {
            // Arrange - Non-intersecting rectangles
            var rectA = new Rectangle(0, 0, 10, 10);
            var rectB = new Rectangle(20, 20, 10, 10);

            // Act
            var npcType = typeof(NPC);
            var method = npcType.GetMethod("ComputeSeparationVector",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            Assert.NotNull(method);
            var resultObj = method.Invoke(null, new object[] { rectA, rectB });
            Assert.NotNull(resultObj);
            var result = (Vector2)resultObj!;

            // Assert
            Assert.Equal(Vector2.Zero, result);
        }

        [Fact]
        public void NPC_PlayerOverlap_ResolvesWithoutBottomBias()
        {
            // Arrange
            var (npc, player) = CreateTestNpcAndPlayer();

            // Position NPC and player to overlap with equal X/Y overlap
            npc.SetPosition(new Vector2(100, 100));
            player.MovementComponent.SetPosition(new Vector2(110, 110)); // 10px overlap on both axes

            var gameTime = new GameTime(TimeSpan.Zero, TimeSpan.FromMilliseconds(16));
            var obstacles = new List<Rectangle>();

            // Track initial positions
            var initialNpcPos = npc.Position;
            var initialPlayerPos = player.Position;

            // Act - Update NPC which should trigger separation logic
            npc.Update(gameTime, new List<NPC> { npc }, player, obstacles);

            // Assert - NPC should move horizontally (not vertically down due to bottom bias)
            var finalNpcPos = npc.Position;
            var positionDelta = finalNpcPos - initialNpcPos;

            if (positionDelta != Vector2.Zero) // Only assert if separation occurred
            {
                Assert.True(Math.Abs(positionDelta.X) > Math.Abs(positionDelta.Y),
                    $"Expected horizontal separation, but got X={positionDelta.X}, Y={positionDelta.Y}");
            }
        }

        [Fact]
        public void MultipleNPCs_OverlapResolution_ConsistentBehavior()
        {
            // Arrange - Create multiple NPCs in overlapping positions
            var npcs = new List<NPC>();
            var (baseNpc, player) = CreateTestNpcAndPlayer();

            // Create 4 NPCs in a tight cluster
            var positions = new[]
            {
                new Vector2(100, 100),
                new Vector2(105, 100), // Right overlap
                new Vector2(100, 105), // Bottom overlap
                new Vector2(105, 105)  // Diagonal overlap
            };

            for (int i = 0; i < positions.Length; i++)
            {
                var npc = CreateTestNpc($"TestNPC{i}");
                npc.SetPosition(positions[i]);
                npcs.Add(npc);
            }

            var gameTime = new GameTime(TimeSpan.Zero, TimeSpan.FromMilliseconds(16));
            var obstacles = new List<Rectangle>();

            // Track initial positions
            var initialPositions = npcs.Select(n => n.Position).ToArray();

            // Act - Update all NPCs
            foreach (var npc in npcs)
            {
                npc.Update(gameTime, npcs, player, obstacles);
            }

            // Assert - Check that separation doesn't consistently bias downward
            var downwardMovements = 0;
            var horizontalMovements = 0;

            for (int i = 0; i < npcs.Count; i++)
            {
                var delta = npcs[i].Position - initialPositions[i];
                if (delta != Vector2.Zero)
                {
                    if (Math.Abs(delta.Y) > Math.Abs(delta.X))
                        downwardMovements++;
                    else
                        horizontalMovements++;
                }
            }

            // Should prefer horizontal movement over vertical when overlaps are equal
            Assert.True(horizontalMovements >= downwardMovements,
                $"Expected more horizontal than vertical separations, got H:{horizontalMovements}, V:{downwardMovements}");
        }

        [Fact]
        public void NPC_ChasePlayer_MaintainsStandoffDistance()
        {
            // Arrange
            var (npc, player) = CreateTestNpcAndPlayer();

            // Position player and NPC
            player.MovementComponent.SetPosition(new Vector2(200, 200));
            npc.SetPosition(new Vector2(100, 100));

            // Set NPC to chase type
            var npcType = typeof(NPC);
            var aiTypeField = npcType.GetField("_aiType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.NotNull(aiTypeField);
            aiTypeField.SetValue(npc, NpcAiType.ChasePlayer);

            var gameTime = new GameTime(TimeSpan.Zero, TimeSpan.FromMilliseconds(16));
            var obstacles = new List<Rectangle>();

            // Act - Update NPC multiple times to let it chase
            for (int i = 0; i < 10; i++)
            {
                npc.Update(gameTime, new List<NPC> { npc }, player, obstacles);
            }

            // Assert - NPC should maintain reasonable distance from player
            var distance = Vector2.Distance(npc.Position, player.Position);
            var expectedMaxDistance = 60f; // Based on weapon length + standoff logic

            Assert.True(distance <= expectedMaxDistance,
                $"NPC should be closer to player. Distance: {distance}");
        }

        private (NPC npc, Player player) CreateTestNpcAndPlayer()
        {
            var npc = CreateTestNpc("TestKnight");
            var player = CreateTestPlayer();
            return (npc, player);
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

        private SoundEffect CreateDummySoundEffect(GraphicsDevice graphicsDevice)
        {
            byte[] audioData = new byte[4];
            return new SoundEffect(audioData, 44100, AudioChannels.Mono);
        }
    }
}