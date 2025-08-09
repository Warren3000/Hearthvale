using System.Collections.Generic;

/// <summary>
/// Configuration data for different NPC types
/// </summary>

namespace Hearthvale.GameCode.Entities.NPCs;
internal static class NpcConfiguration
{
    private static readonly Dictionary<string, (string AnimationPrefix, int Health)> Configurations = new()
        {
            { "merchant", ("Merchant", 8) },
            { "mage", ("Mage", 12) },
            { "archer", ("Archer", 10) },
            { "blacksmith", ("Blacksmith", 10) },
            { "knight", ("Knight", 20) },
            { "heavyknight", ("HeavyKnight", 10) },
            { "fatnun", ("FatNun", 10) }
        };

    public static (string AnimationPrefix, int Health) GetConfiguration(string npcType)
    {
        return Configurations.TryGetValue(npcType.ToLower(), out var config) ? config : ("Merchant", 10);
    }
}