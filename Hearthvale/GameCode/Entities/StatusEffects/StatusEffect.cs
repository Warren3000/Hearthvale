using Hearthvale.GameCode.Entities.Stats;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace Hearthvale.GameCode.Entities.StatusEffects
{
    /// <summary>
    /// Represents a status effect that can be applied to a character
    /// </summary>
    public class StatusEffect
    {
        // Basic properties
        public string Id { get; }
        public string Name { get; }
        public StatusEffectType Type { get; }
        public bool IsPositive => IsPositiveEffect(Type);
        public string Description { get; }

        // Timing
        public float Duration { get; private set; }
        public float RemainingTime { get; private set; }
        public bool IsActive => RemainingTime > 0;
        public bool IsPermanent { get; }

        // Effect strength
        public float Intensity { get; private set; }

        // Visual properties
        public Color EffectColor { get; }
        public string IconTextureName { get; }

        // Stat modifications
        private readonly Dictionary<StatType, int> _statModifiers = new();

        // Effect source
        public object Source { get; }

        // Stacking
        public int StackCount { get; private set; } = 1;
        public int MaxStacks { get; }
        public bool CanStack { get; }

        // Effect triggers
        public Action<Character, float> OnUpdateEffect { get; }
        public Action<Character> OnApplyEffect { get; }
        public Action<Character> OnRemoveEffect { get; }

        public StatusEffect(
            string id,
            string name,
            StatusEffectType type,
            float duration,
            float intensity = 1.0f,
            bool isPermanent = false,
            int maxStacks = 1,
            object source = null)
        {
            Id = id;
            Name = name;
            Type = type;
            Duration = duration;
            RemainingTime = duration;
            Intensity = intensity;
            IsPermanent = isPermanent;
            MaxStacks = maxStacks;
            CanStack = maxStacks > 1;
            Source = source;

            // Set default color based on effect type
            EffectColor = GetDefaultColor(type);

            // Set default icon name
            IconTextureName = $"effect_{type.ToString().ToLower()}";

            // Generate description
            Description = GenerateDescription();
        }

        public void Update(float deltaTime)
        {
            if (!IsPermanent && IsActive)
            {
                RemainingTime -= deltaTime;
                if (RemainingTime <= 0)
                {
                    RemainingTime = 0;
                }
            }
        }

        public void AddStack()
        {
            if (CanStack && StackCount < MaxStacks)
            {
                StackCount++;
                // Reset duration when stacking
                RemainingTime = Duration;
            }
        }

        public void SetModifier(StatType statType, int value)
        {
            _statModifiers[statType] = value;
        }

        public int GetModifier(StatType statType)
        {
            return _statModifiers.TryGetValue(statType, out int value) ? value : 0;
        }

        public Dictionary<StatType, int> GetAllModifiers()
        {
            return new Dictionary<StatType, int>(_statModifiers);
        }

        public void Refresh()
        {
            RemainingTime = Duration;
        }

        private static Color GetDefaultColor(StatusEffectType type)
        {
            return type switch
            {
                StatusEffectType.Poison => Color.Green,
                StatusEffectType.Burn => Color.OrangeRed,
                StatusEffectType.Freeze => Color.LightBlue,
                StatusEffectType.Stun => Color.Yellow,
                StatusEffectType.Slow => Color.DarkBlue,
                StatusEffectType.Bleed => Color.Red,
                StatusEffectType.Regeneration => Color.LightGreen,
                StatusEffectType.Haste => Color.Cyan,
                StatusEffectType.Strength => Color.Orange,
                StatusEffectType.Protection => Color.SteelBlue,
                StatusEffectType.Invisibility => Color.LightGray,
                StatusEffectType.Curse => Color.Purple,
                StatusEffectType.Blessing => Color.Gold,
                _ => Color.White
            };
        }

        private static bool IsPositiveEffect(StatusEffectType type)
        {
            return type switch
            {
                StatusEffectType.Regeneration => true,
                StatusEffectType.Haste => true,
                StatusEffectType.Strength => true,
                StatusEffectType.Protection => true,
                StatusEffectType.Invisibility => true,
                StatusEffectType.ManaShield => true,
                StatusEffectType.Blessing => true,
                StatusEffectType.Invulnerable => true,
                _ => false
            };
        }

        private string GenerateDescription()
        {
            string baseDesc = Type switch
            {
                StatusEffectType.Poison => $"Deals {Intensity} damage every second",
                StatusEffectType.Burn => $"Deals {Intensity * 2} fire damage every second",
                StatusEffectType.Bleed => $"Deals {Intensity} physical damage every second",
                StatusEffectType.Freeze => $"Reduces movement and attack speed by {Intensity * 50}%",
                StatusEffectType.Stun => "Cannot move or attack",
                StatusEffectType.Slow => $"Reduces movement speed by {Intensity * 30}%",
                StatusEffectType.Confusion => "Causes random movement direction",
                StatusEffectType.Regeneration => $"Restores {Intensity} health every second",
                StatusEffectType.Haste => $"Increases speed by {Intensity * 30}%",
                StatusEffectType.Strength => $"Increases attack power by {Intensity * 25}%",
                StatusEffectType.Protection => $"Reduces damage taken by {Intensity * 20}%",
                _ => "Applies a status effect"
            };

            if (IsPermanent)
                return $"{baseDesc} permanently";
            else
                return $"{baseDesc} for {Duration:0.0} seconds";
        }

        public void Draw(SpriteBatch spriteBatch, Vector2 position, Texture2D iconTexture = null)
        {
            // If implemented, draw effect icon and remaining time indicator
        }
    }
}