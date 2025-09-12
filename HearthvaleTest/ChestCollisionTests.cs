using System;
using System.Collections.Generic;
using Hearthvale.GameCode.Collision;
using Hearthvale.GameCode.Entities.Players;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGameLibrary.Graphics;
using Xunit;

namespace HearthvaleTest
{
    public class ChestCollisionTests
    {
        [Fact]
        public void CollisionWorld_WouldCollideWithChest_ReturnsTrue()
        {
            // Arrange
            var cw = new CollisionWorld(new RectangleF(0, 0, 1024, 1024));
            // Place chest at tile (3,3) => pixel (96,96) size 32x32
            var (loot, chestBounds) = CreateAndAttachChest(cw, col: 3, row: 3);

            // Act
            // Query an AABB overlapping the chest
            var queryRect = new RectangleF(chestBounds.X + 4, chestBounds.Y + 4, 8, 8);
            var collides = cw.WouldCollideWith<ChestCollisionActor>(queryRect);

            // Assert
            Assert.True(collides);
        }

        [Fact]
        public void Character_MoveIntoChest_IsBlocked()
        {
            // Arrange
            var cw = new CollisionWorld(new RectangleF(0, 0, 1024, 1024));
            // Chest at tile (6,6) => pixel (192,192)
            var (_, chestBounds) = CreateAndAttachChest(cw, col: 6, row: 6);

            var player = CreateTestPlayer();
            // Wire collision world so the character collision component can detect chests
            player.CollisionComponent.SetCollisionWorld(cw);

            // Place player left of chest
            var initialPos = new Vector2(chestBounds.Left - 32, chestBounds.Y);
            player.MovementComponent.SetPosition(initialPos);

            // Align player's bounds vertically to overlap chest (center-to-center on Y)
            var pb = player.Bounds; // bounds reflect current position
            var playerCenterY = pb.Top + pb.Height / 2f;
            var chestCenterY = chestBounds.Top + chestBounds.Height / 2f;
            var deltaY = chestCenterY - playerCenterY;
            player.MovementComponent.SetPosition(new Vector2(player.Position.X, player.Position.Y + deltaY));

            // Target attempts to move through the chest horizontally
            var targetPos = new Vector2(chestBounds.Right + 28, player.Position.Y);

            // Act
            var moved = player.CollisionComponent.TryMove(targetPos, Array.Empty<Hearthvale.GameCode.Entities.Character>());

            // Assert
            var finalBounds = player.Bounds;
            // Must not intersect with the chest
            Assert.False(finalBounds.Intersects(chestBounds));
            // Must not have crossed through (player's right edge should stay at or left of chest's left edge)
            Assert.True(finalBounds.Right <= chestBounds.Left, $"Player penetrated chest. finalBounds={finalBounds}, chest={chestBounds}, moved={moved}");
        }

        private static (DungeonLoot loot, Rectangle bounds) CreateAndAttachChest(CollisionWorld cw, int col, int row)
        {
            // DungeonLoot.Bounds assumes 32px tiles if Tilemap is unavailable
            var loot = new DungeonLoot(id: $"loot_{col}_{row}", lootTableId: "test-table", column: col, row: row);
            loot.AttachCollision(cw);
            var rect = new Rectangle(col * 32, row * 32, 32, 32);
            return (loot, rect);
        }

        private static Player CreateTestPlayer()
        {
            var graphicsDevice = new GraphicsDevice(
                GraphicsAdapter.DefaultAdapter, GraphicsProfile.Reach, new PresentationParameters());

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
    }
}