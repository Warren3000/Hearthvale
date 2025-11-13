using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace Hearthvale.GameCode.Entities.NPCs
{
    /// <summary>
    /// Centralized configuration for NPC movement speeds
    /// </summary>
    public static class NpcSpeedConfiguration
    {
        // Speed constraints
        public const float MIN_SPEED = 1f;
        public const float MAX_SPEED = 50f; // Maximum reasonable speed for NPCs

        // Default speeds for each NPC class
        private static readonly Dictionary<NpcClass, SpeedProfile> _speedProfiles = new()
        {
            {
                NpcClass.Skeleton,
                new SpeedProfile
                {
                    WanderSpeed = 12f,  // Increased for more dynamic movement
                    ChaseSpeed = 35f,   // Higher chase speed for more aggressive pursuit
                    FleeSpeed = 20f,    // Increased flee speed
                    MovementSpeed = 2f
                }
            },
            // {
            //     NpcClass.HeavyKnight,
            //     new SpeedProfile
            //     {
            //         WanderSpeed = 8f,
            //         ChaseSpeed = 25f,   // Slower but still aggressive
            //         FleeSpeed = 15f,
            //         MovementSpeed = 1.5f
            //     }
            // },
            // {
            //     NpcClass.Merchant,
            //     new SpeedProfile
            //     {
            //         WanderSpeed = 10f,
            //         ChaseSpeed = 8f,    // Merchants shouldn't be aggressive chasers
            //         FleeSpeed = 25f,    // But they should flee quickly
            //         MovementSpeed = 1.8f
            //     }
            // }
        };

        public static SpeedProfile GetSpeedProfile(NpcClass npcClass)
        {
            if (_speedProfiles.TryGetValue(npcClass, out var profile))
            {
                return profile.Clone(); // Return a copy to prevent modification
            }

            // Fallback to merchant speeds
            return _speedProfiles[NpcClass.Skeleton].Clone();
        }

        public static bool IsValidSpeed(float speed)
        {
            return speed >= MIN_SPEED && speed <= MAX_SPEED && !float.IsNaN(speed) && !float.IsInfinity(speed);
        }

        public static float ClampSpeed(float speed)
        {
            if (float.IsNaN(speed) || float.IsInfinity(speed))
                return MIN_SPEED;

            return MathHelper.Clamp(speed, MIN_SPEED, MAX_SPEED);
        }
    }

    public class SpeedProfile
    {
        public float WanderSpeed { get; set; }
        public float ChaseSpeed { get; set; }
        public float FleeSpeed { get; set; }
        public float MovementSpeed { get; set; }

        public SpeedProfile Clone()
        {
            return new SpeedProfile
            {
                WanderSpeed = WanderSpeed,
                ChaseSpeed = ChaseSpeed,
                FleeSpeed = FleeSpeed,
                MovementSpeed = MovementSpeed
            };
        }

        public void Validate()
        {
            WanderSpeed = NpcSpeedConfiguration.ClampSpeed(WanderSpeed);
            ChaseSpeed = NpcSpeedConfiguration.ClampSpeed(ChaseSpeed);
            FleeSpeed = NpcSpeedConfiguration.ClampSpeed(FleeSpeed);
            MovementSpeed = NpcSpeedConfiguration.ClampSpeed(MovementSpeed);
        }
    }
}