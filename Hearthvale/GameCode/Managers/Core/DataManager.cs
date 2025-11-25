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
        private Dictionary<string, AttackTimingProfile> _enemyAttackProfiles;
        private Dictionary<string, AttackTimingProfile> _playerAttackProfiles;
        private int _enemyAttackProfileSchemaVersion = 1;
        private Dictionary<string, object> _dungeonData; // For dungeon-specific data
        private Dictionary<string, string> _dialogData; // For NPC/Quest dialogs

        private string _contentRoot = "Content/Data";
        private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

        private DataManager()
        {
#if DEBUG
            // In debug mode, try to load from source to enable hot reloading
            // Go up from bin/Debug/net6.0/ to project root
            try 
            {
                var debugContentRoot = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../Content/Data"));
                if (Directory.Exists(debugContentRoot))
                {
                    _contentRoot = debugContentRoot;
                    System.Diagnostics.Debug.WriteLine($"[DataManager] Hot Reload Enabled. Watching: {_contentRoot}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DataManager] Failed to resolve debug content path: {ex.Message}");
            }
#endif
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
            _enemyAttackProfiles = new Dictionary<string, AttackTimingProfile>(StringComparer.OrdinalIgnoreCase);
            _playerAttackProfiles = new Dictionary<string, AttackTimingProfile>(StringComparer.OrdinalIgnoreCase);
            _dungeonData = new Dictionary<string, object>();
            _dialogData = new Dictionary<string, string>();
        }

        private void LoadAllContent()
        {
            try
            {
                // Load character data
                LoadCharacterData();
                LoadPlayerAttackProfiles();
                
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

        private void LoadPlayerAttackProfiles()
        {
            _playerAttackProfiles.Clear();
            var path = Path.Combine(_contentRoot, "Characters/Players/PlayerAttackProfiles.json");
            if (!File.Exists(path)) return;

            try
            {
                var wrapped = LoadJsonFile<AttackProfilesFile>(path);
                if (wrapped != null)
                {
                    var resolved = wrapped.ResolveProfiles(_jsonOptions);
                    foreach (var (key, profile) in resolved)
                    {
                        _playerAttackProfiles[key] = profile;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading player attack profiles: {ex.Message}");
            }
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

            LoadEnemyAttackProfiles();
        }

        private void LoadEnemyAttackProfiles()
        {
            _enemyAttackProfiles.Clear();

            var attackProfilesPath = Path.Combine(_contentRoot, "Characters/Enemies/AttackProfiles.json");
            if (!File.Exists(attackProfilesPath))
            {
                return;
            }

            try
            {
                var wrappedProfiles = LoadJsonFile<AttackProfilesFile>(attackProfilesPath);
                if (wrappedProfiles != null)
                {
                    var resolved = wrappedProfiles.ResolveProfiles(_jsonOptions);
                    if (resolved.Count > 0)
                    {
                        PopulateEnemyAttackProfiles(resolved);
                        _enemyAttackProfileSchemaVersion = Math.Max(1, wrappedProfiles.SchemaVersion);
                        return;
                    }
                }

                // Legacy fallback: older files may still be a flat dictionary
                var legacyProfiles = LoadJsonFile<Dictionary<string, AttackTimingProfile>>(attackProfilesPath);
                if (legacyProfiles == null)
                {
                    return;
                }

                PopulateEnemyAttackProfiles(legacyProfiles);
                _enemyAttackProfileSchemaVersion = 1;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading enemy attack profiles: {ex.Message}");
            }
        }

        private void PopulateEnemyAttackProfiles(Dictionary<string, AttackTimingProfile> source)
        {
            foreach (var (key, profile) in source)
            {
                if (string.IsNullOrWhiteSpace(key) || profile == null)
                {
                    continue;
                }

                _enemyAttackProfiles[key.ToLowerInvariant()] = profile;
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

        public AttackTimingProfile GetEnemyAttackProfile(string idOrType)
        {
            if (string.IsNullOrWhiteSpace(idOrType))
            {
                return null;
            }

            var key = idOrType.Trim();

            if (_enemyAttackProfiles.TryGetValue(key.ToLowerInvariant(), out var profile))
            {
                return profile;
            }

            // Allow lookup by enemy id (e.g., skeleton_warrior) if a more specific entry exists
            var enemy = _enemies.FirstOrDefault(e => string.Equals(e.Key, key, StringComparison.OrdinalIgnoreCase) || string.Equals(e.Value?.Id, key, StringComparison.OrdinalIgnoreCase)).Value;
            if (enemy != null && !string.IsNullOrWhiteSpace(enemy.Id))
            {
                var shortId = enemy.Id.ToLowerInvariant();
                if (_enemyAttackProfiles.TryGetValue(shortId, out var specificProfile))
                {
                    return specificProfile;
                }
            }

            return null;
        }

        public AttackTimingProfile GetPlayerAttackProfile(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            _playerAttackProfiles.TryGetValue(id, out var profile);
            return profile;
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
                    LoadPlayerAttackProfiles();
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