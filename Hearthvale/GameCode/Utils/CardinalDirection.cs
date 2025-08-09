using Microsoft.Xna.Framework;
using System;

namespace Hearthvale.GameCode.Utils
{
    public enum CardinalDirection
    {
        North,
        East,
        South,
        West
    }

    public static class CardinalDirectionExtensions
    {
        /// <summary>
        /// Converts a movement vector to the closest cardinal direction
        /// </summary>
        public static CardinalDirection ToCardinalDirection(this Vector2 movement)
        {
            if (movement == Vector2.Zero)
                return CardinalDirection.South; // Default facing direction

            // Find the dominant direction (largest absolute component)
            if (Math.Abs(movement.X) > Math.Abs(movement.Y))
            {
                return movement.X > 0 ? CardinalDirection.East : CardinalDirection.West;
            }
            else
            {
                return movement.Y > 0 ? CardinalDirection.South : CardinalDirection.North;
            }
        }

        /// <summary>
        /// Converts a cardinal direction to a unit vector
        /// </summary>
        public static Vector2 ToVector(this CardinalDirection direction)
        {
            return direction switch
            {
                CardinalDirection.North => -Vector2.UnitY,
                CardinalDirection.East => Vector2.UnitX,
                CardinalDirection.South => Vector2.UnitY,
                CardinalDirection.West => -Vector2.UnitX,
                _ => Vector2.Zero,
            };
        }

        /// <summary>
        /// Gets the rotation angle for this cardinal direction (in radians)
        /// </summary>
        public static float ToRotation(this CardinalDirection direction)
        {
            return direction switch
            {
                CardinalDirection.North => -MathHelper.PiOver2, // -90 degrees (up)
                CardinalDirection.East => 0, // 0 degrees (right)
                CardinalDirection.South => MathHelper.PiOver2, // 90 degrees (down)
                CardinalDirection.West => MathHelper.Pi, // 180 degrees (left)
                _ => 0,
            };
        }
    }
}