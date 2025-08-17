using Hearthvale.GameCode.Data;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System;
using System.Xml;
using System.Linq;
using Hearthvale.GameCode.Data.Models;

namespace Hearthvale.GameCode.Managers
{
    public class DataManager
    {
        private static DataManager _instance;
        public static DataManager Instance => _instance ?? throw new InvalidOperationException("DataManager not initialized. Call Initialize first.");

        private Dictionary<string, CharacterStats> _characterStats;
        private Dictionary<string, WeaponStats> _weaponStats;
        private Dictionary<string, Item> _items;
        private Dictionary<string, EnemyData> _enemies;

        private DataManager()
        {
            LoadContent();
        }

        /// <summary>
        /// Initializes the singleton instance. Call this once at startup.
        /// </summary>
        public static void Initialize()
        {
            _instance = new DataManager();
        }

        private void LoadContent()
        {
            var characterStatsJson = File.ReadAllText("Content/Data/CharacterStats.json");
            _characterStats = JsonSerializer.Deserialize<Dictionary<string, CharacterStats>>(characterStatsJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var weaponStatsJson = File.ReadAllText("Content/Data/WeaponStats.json");
            _weaponStats = JsonSerializer.Deserialize<Dictionary<string, WeaponStats>>(weaponStatsJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var itemsJson = File.ReadAllText("Content/Data/Items.json");
            _items = JsonSerializer.Deserialize<Dictionary<string, Item>>(itemsJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            // Load enemy data
            var enemiesXml = File.ReadAllText("Content/Data/enemies.xml");
            _enemies = LoadEnemiesFromXml(enemiesXml);
        }

        private Dictionary<string, EnemyData> LoadEnemiesFromXml(string xmlContent)
        {
            var enemies = new Dictionary<string, EnemyData>();
            var doc = new XmlDocument();
            doc.LoadXml(xmlContent);

            foreach (XmlNode enemyNode in doc.SelectNodes("//Enemy"))
            {
                var enemy = new EnemyData
                {
                    Id = enemyNode.Attributes["id"].Value,
                    Name = enemyNode.SelectSingleNode("Name").InnerText,
                    Description = enemyNode.SelectSingleNode("Description").InnerText,
                    Type = Enum.Parse<EnemyType>(enemyNode.SelectSingleNode("Type").InnerText),
                    // ... populate other fields from XML
                };

                enemies.Add(enemy.Id, enemy);
            }

            return enemies;
        }

        public CharacterStats GetCharacterStats(string name)
        {
            _characterStats.TryGetValue(name.ToLowerInvariant(), out var stats);
            return stats ?? new CharacterStats { MaxHealth = 10, AttackPower = 1, XpYield = 5 }; // Return default if not found
        }

        public WeaponStats GetWeaponStats(string name)
        {
            _weaponStats.TryGetValue(name, out var stats);
            return stats ?? new WeaponStats { BaseDamage = 1, Scale = 0.5f }; // Return default if not found
        }

        public Item GetItem(string id)
        {
            _items.TryGetValue(id, out var item);
            return item;
        }

        public EnemyData GetEnemyData(string id)
        {
            _enemies.TryGetValue(id, out var enemy);
            return enemy;
        }

        public IEnumerable<EnemyData> GetEnemiesByFaction(string faction)
        {
            return _enemies.Values.Where(e => e.Faction == faction);
        }

        public IEnumerable<EnemyData> GetEnemiesByLevel(int minLevel, int maxLevel)
        {
            return _enemies.Values.Where(e => e.Level >= minLevel && e.Level <= maxLevel);
        }
    }
}