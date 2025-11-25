using Hearthvale.GameCode.Entities.Players;
using Hearthvale.GameCode.Entities.Components;
using Hearthvale.GameCode.Utils;
using Microsoft.Xna.Framework;
using MonoGameLibrary.Graphics;
using Xunit;
using System.Collections.Generic;

namespace HearthvaleTest
{
    public class PlayerAnimationTests
    {
        [Theory]
        [InlineData(CardinalDirection.North, false, "Idle_Up")]
        [InlineData(CardinalDirection.North, true, "Run_Up")]
        [InlineData(CardinalDirection.NorthEast, false, "Idle_Up")]
        [InlineData(CardinalDirection.NorthEast, true, "Run_Up")]
        [InlineData(CardinalDirection.NorthWest, false, "Idle_Up")]
        [InlineData(CardinalDirection.NorthWest, true, "Run_Up")]
        [InlineData(CardinalDirection.South, false, "Idle_Down")]
        [InlineData(CardinalDirection.South, true, "Run_Down")]
        [InlineData(CardinalDirection.SouthEast, false, "Idle_Down")]
        [InlineData(CardinalDirection.SouthEast, true, "Run_Down")]
        [InlineData(CardinalDirection.SouthWest, false, "Idle_Down")]
        [InlineData(CardinalDirection.SouthWest, true, "Run_Down")]
        [InlineData(CardinalDirection.East, false, "Idle_Right")]
        [InlineData(CardinalDirection.East, true, "Run_Right")]
        [InlineData(CardinalDirection.West, false, "Idle_Left")]
        [InlineData(CardinalDirection.West, true, "Run_Left")]
        public void Player_Animation_Matches_Direction_And_Movement(CardinalDirection dir, bool isMoving, string expectedAnim)
        {
            // Arrange
            var dummyRegion = new TextureRegion
            {
                SourceRectangle = new Rectangle(0, 0, 1, 1)
            };
            var anims = new Dictionary<string, Animation>
            {
                { "Idle_Down", new Animation(new List<TextureRegion> { dummyRegion }, System.TimeSpan.FromSeconds(0.1)) },
                { "Idle_Up", new Animation(new List<TextureRegion> { dummyRegion }, System.TimeSpan.FromSeconds(0.1)) },
                { "Idle_Right", new Animation(new List<TextureRegion> { dummyRegion }, System.TimeSpan.FromSeconds(0.1)) },
                { "Idle_Left", new Animation(new List<TextureRegion> { dummyRegion }, System.TimeSpan.FromSeconds(0.1)) },
                { "Run_Down", new Animation(new List<TextureRegion> { dummyRegion }, System.TimeSpan.FromSeconds(0.1)) },
                { "Run_Up", new Animation(new List<TextureRegion> { dummyRegion }, System.TimeSpan.FromSeconds(0.1)) },
                { "Run_Right", new Animation(new List<TextureRegion> { dummyRegion }, System.TimeSpan.FromSeconds(0.1)) },
                { "Run_Left", new Animation(new List<TextureRegion> { dummyRegion }, System.TimeSpan.FromSeconds(0.1)) }
            };
            var animComponent = new CharacterAnimationComponent(null, new AnimatedSprite(anims["Idle_Down"]));
            foreach (var kv in anims)
                animComponent.AddAnimation(kv.Key, kv.Value);
            // Act
            // Simulate direction
            var movementComponent = new CharacterMovementComponent(null, Vector2.Zero);
            movementComponent.FacingDirection = dir;
            typeof(CharacterAnimationComponent)
                .GetField("_character", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(animComponent, new DummyCharacter(movementComponent));
            animComponent.UpdateAnimation(isMoving);
            // Assert
            Assert.Equal(expectedAnim, animComponent.CurrentAnimationName);
        }

        private class DummyCharacter : Hearthvale.GameCode.Entities.Character
        {
            public DummyCharacter(CharacterMovementComponent movement) { _movementComponent = movement; }
            public override void Flash() { }
            protected override Vector2 GetAttackDirection() => Vector2.Zero;
        }
    }
}
