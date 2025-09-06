using Hearthvale.GameCode.Data;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System;
using System.Xml;
using System.Linq;
using Hearthvale.GameCode.Data.Models;
using Microsoft.Xna.Framework;

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
        private Dictionary<string, object> _dungeonData; // For dungeon-specific data
        private Dictionary<string, string> _dialogData; // For NPC/Quest dialogs

        private readonly string _contentRoot = "Content/Data";
        private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

        private DataManager()
        {
            InitializeCollections();
            LoadAllContent();
        }

        /// <summary>
        /// Initializes the singleton instance. Call this once at startup.
        /// </summary>
        public static void Initialize()
        {
            _instance = new DataManager();
        }

        private void InitializeCollections()
        {
            _characterStats = new Dictionary<string, CharacterStats>();
            _weaponStats = new Dictionary<string, WeaponStats>();
            _items = new Dictionary<string, Item>();
            _enemies = new Dictionary<string, EnemyData>();
            _dungeonData = new Dictionary<string, object>();
            _dialogData = new Dictionary<string, string>();
        }

        private void LoadAllContent()
        {
            try
            {
                // Load character data
                LoadCharacterData();
                
                // Load item data
                LoadItemData();
                
                // Load enemy data
                LoadEnemyData();
                
                // Load dungeon data
                LoadDungeonData();
                
                // Load dialog data
                LoadDialogData();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading game data: {ex.Message}");
                throw new InvalidOperationException("Failed to load game data", ex);
            }
        }

        private void LoadCharacterData()
        {
            // Load main character stats
            var characterStatsPath = Path.Combine(_contentRoot, "Characters/CharacterStats.json");
            if (File.Exists(characterStatsPath))
            {
                _characterStats = LoadJsonFile<Dictionary<string, CharacterStats>>(characterStatsPath);
            }

            // Load NPC data if needed
            LoadDataFromDirectory("Characters/NPCs", (file, data) =>
            {
                var npcData = JsonSerializer.Deserialize<Dictionary<string, CharacterStats>>(data, _jsonOptions);
                foreach (var kvp in npcData)
                {
                    _characterStats[$"npc_{kvp.Key}"] = kvp.Value;
                }
            });
        }

        private void LoadItemData()
        {
            // Load main items file
            var itemsPath = Path.Combine(_contentRoot, "Items/Items.json");
            if (File.Exists(itemsPath))
            {
                _items = LoadJsonFile<Dictionary<string, Item>>(itemsPath);
            }

            // Load weapon stats
            var weaponStatsPath = Path.Combine(_contentRoot, "Items/Weapons/WeaponStats.json");
            if (File.Exists(weaponStatsPath))
            {
                _weaponStats = LoadJsonFile<Dictionary<string, WeaponStats>>(weaponStatsPath);
            }

            // Load additional weapon categories
            LoadDataFromDirectory("Items/Weapons", (file, data) =>
            {
                if (Path.GetFileName(file) != "WeaponStats.json")
                {
                    var weapons = JsonSerializer.Deserialize<Dictionary<string, Item>>(data, _jsonOptions);
                    foreach (var kvp in weapons)
                    {
                        _items[kvp.Key] = kvp.Value;
                    }
                }
            });

            // Load consumables
            var consumablesPath = Path.Combine(_contentRoot, "Items/Consumables.json");
            if (File.Exists(consumablesPath))
            {
                var consumables = LoadJsonFile<Dictionary<string, Item>>(consumablesPath);
                foreach (var kvp in consumables)
                {
                    _items[kvp.Key] = kvp.Value;
                }
            }
        }

        private void LoadEnemyData()
        {
            _enemies = new Dictionary<string, EnemyData>();

            // Load main enemies file (XML for now, but consider converting to JSON)
            var enemiesXmlPath = Path.Combine(_contentRoot, "Characters/Enemies/enemies.xml");
            if (File.Exists(enemiesXmlPath))
            {
                var xmlContent = File.ReadAllText(enemiesXmlPath);
                _enemies = LoadEnemiesFromXml(xmlContent);
            }

            // Load faction-specific enemies
            LoadFactionEnemies();

            // Load boss data
            var bossesPath = Path.Combine(_contentRoot, "Characters/Enemies/Bosses.json");
            if (File.Exists(bossesPath))
            {
                var bosses = LoadJsonFile<Dictionary<string, EnemyData>>(bossesPath);
                foreach (var kvp in bosses)
                {
                    _enemies[kvp.Key] = kvp.Value;
                }
            }
        }

        private void LoadFactionEnemies()
        {
            var factionPath = Path.Combine(_contentRoot, "Characters/Enemies/Factions");
            LoadDataFromDirectory(factionPath, (file, data) =>
            {
                var factionEnemies = JsonSerializer.Deserialize<Dictionary<string, EnemyData>>(data, _jsonOptions);
                foreach (var kvp in factionEnemies)
                {
                    _enemies[kvp.Key] = kvp.Value;
                }
            });
        }

        private void LoadDungeonData()
        {
            // Load dungeon element configurations
            LoadDataFromDirectory("Dungeons/Elements", (file, data) =>
            {
                var elementType = Path.GetFileNameWithoutExtension(file);
                _dungeonData[elementType] = JsonSerializer.Deserialize<object>(data, _jsonOptions);
            });

            // Load procedural generation rules
            var rulesPath = Path.Combine(_contentRoot, "Dungeons/Layouts/ProceduralRules.json");
            if (File.Exists(rulesPath))
            {
                _dungeonData["ProceduralRules"] = LoadJsonFile<object>(rulesPath);
            }
        }

        private void LoadDialogData()
        {
            // Load NPC dialogs
            var npcDialogsPath = Path.Combine(_contentRoot, "Dialog/NPCDialogs.json");
            if (File.Exists(npcDialogsPath))
            {
                var dialogs = LoadJsonFile<Dictionary<string, string>>(npcDialogsPath);
                foreach (var kvp in dialogs)
                {
                    _dialogData[kvp.Key] = kvp.Value;
                }
            }

            // Load quest dialogs
            var questDialogsPath = Path.Combine(_contentRoot, "Dialog/QuestDialogs.json");
            if (File.Exists(questDialogsPath))
            {
                var questDialogs = LoadJsonFile<Dictionary<string, string>>(questDialogsPath);
                foreach (var kvp in questDialogs)
                {
                    _dialogData[$"quest_{kvp.Key}"] = kvp.Value;
                }
            }
        }

        /// <summary>
        /// Loads all JSON files from a directory and processes them with the provided action
        /// </summary>
        private void LoadDataFromDirectory(string relativePath, Action<string, string> processFile)
        {
            var fullPath = Path.Combine(_contentRoot, relativePath);
            if (Directory.Exists(fullPath))
            {
                foreach (var file in Directory.GetFiles(fullPath, "*.json"))
                {
                    try
                    {
                        var data = File.ReadAllText(file);
                        processFile(file, data);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error loading file {file}: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Generic method to load JSON data from a file
        /// </summary>
        private T LoadJsonFile<T>(string path)
        {
            try
            {
                var json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<T>(json, _jsonOptions);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading JSON from {path}: {ex.Message}");
                return default(T);
            }
        }

        private Dictionary<string, EnemyData> LoadEnemiesFromXml(string xmlContent)
        {
            var enemies = new Dictionary<string, EnemyData>();
            var doc = new XmlDocument();
            doc.LoadXml(xmlContent);

            foreach (XmlNode enemyNode in doc.SelectNodes("//Enemy"))
            {
                try
                {
                    var enemy = new EnemyData
                    {
                        Id = enemyNode.Attributes["id"]?.Value,
                        Name = enemyNode.SelectSingleNode("Name")?.InnerText,
                        Description = enemyNode.SelectSingleNode("Description")?.InnerText,
                        Type = Enum.Parse<EnemyType>(enemyNode.SelectSingleNode("Type")?.InnerText ?? "Basic"),
                        Sprite = enemyNode.SelectSingleNode("Sprite")?.InnerText,
                        Size = Enum.Parse<CreatureSize>(enemyNode.SelectSingleNode("Size")?.InnerText ?? "Medium"),
                        Faction = enemyNode.SelectSingleNode("Faction")?.InnerText ?? "Neutral",
                        Level = int.Parse(enemyNode.SelectSingleNode("Level")?.InnerText ?? "1"),
                        ExpValue = int.Parse(enemyNode.SelectSingleNode("ExpValue")?.InnerText ?? "10"),
                        MaxHealth = int.Parse(enemyNode.SelectSingleNode("MaxHealth")?.InnerText ?? "10"),
                        AttackPower = int.Parse(enemyNode.SelectSingleNode("AttackPower")?.InnerText ?? "1"),
                        Defense = int.Parse(enemyNode.SelectSingleNode("Defense")?.InnerText ?? "0"),
                        MovementSpeed = float.Parse(enemyNode.SelectSingleNode("MovementSpeed")?.InnerText ?? "1.0"),
                        DetectionRange = float.Parse(enemyNode.SelectSingleNode("DetectionRange")?.InnerText ?? "5.0"),
                        AttackRange = float.Parse(enemyNode.SelectSingleNode("AttackRange")?.InnerText ?? "1.0"),
                        GoldMin = int.Parse(enemyNode.SelectSingleNode("GoldMin")?.InnerText ?? "0"),
                        GoldMax = int.Parse(enemyNode.SelectSingleNode("GoldMax")?.InnerText ?? "5")
                    };

                    if (!string.IsNullOrEmpty(enemy.Id))
                    {
                        enemies[enemy.Id] = enemy;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error parsing enemy node: {ex.Message}");
                }
            }

            return enemies;
        }

        // Accessor methods remain the same
        public CharacterStats GetCharacterStats(string name)
        {
            _characterStats.TryGetValue(name.ToLowerInvariant(), out var stats);
            return stats ?? new CharacterStats { MaxHealth = 10, AttackPower = 1, XpYield = 5 };
        }

        public WeaponStats GetWeaponStats(string name)
        {
            _weaponStats.TryGetValue(name, out var stats);
            return stats ?? new WeaponStats { BaseDamage = 1, Scale = 0.5f };
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

        // New methods for accessing additional data
        public T GetDungeonData<T>(string key) where T : class
        {
            _dungeonData.TryGetValue(key, out var data);
            return data as T;
        }

        public string GetDialog(string key)
        {
            _dialogData.TryGetValue(key, out var dialog);
            return dialog;
        }

        /// <summary>
        /// Reloads specific data category without restarting the game (useful for development)
        /// </summary>
        public void ReloadDataCategory(string category)
        {
            switch (category.ToLower())
            {
                case "characters":
                    LoadCharacterData();
                    break;
                case "items":
                    LoadItemData();
                    break;
                case "enemies":
                    LoadEnemyData();
                    break;
                case "dungeons":
                    LoadDungeonData();
                    break;
                case "dialogs":
                    LoadDialogData();
                    break;
                default:
                    LoadAllContent();
                    break;
            }
        }
    }
}