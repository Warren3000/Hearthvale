namespace Hearthvale.GameCode.Entities.Stats
{
    /// <summary>
    /// Defines all character statistics that can be modified
    /// </summary>
    public enum StatType
    {
        // Core attributes
        Health,
        MaxHealth,
        Mana,
        MaxMana,

        // Combat stats
        AttackPower,
        Defense,
        MagicPower,
        MagicResistance,
        CritChance,
        CritDamage,

        // Secondary attributes
        MovementSpeed,
        AttackSpeed,
        Accuracy,
        Evasion,

        // Resource related
        HealthRegen,
        ManaRegen,
        StaminaRegen,

        // Resistances
        PoisonResist,
        BurnResist,
        FreezeResist,
        StunResist,
        BleedResist,

        // Special attributes
        Luck,
        ItemDiscovery,
        ExperienceBonus,
        GoldBonus
    }
}