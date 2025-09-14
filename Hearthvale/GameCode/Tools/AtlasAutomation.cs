using System;
using System.IO;
using Hearthvale.GameCode.Tools;

namespace Hearthvale.GameCode.Tools
{
    /// <summary>
    /// Helper class to automate atlas generation for your existing spritesheets
    /// </summary>
    public static class AtlasAutomation
    {
        /// <summary>
        /// Regenerates all atlas files for the project's existing spritesheets
        /// Call this during development when spritesheets change
        /// </summary>
        public static void RegenerateAllAtlases()
        {
            Console.WriteLine("Regenerating all texture atlases...");

            try
            {
                // Character atlas
                GenerateCharacterAtlas();
                
                // Weapon atlas  
                GenerateWeaponAtlas();
                
                // Add more atlases as needed
                Console.WriteLine("✓ All atlases regenerated successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error regenerating atlases: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Generates the character atlas from your existing Character.png spritesheet
        /// </summary>
        public static void GenerateCharacterAtlas()
        {
            var config = new AtlasGenerator.AtlasConfig
            {
                SpritesheetPath = "Content/images/sprites/Character.png",
                OutputPath = "Content/images/xml/character-atlas-generated.xml",
                TexturePath = "images/sprites/Character",
                GridWidth = 32,
                GridHeight = 32,
                NamingPattern = "Char_{animation}_{direction}_{variant}_{frame}",
                IsGridBased = true,
                TrimTransparency = false,
                TransparencyThreshold = 0
            };

            // Add character animations based on your existing pattern
            var animations = new[]
            {
                ("Idle_Down_NH", 100, new[] { "Char_Idle_Down_NH_1", "Char_Idle_Down_NH_2", "Char_Idle_Down_NH_3", "Char_Idle_Down_NH_4", "Char_Idle_Down_NH_5", "Char_Idle_Down_NH_6" }),
                ("Idle_Down", 100, new[] { "Char_Idle_Down_1", "Char_Idle_Down_2", "Char_Idle_Down_3", "Char_Idle_Down_4", "Char_Idle_Down_5", "Char_Idle_Down_6" }),
                ("Run_Down_NH", 80, new[] { "Char_Run_Down_NH_1", "Char_Run_Down_NH_2", "Char_Run_Down_NH_3", "Char_Run_Down_NH_4", "Char_Run_Down_NH_5", "Char_Run_Down_NH_6" }),
                ("Run_Down", 80, new[] { "Char_Run_Down_1", "Char_Run_Down_2", "Char_Run_Down_3", "Char_Run_Down_4", "Char_Run_Down_5", "Char_Run_Down_6" }),
                // Add more animations as needed
            };

            foreach (var (name, delay, frames) in animations)
            {
                config.Animations.Add(new AtlasGenerator.AnimationConfig
                {
                    Name = name,
                    DelayMs = delay,
                    FrameNames = new System.Collections.Generic.List<string>(frames)
                });
            }

            if (File.Exists(config.SpritesheetPath))
            {
                var xml = AtlasGenerator.GenerateAtlas(config);
                AtlasGenerator.SaveAtlas(config, xml);
                Console.WriteLine("✓ Character atlas generated");
            }
            else
            {
                Console.WriteLine($"⚠ Character spritesheet not found: {config.SpritesheetPath}");
            }
        }

        /// <summary>
        /// Generates the weapon atlas from your existing WeaponsSpriteSheet.png
        /// </summary>
        public static void GenerateWeaponAtlas()
        {
            var config = new AtlasGenerator.AtlasConfig
            {
                SpritesheetPath = "Content/images/atlases/WeaponsSpriteSheet.png",
                OutputPath = "Content/images/xml/weapon-atlas-generated.xml",
                TexturePath = "images/atlases/WeaponsSpriteSheet",
                GridWidth = 64,
                GridHeight = 64,
                NamingPattern = "Weapon_{type}_{variant}",
                IsGridBased = true,
                TrimTransparency = true,
                TransparencyThreshold = 0
            };

            // Add weapon animations (simple single-frame "animations")
            var weapons = new[] { "Dagger", "Dagger-Steel", "Dagger-Copper", "Dagger-Cold", "Dagger-Obsidian", "Dagger-Holy", "Dagger-Gold", "Dagger-Fire", "Dagger-Dark" };
            
            foreach (var weapon in weapons)
            {
                config.Animations.Add(new AtlasGenerator.AnimationConfig
                {
                    Name = $"{weapon}_Attack",
                    DelayMs = 120,
                    FrameNames = new System.Collections.Generic.List<string> { weapon }
                });
            }

            if (File.Exists(config.SpritesheetPath))
            {
                var xml = AtlasGenerator.GenerateAtlas(config);
                AtlasGenerator.SaveAtlas(config, xml);
                Console.WriteLine("✓ Weapon atlas generated");
            }
            else
            {
                Console.WriteLine($"⚠ Weapon spritesheet not found: {config.SpritesheetPath}");
            }
        }

        /// <summary>
        /// Compares a generated atlas with the existing one to see what changed
        /// </summary>
        /// <param name="generatedPath">Path to newly generated atlas</param>
        /// <param name="existingPath">Path to existing atlas</param>
        /// <returns>True if files are different</returns>
        public static bool HasAtlasChanged(string generatedPath, string existingPath)
        {
            if (!File.Exists(existingPath))
                return true;

            if (!File.Exists(generatedPath))
                return false;

            var generatedContent = File.ReadAllText(generatedPath);
            var existingContent = File.ReadAllText(existingPath);

            return !string.Equals(generatedContent, existingContent, StringComparison.Ordinal);
        }

        /// <summary>
        /// Creates configuration files for later use
        /// </summary>
        public static void CreateConfigurationFiles()
        {
            var configDir = "Content/atlas-configs/";
            Directory.CreateDirectory(configDir);

            // Character config
            var characterConfig = AtlasConfigManager.CreateCharacterConfig(
                "Content/images/sprites/Character.png",
                "Content/images/xml/character-atlas-generated.xml",
                "images/sprites/Character"
            );
            AtlasConfigManager.SaveConfig(characterConfig, Path.Combine(configDir, "character.json"));

            // Weapon config
            var weaponConfig = AtlasConfigManager.CreateWeaponConfig(
                "Content/images/atlases/WeaponsSpriteSheet.png",
                "Content/images/xml/weapon-atlas-generated.xml", 
                "images/atlases/WeaponsSpriteSheet"
            );
            AtlasConfigManager.SaveConfig(weaponConfig, Path.Combine(configDir, "weapons.json"));

            Console.WriteLine($"✓ Configuration files created in {configDir}");
        }
    }
}