using Hearthvale.GameCode.Data;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Hearthvale.GameCode.Managers
{
    public static class DataManager
    {
        private static Dictionary<string, CharacterStats> _characterStats;
        private static Dictionary<string, WeaponStats> _weaponStats;

        public static void LoadContent()
        {
            var characterStatsJson = File.ReadAllText("Content/Data/CharacterStats.json");
            _characterStats = JsonSerializer.Deserialize<Dictionary<string, CharacterStats>>(characterStatsJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var weaponStatsJson = File.ReadAllText("Content/Data/WeaponStats.json");
            _weaponStats = JsonSerializer.Deserialize<Dictionary<string, WeaponStats>>(weaponStatsJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        public static CharacterStats GetCharacterStats(string name)
        {
            _characterStats.TryGetValue(name.ToLowerInvariant(), out var stats);
            return stats ?? new CharacterStats { MaxHealth = 10, AttackPower = 1, XpYield = 5 }; // Return default if not found
        }

        public static WeaponStats GetWeaponStats(string name)
        {
            _weaponStats.TryGetValue(name, out var stats);
            return stats ?? new WeaponStats { BaseDamage = 1, Scale = 0.5f }; // Return default if not found
        }
    }
}