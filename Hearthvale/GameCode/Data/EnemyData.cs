using Hearthvale.GameCode.Entities.StatusEffects;
using System.Collections.Generic;

namespace Hearthvale.GameCode.Data.Models
{
    public class EnemyData
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public EnemyType Type { get; set; }
        public string Sprite { get; set; }  // Reference to sprite/animation asset
        public CreatureSize Size { get; set; }
        public string Faction { get; set; }  // e.g., "Undead", "Beast", "Humanoid"
        public int Level { get; set; }
        public int ExpValue { get; set; }

        // Core Stats
        public int MaxHealth { get; set; }
        public int AttackPower { get; set; }
        public int Defense { get; set; }
        public float MovementSpeed { get; set; }
        public float DetectionRange { get; set; }
        public float AttackRange { get; set; }

        // Combat Behavior
        public AttackPattern AttackPattern { get; set; }
        public List<StatusEffectResistance> Resistances { get; set; }
        public List<SpecialAbility> Abilities { get; set; }
        public CombatBehavior Behavior { get; set; }

        // Loot
        public List<LootDrop> LootTable { get; set; }
        public int GoldMin { get; set; }
        public int GoldMax { get; set; }

        // Environment
        public List<string> PreferredBiomes { get; set; }
        public SpawnConditions SpawnConditions { get; set; }
    }

    public class StatusEffectResistance
    {
        public StatusEffectType Type { get; set; }
        public float Resistance { get; set; } // 0.0 to 1.0, where 1.0 is immune
    }

    public class SpecialAbility
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public float Cooldown { get; set; }
        public float Chance { get; set; } // Chance to use when available
        public AbilityType Type { get; set; }
        public Dictionary<string, float> Effects { get; set; }
    }

    public class LootDrop
    {
        public string ItemId { get; set; }
        public float DropChance { get; set; }
        public int MinQuantity { get; set; }
        public int MaxQuantity { get; set; }
        public List<string> RequiredConditions { get; set; }
    }

    public class SpawnConditions
    {
        public bool RequiresDarkness { get; set; }
        public int MinDepth { get; set; }
        public List<string> RequiredNearbyTiles { get; set; }
        public float MinDistanceFromPlayer { get; set; }
    }

    public enum EnemyType
    {
        Normal,
        Elite,
        MiniBoss,
        Boss,
        Unique
    }

    public enum CreatureSize
    {
        Tiny,
        Small,
        Medium,
        Large,
        Huge
    }

    public enum AttackPattern
    {
        Melee,
        Ranged,
        Magic,
        Mixed
    }

    public enum CombatBehavior
    {
        Aggressive,    // Always attacks when in range
        Territorial,   // Attacks only when territory is invaded
        Cautious,      // Maintains distance, prefers ranged attacks
        Defensive,     // Attacks only when attacked
        Cowardly,     // Flees when damaged
        Pack,         // Coordinates with nearby allies
        Berserker     // More aggressive at low health
    }

    public enum AbilityType
    {
        Damage,
        Heal,
        Buff,
        Debuff,
        Summon,
        Movement,
        AreaEffect
    }
}