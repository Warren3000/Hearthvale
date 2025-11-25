using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hearthvale.GameCode.Tools
{
    /// <summary>
    /// Configuration manager for atlas generation
    /// </summary>
    public class AtlasConfigManager
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        /// <summary>
        /// Loads atlas configuration from JSON file
        /// </summary>
        /// <param name="configPath">Path to the configuration file</param>
        /// <returns>Loaded atlas configuration</returns>
        public static AtlasGenerator.AtlasConfig LoadConfig(string configPath)
        {
            if (!File.Exists(configPath))
                throw new FileNotFoundException($"Configuration file not found: {configPath}");

            var json = File.ReadAllText(configPath);
            var config = JsonSerializer.Deserialize<AtlasGenerator.AtlasConfig>(json, JsonOptions);

            // Validate configuration
            ValidateConfig(config);

            return config;
        }

        /// <summary>
        /// Saves atlas configuration to JSON file
        /// </summary>
        /// <param name="config">Configuration to save</param>
        /// <param name="configPath">Path where to save the configuration</param>
        public static void SaveConfig(AtlasGenerator.AtlasConfig config, string configPath)
        {
            ValidateConfig(config);

            var directory = Path.GetDirectoryName(configPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(config, JsonOptions);
            File.WriteAllText(configPath, json);
        }

        /// <summary>
        /// Creates a sample configuration for reference
        /// </summary>
        /// <param name="spritesheetPath">Path to the spritesheet image</param>
        /// <param name="outputPath">Path where to save the generated atlas XML</param>
        /// <param name="texturePath">Content path to the texture (used in the XML)</param>
        /// <returns>Sample configuration</returns>
        public static AtlasGenerator.AtlasConfig CreateSampleConfig(string spritesheetPath, string outputPath, string texturePath)
        {
            return new AtlasGenerator.AtlasConfig
            {
                SpritesheetPath = spritesheetPath,
                OutputPath = outputPath,
                TexturePath = texturePath,
                GridWidth = 32,
                GridHeight = 32,
                NamingPattern = "Sprite_{row}_{col}",
                IsGridBased = true,
                TrimTransparency = true,
                TransparencyThreshold = 0,
                Animations = new List<AtlasGenerator.AnimationConfig>
                {
                    new AtlasGenerator.AnimationConfig
                    {
                        Name = "SampleAnimation",
                        DelayMs = 100,
                        Pattern = "Sprite_0_.*", // Matches all sprites in row 0
                    }
                }
            };
        }

        /// <summary>
        /// Creates a configuration specifically for character animations
        /// </summary>
        public static AtlasGenerator.AtlasConfig CreateCharacterConfig(string spritesheetPath, string outputPath, string texturePath)
        {
            var config = new AtlasGenerator.AtlasConfig
            {
                SpritesheetPath = spritesheetPath,
                OutputPath = outputPath,
                TexturePath = texturePath,
                GridWidth = 32,
                GridHeight = 32,
                NamingPattern = "Char_{animation}_{direction}_{frame}",
                IsGridBased = true,
                TrimTransparency = true,
                TransparencyThreshold = 0,
                Animations = new List<AtlasGenerator.AnimationConfig>()
            };

            // Common character animations
            var directions = new[] { "Down", "Up", "Left", "Right" };
            var states = new[] { "Idle", "Walk", "Run", "Attack" };
            var delays = new Dictionary<string, int>
            {
                ["Idle"] = 150,
                ["Walk"] = 120,
                ["Run"] = 80,
                ["Attack"] = 100
            };

            foreach (var state in states)
            {
                foreach (var direction in directions)
                {
                    config.Animations.Add(new AtlasGenerator.AnimationConfig
                    {
                        Name = $"{state}_{direction}",
                        DelayMs = delays[state],
                        Pattern = $"Char_{state}_{direction}_.*"
                    });
                }
            }

            return config;
        }

        /// <summary>
        /// Creates a configuration for weapon icons
        /// </summary>
        public static AtlasGenerator.AtlasConfig CreateWeaponConfig(string spritesheetPath, string outputPath, string texturePath)
        {
            return new AtlasGenerator.AtlasConfig
            {
                SpritesheetPath = spritesheetPath,
                OutputPath = outputPath,
                TexturePath = texturePath,
                GridWidth = 64,
                GridHeight = 64,
                NamingPattern = "Weapon_{row}_{col}",
                IsGridBased = true,
                TrimTransparency = true,
                TransparencyThreshold = 0,
                Animations = new List<AtlasGenerator.AnimationConfig>()
            };
        }

        /// <summary>
        /// Validates the configuration for common issues
        /// </summary>
        private static void ValidateConfig(AtlasGenerator.AtlasConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            if (string.IsNullOrEmpty(config.SpritesheetPath))
                throw new ArgumentException("SpritesheetPath cannot be null or empty");

            if (string.IsNullOrEmpty(config.OutputPath))
                throw new ArgumentException("OutputPath cannot be null or empty");

            if (string.IsNullOrEmpty(config.TexturePath))
                throw new ArgumentException("TexturePath cannot be null or empty");

            if (config.IsGridBased && config.GridWidth <= 0 && config.GridHeight <= 0)
                throw new ArgumentException("For grid-based detection, at least one of GridWidth or GridHeight must be specified");

            if (config.TransparencyThreshold < 0 || config.TransparencyThreshold > 255)
                throw new ArgumentException("TransparencyThreshold must be between 0 and 255");

            if (config.MarginLeft < 0 || config.MarginTop < 0)
                throw new ArgumentException("Margins cannot be negative");

            // Validate animation configurations
            foreach (var anim in config.Animations)
            {
                if (string.IsNullOrEmpty(anim.Name))
                    throw new ArgumentException("Animation name cannot be null or empty");

                if (anim.DelayMs <= 0)
                    throw new ArgumentException($"Animation '{anim.Name}' must have a positive delay");

                if (anim.FrameCount < 0)
                    throw new ArgumentException($"Animation '{anim.Name}' cannot have negative frame count");

                if (anim.StartFrame < 0)
                    throw new ArgumentException($"Animation '{anim.Name}' cannot have negative start frame");
            }
        }

        /// <summary>
        /// Batch loads multiple configurations from a directory
        /// </summary>
        /// <param name="configDirectory">Directory containing .json config files</param>
        /// <returns>Dictionary of config name to configuration</returns>
        public static Dictionary<string, AtlasGenerator.AtlasConfig> LoadBatchConfigs(string configDirectory)
        {
            var configs = new Dictionary<string, AtlasGenerator.AtlasConfig>();

            if (!Directory.Exists(configDirectory))
                return configs;

            var configFiles = Directory.GetFiles(configDirectory, "*.json");

            foreach (var configFile in configFiles)
            {
                try
                {
                    var config = LoadConfig(configFile);
                    var configName = Path.GetFileNameWithoutExtension(configFile);
                    configs[configName] = config;
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine($"Warning: Failed to load config '{configFile}': {ex.Message}");
                }
            }

            return configs;
        }
    }

    /// <summary>
    /// Preset configurations for common spritesheet types
    /// </summary>
    public static class AtlasPresets
    {
        public static AtlasGenerator.AtlasConfig RPGCharacter32x32 => new AtlasGenerator.AtlasConfig
        {
            GridWidth = 32,
            GridHeight = 32,
            NamingPattern = "Char_{animation}_{direction}_{frame}",
            IsGridBased = true,
            TrimTransparency = false,
            TransparencyThreshold = 0
        };

        public static AtlasGenerator.AtlasConfig RPGCharacter48x48 => new AtlasGenerator.AtlasConfig
        {
            GridWidth = 48,
            GridHeight = 48,
            NamingPattern = "Char_{animation}_{direction}_{frame}",
            IsGridBased = true,
            TrimTransparency = false,
            TransparencyThreshold = 0
        };

        public static AtlasGenerator.AtlasConfig WeaponIcons64x64 => new AtlasGenerator.AtlasConfig
        {
            GridWidth = 64,
            GridHeight = 64,
            NamingPattern = "Weapon_{type}_{variant}",
            IsGridBased = true,
            TrimTransparency = true,
            TransparencyThreshold = 0
        };

        public static AtlasGenerator.AtlasConfig ItemIcons32x32 => new AtlasGenerator.AtlasConfig
        {
            GridWidth = 32,
            GridHeight = 32,
            NamingPattern = "Item_{category}_{name}",
            IsGridBased = true,
            TrimTransparency = true,
            TransparencyThreshold = 0
        };

        public static AtlasGenerator.AtlasConfig TileSet16x16 => new AtlasGenerator.AtlasConfig
        {
            GridWidth = 16,
            GridHeight = 16,
            NamingPattern = "Tile_{row}_{col}",
            IsGridBased = true,
            TrimTransparency = false,
            TransparencyThreshold = 0
        };

        public static AtlasGenerator.AtlasConfig UIElements => new AtlasGenerator.AtlasConfig
        {
            GridWidth = 0, // Auto-detect
            GridHeight = 0, // Auto-detect
            NamingPattern = "UI_{element}_{state}",
            IsGridBased = false, // Use packed detection
            TrimTransparency = true,
            TransparencyThreshold = 10
        };
    }
}