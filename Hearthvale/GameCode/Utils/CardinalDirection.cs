using Microsoft.Xna.Framework;
using System;

namespace Hearthvale.GameCode.Utils
{
    public enum CardinalDirection
    {
        North = 0,
        East = 1,
        South = 2,
        West = 3,
        NorthEast = 4,
        SouthEast = 5,
        SouthWest = 6,
        NorthWest = 7
    }

    public static class CardinalDirectionExtensions
    {
        private const float HalfSliceDegrees = 22.5f;
        private const float InvSqrt2 = 0.70710677f;

        /// <summary>
        /// Converts a movement vector to the closest cardinal direction
        /// </summary>
        public static CardinalDirection ToCardinalDirection(this Vector2 movement)
        {
            if (movement == Vector2.Zero)
                return CardinalDirection.South; // Default facing direction

            float angle = MathF.Atan2(movement.Y, movement.X);
            return FromAngle(angle);
        }

        /// <summary>
        /// Converts a cardinal direction to a unit vector
        /// </summary>
        public static Vector2 ToVector(this CardinalDirection direction)
        {
            return direction switch
            {
                CardinalDirection.North => -Vector2.UnitY,
                CardinalDirection.NorthEast => new Vector2(InvSqrt2, -InvSqrt2),
                CardinalDirection.East => Vector2.UnitX,
                CardinalDirection.SouthEast => new Vector2(InvSqrt2, InvSqrt2),
                CardinalDirection.South => Vector2.UnitY,
                CardinalDirection.SouthWest => new Vector2(-InvSqrt2, InvSqrt2),
                CardinalDirection.West => -Vector2.UnitX,
                CardinalDirection.NorthWest => new Vector2(-InvSqrt2, -InvSqrt2),
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
                CardinalDirection.NorthEast => -MathHelper.PiOver4,
                CardinalDirection.East => 0, // 0 degrees (right)
                CardinalDirection.SouthEast => MathHelper.PiOver4,
                CardinalDirection.South => MathHelper.PiOver2, // 90 degrees (down)
                CardinalDirection.SouthWest => MathHelper.Pi - MathHelper.PiOver4,
                CardinalDirection.West => MathHelper.Pi, // 180 degrees (left)
                CardinalDirection.NorthWest => -MathHelper.Pi + MathHelper.PiOver4,
                _ => 0,
            };
        }

        /// <summary>
        /// Collapses an eight-direction value to the nearest four-way primary axis.
        /// </summary>
        public static CardinalDirection ToFourWay(this CardinalDirection direction)
        {
            return direction switch
            {
                CardinalDirection.NorthEast => CardinalDirection.North,
                CardinalDirection.NorthWest => CardinalDirection.North,
                CardinalDirection.SouthEast => CardinalDirection.South,
                CardinalDirection.SouthWest => CardinalDirection.South,
                _ => direction
            };
        }

        /// <summary>
        /// Determines whether the direction is predominantly facing right.
        /// </summary>
        public static bool IsRightFacing(this CardinalDirection direction)
        {
            return direction is CardinalDirection.East or CardinalDirection.NorthEast or CardinalDirection.SouthEast;
        }

        /// <summary>
        /// Determines whether the direction is predominantly facing left.
        /// </summary>
        public static bool IsLeftFacing(this CardinalDirection direction)
        {
            return direction is CardinalDirection.West or CardinalDirection.NorthWest or CardinalDirection.SouthWest;
        }

        /// <summary>
        /// Determines whether the direction has an upward component.
        /// </summary>
        public static bool IsUpwardFacing(this CardinalDirection direction)
        {
            return direction is CardinalDirection.North or CardinalDirection.NorthEast or CardinalDirection.NorthWest;
        }

        /// <summary>
        /// Determines whether the direction has a downward component.
        /// </summary>
        public static bool IsDownwardFacing(this CardinalDirection direction)
        {
            return direction is CardinalDirection.South or CardinalDirection.SouthEast or CardinalDirection.SouthWest;
        }

        /// <summary>
        /// Converts an angle in radians to the nearest eight-direction value.
        /// </summary>
        public static CardinalDirection FromAngle(float angleRadians)
        {
            float normalized = angleRadians;
            while (normalized < 0f)
            {
                normalized += MathF.Tau;
            }
            while (normalized >= MathF.Tau)
            {
                normalized -= MathF.Tau;
            }

            float degrees = MathHelper.ToDegrees(normalized);

            if (degrees < HalfSliceDegrees || degrees >= 360f - HalfSliceDegrees)
                return CardinalDirection.East;
            if (degrees < 45f + HalfSliceDegrees)
                return CardinalDirection.SouthEast;
            if (degrees < 90f + HalfSliceDegrees)
                return CardinalDirection.South;
            if (degrees < 135f + HalfSliceDegrees)
                return CardinalDirection.SouthWest;
            if (degrees < 180f + HalfSliceDegrees)
                return CardinalDirection.West;
            if (degrees < 225f + HalfSliceDegrees)
                return CardinalDirection.NorthWest;
            if (degrees < 270f + HalfSliceDegrees)
                return CardinalDirection.North;
            return CardinalDirection.NorthEast;
        }
    }
}