using System;
using System.IO;
using Hearthvale.GameCode.Tools;

namespace AtlasGeneratorTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Atlas Generator Tool Test");
            Console.WriteLine("=========================");

            try
            {
                // Test configuration creation
                var config = AtlasConfigManager.CreateSampleConfig(
                    @"C:\test\spritesheet.png",
                    @"C:\test\atlas.xml", 
                    "images/spritesheet"
                );

                config.GridWidth = 32;
                config.GridHeight = 32;

                Console.WriteLine("✓ Sample configuration created successfully");

                // Test configuration serialization
                var jsonOutput = System.Text.Json.JsonSerializer.Serialize(config, new System.Text.Json.JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });

                Console.WriteLine("✓ Configuration serializes to JSON successfully");
                Console.WriteLine("Sample JSON configuration:");
                Console.WriteLine(jsonOutput);

                // Test presets
                var characterPreset = AtlasPresets.RPGCharacter32x32;
                Console.WriteLine($"✓ Character preset loaded: {characterPreset.GridWidth}x{characterPreset.GridHeight}");

                var weaponPreset = AtlasPresets.WeaponIcons64x64;
                Console.WriteLine($"✓ Weapon preset loaded: {weaponPreset.GridWidth}x{weaponPreset.GridHeight}");

                Console.WriteLine("\nAtlas Generator tool is ready to use!");
                Console.WriteLine("\nTo generate an atlas:");
                Console.WriteLine("1. Create a configuration file using AtlasConfigManager.CreateSampleConfig()");
                Console.WriteLine("2. Modify the configuration as needed");
                Console.WriteLine("3. Call AtlasGenerator.GenerateAtlas(config)");
                Console.WriteLine("4. Save the result using AtlasGenerator.SaveAtlas()");

            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error: {ex.Message}");
                return;
            }
        }
    }
}