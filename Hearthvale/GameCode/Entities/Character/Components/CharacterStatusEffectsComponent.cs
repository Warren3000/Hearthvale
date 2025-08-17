using Hearthvale.GameCode.Entities.Interfaces;
using Hearthvale.GameCode.Entities.StatusEffects;
using Hearthvale.GameCode.Entities.Stats;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hearthvale.GameCode.Entities.Components
{
    public class CharacterStatusEffectsComponent : IStatusEffectHandler
    {
        private readonly Character _character;
        private readonly List<StatusEffect> _activeEffects = new();
        private readonly Dictionary<StatusEffectType, float> _resistances = new();

        // Visual positioning for effect icons
        private Vector2 _effectIconOffset = new Vector2(0, -40);
        private float _effectIconSpacing = 20f;

        public CharacterStatusEffectsComponent(Character character)
        {
            _character = character;
            InitializeDefaultResistances();
        }

        private void InitializeDefaultResistances()
        {
            // Default 0% resistance to all effect types
            foreach (StatusEffectType type in Enum.GetValues(typeof(StatusEffectType)))
            {
                _resistances[type] = 0f;
            }
        }

        public void ApplyEffect(StatusEffect effect)
        {
            if (effect == null || effect.Type == StatusEffectType.None)
                return;

            // Check for resistance (0.0-1.0 scale, where 1.0 means immune)
            float resistance = GetResistance(effect.Type);
            if (resistance >= 1.0f || (resistance > 0 && new Random().NextDouble() < resistance))
            {
                // Effect resisted
                return;
            }

            // Check if the effect already exists
            StatusEffect existingEffect = GetActiveEffect(effect.Type);

            if (existingEffect != null)
            {
                // Handle effect stacking or refreshing
                if (existingEffect.CanStack && existingEffect.StackCount < existingEffect.MaxStacks)
                {
                    existingEffect.AddStack();
                }
                else
                {
                    // Refresh duration if the new effect is stronger or has longer duration
                    if (effect.Intensity > existingEffect.Intensity ||
                        (effect.Duration > existingEffect.RemainingTime && !existingEffect.IsPermanent))
                    {
                        existingEffect.Refresh();
                    }
                }
            }
            else
            {
                // Apply new effect
                _activeEffects.Add(effect);

                // Execute the OnApply action if it exists
                effect.OnApplyEffect?.Invoke(_character);

                // Apply immediate stat modifiers if applicable
                ApplyStatModifiers(effect);
            }
        }

        public bool HasActiveEffect(StatusEffectType type)
        {
            return _activeEffects.Exists(e => e.Type == type && e.IsActive);
        }

        public void RemoveEffect(StatusEffectType type)
        {
            StatusEffect effect = GetActiveEffect(type);
            if (effect != null)
            {
                RemoveEffectInstance(effect);
            }
        }

        public void RemoveEffectById(string effectId)
        {
            StatusEffect effect = _activeEffects.FirstOrDefault(e => e.Id == effectId);
            if (effect != null)
            {
                RemoveEffectInstance(effect);
            }
        }

        private void RemoveEffectInstance(StatusEffect effect)
        {
            // Execute the OnRemove action if it exists
            effect.OnRemoveEffect?.Invoke(_character);

            // Remove the effect
            _activeEffects.Remove(effect);

            // Re-calculate stats after effect removal
            RecalculateStatModifiers();
        }

        public void RemoveAllEffects()
        {
            // Execute OnRemove actions for all active effects
            foreach (var effect in _activeEffects)
            {
                effect.OnRemoveEffect?.Invoke(_character);
            }

            _activeEffects.Clear();

            // Reset stats after clearing all effects
            RecalculateStatModifiers();
        }

        public void RemoveExpiredEffects()
        {
            var expiredEffects = _activeEffects.Where(e => !e.IsActive && !e.IsPermanent).ToList();
            foreach (var effect in expiredEffects)
            {
                RemoveEffectInstance(effect);
            }
        }

        public void RemoveEffectsOfCategory(bool removePositive, bool removeNegative)
        {
            var effectsToRemove = _activeEffects.Where(e =>
                (removePositive && e.IsPositive) || (removeNegative && !e.IsPositive)).ToList();

            foreach (var effect in effectsToRemove)
            {
                RemoveEffectInstance(effect);
            }
        }

        public void UpdateEffects(float deltaTime)
        {
            // Update each effect's timer
            foreach (var effect in _activeEffects.ToList()) // Use ToList to avoid collection modified exceptions
            {
                effect.Update(deltaTime);

                // Handle ongoing effects (damage over time, healing over time, etc.)
                if (effect.IsActive)
                {
                    // Execute the OnUpdate action if it exists
                    effect.OnUpdateEffect?.Invoke(_character, deltaTime);

                    // Apply periodic effects based on type
                    ApplyPeriodicEffect(effect, deltaTime);
                }
            }

            // Remove expired effects
            RemoveExpiredEffects();
        }

        private void ApplyPeriodicEffect(StatusEffect effect, float deltaTime)
        {
            // Handle damage-over-time and healing-over-time effects
            switch (effect.Type)
            {
                case StatusEffectType.Poison:
                    ApplyDamageOverTime(effect, deltaTime, 1.0f);
                    break;

                case StatusEffectType.Burn:
                    ApplyDamageOverTime(effect, deltaTime, 2.0f);
                    break;

                case StatusEffectType.Bleed:
                    ApplyDamageOverTime(effect, deltaTime, 1.0f);
                    break;

                case StatusEffectType.Regeneration:
                    ApplyHealingOverTime(effect, deltaTime, 1.0f);
                    break;

                    // Other effect types handled by stat modifications or OnUpdate actions
            }
        }

        private void ApplyDamageOverTime(StatusEffect effect, float deltaTime, float multiplier)
        {
            // Apply damage once per second
            float secondsElapsed = deltaTime;
            if (secondsElapsed > 0)
            {
                int damageAmount = (int)Math.Ceiling(effect.Intensity * effect.StackCount * multiplier * secondsElapsed);
                if (damageAmount > 0)
                {
                    _character.TakeDamage(damageAmount);
                }
            }
        }

        private void ApplyHealingOverTime(StatusEffect effect, float deltaTime, float multiplier)
        {
            // Apply healing once per second
            float secondsElapsed = deltaTime;
            if (secondsElapsed > 0)
            {
                int healAmount = (int)Math.Ceiling(effect.Intensity * effect.StackCount * multiplier * secondsElapsed);
                if (healAmount > 0)
                {
                    _character.Heal(healAmount);
                }
            }
        }

        public StatusEffect GetActiveEffect(StatusEffectType type)
        {
            return _activeEffects.FirstOrDefault(e => e.Type == type && e.IsActive);
        }

        public IEnumerable<StatusEffect> GetActiveEffects()
        {
            return _activeEffects.Where(e => e.IsActive);
        }

        public IEnumerable<StatusEffect> GetPositiveEffects()
        {
            return _activeEffects.Where(e => e.IsActive && e.IsPositive);
        }

        public IEnumerable<StatusEffect> GetNegativeEffects()
        {
            return _activeEffects.Where(e => e.IsActive && !e.IsPositive);
        }

        public void SetResistance(StatusEffectType type, float value)
        {
            _resistances[type] = MathHelper.Clamp(value, 0f, 1f);
        }

        public float GetResistance(StatusEffectType type)
        {
            return _resistances.TryGetValue(type, out float value) ? value : 0f;
        }

        // Apply stat modifiers from an effect
        private void ApplyStatModifiers(StatusEffect effect)
        {
            // Implementation depends on how you handle stats in your Character class
            // This is a placeholder - you'll need to adapt this to your stat system
            var modifiers = effect.GetAllModifiers();
            foreach (var modifier in modifiers)
            {
                // Apply the modifier to the character's stats
                // Example: _character.StatsComponent.AddModifier(modifier.Key, modifier.Value);
            }
        }

        // Recalculate all stat modifiers after effects change
        private void RecalculateStatModifiers()
        {
            // Implementation depends on how you handle stats in your Character class
            // This is a placeholder - you'll need to adapt this to your stat system

            // Example:
            // _character.StatsComponent.ClearAllModifiers();
            // foreach (var effect in _activeEffects)
            // {
            //     ApplyStatModifiers(effect);
            // }
        }

        // Draw status effect icons above the character
        public void DrawEffectIcons(SpriteBatch spriteBatch, Dictionary<string, Texture2D> effectIcons)
        {
            if (_activeEffects.Count == 0) return;

            Vector2 position = _character.Position + _effectIconOffset;
            int visibleEffects = 0;

            foreach (var effect in _activeEffects.Where(e => e.IsActive).Take(5)) // Limit display to 5 icons
            {
                if (effectIcons.TryGetValue(effect.IconTextureName, out var texture))
                {
                    // Draw icon
                    spriteBatch.Draw(
                        texture,
                        position + new Vector2(visibleEffects * _effectIconSpacing, 0),
                        null,
                        effect.EffectColor,
                        0f,
                        new Vector2(texture.Width / 2, texture.Height / 2),
                        0.5f, // Scale the icon
                        SpriteEffects.None,
                        0f
                    );

                    // Draw stack count if stacked
                    if (effect.StackCount > 1)
                    {
                        // Drawing stack count requires a font
                        // spriteBatch.DrawString(font, effect.StackCount.ToString(), position + new Vector2(...), Color.White);
                    }

                    visibleEffects++;
                }
            }
        }
    }
}