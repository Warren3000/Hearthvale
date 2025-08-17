using System;

namespace Hearthvale.GameCode.Entities.StatusEffects
{
    /// <summary>
    /// Defines all possible status effect types that can be applied to characters
    /// </summary>
    public enum StatusEffectType
    {
        None = 0,

        // Harmful effects
        Poison,     // Damage over time
        Burn,       // Fire damage over time, higher damage but shorter duration
        Bleed,      // Physical damage over time, can stack
        Freeze,     // Slows movement and attack speed
        Stun,       // Cannot move or attack
        Slow,       // Reduced movement speed
        Confusion,  // Random movement direction
        Weakness,   // Reduced attack power
        Blind,      // Reduced accuracy
        Fear,       // Forces target to flee

        // Beneficial effects
        Regeneration,   // Health recovery over time
        Haste,          // Increased movement and attack speed
        Strength,       // Increased attack power
        Protection,     // Damage reduction
        Invisibility,   // Cannot be targeted by enemies
        ManaShield,     // Damage is taken from mana instead of health

        // Special effects
        Curse,          // Custom negative effect defined by the source
        Blessing,       // Custom positive effect defined by the source
        Charged,        // Building up energy for a special attack
        Invulnerable    // Cannot take damage
    }
}