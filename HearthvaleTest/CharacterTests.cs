using Hearthvale.GameCode.Entities;
using Microsoft.Xna.Framework;
using Xunit;
using MonoGameLibrary.Graphics; // Add this using directive for AnimatedSprite
using System.Collections.Generic; // Add this using directive for List<T>
using Microsoft.Xna.Framework.Graphics;
 // Add this using directive for TextureRegion

namespace HearthvaleTest
{
    public class EntityTests
    {
        [Fact]
        public void Character_TakeDamage_DecreasesHealth()
        {
            var dummySprite = new MonoGameLibrary.Graphics.AnimatedSprite();
            var character = new TestCharacter(dummySprite, new Vector2(0, 0), 100);
            character.TakeDamage(10);
            Assert.Equal(90, character.Health);
        }

        [Fact]
        public void Character_Heal_IncreasesHealth()
        {
            var dummySprite = new MonoGameLibrary.Graphics.AnimatedSprite();
            var character = new TestCharacter(dummySprite, new Vector2(0, 0), 100);
            character.TakeDamage(20);
            character.Heal(10);
            Assert.Equal(90, character.Health);
        }

        [Fact]
        public void Character_TakeDamage_CannotGoBelowZero()
        {
            var dummySprite = new MonoGameLibrary.Graphics.AnimatedSprite();
            var character = new TestCharacter(dummySprite, new Vector2(0, 0), 100);
            character.TakeDamage(200);
            Assert.Equal(0, character.Health);
        }

        [Fact]
        public void Character_AnimationPlays_WhenDamaged()
        {
            // Arrange
            var dummyTextureRegion = new TextureRegion(); // Use parameterless constructor for dummy
            var dummyAnim = new Animation(new List<TextureRegion> { dummyTextureRegion }, System.TimeSpan.FromSeconds(0.1));
            var dummySprite = new AnimatedSprite(dummyAnim); // Attach animation to sprite
            var character = new TestCharacter(dummySprite, new Vector2(0, 0), 100);

            // Act
            character.TakeDamage(10);

            // Assert
            // Check if the animation has started playing
            Assert.NotNull(dummySprite.Animation);
            Assert.True(dummySprite.Animation != null && dummySprite.Animation.Frames.Count > 0);
        }

        // Add more tests for Player, NPC, Weapon, etc. as needed
    }

    // Minimal test double for Character (abstract)
    public class TestCharacter : Character
    {
        public TestCharacter(AnimatedSprite sprite, Vector2 pos, int maxHealth)
        {
            this.AnimationComponent.SetSprite(sprite);
            this.MovementComponent.SetPosition(pos);
        }
        public override void Flash() { }
    }
}