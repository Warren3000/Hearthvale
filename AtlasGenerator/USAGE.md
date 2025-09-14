# Atlas Generator - Usage Guide

## Overview

The Atlas Generator successfully automates the creation of MonoGame texture atlas XML files from spritesheets. The tool is now fully functional and ready to use!

## ✅ What's Working

✅ **Core AtlasGenerator class** - Automatic sprite detection and XML generation  
✅ **Configuration system** - JSON-based configuration with presets  
✅ **Grid-based sprite detection** - Automatic detection of 32x32, 64x64, etc. sprites  
✅ **Animation support** - Generate animation sequences from sprite patterns  
✅ **Transparency trimming** - Automatically remove transparent pixels  
✅ **Preset configurations** - Ready-made configs for RPG characters, weapons, etc.  
✅ **Integration with existing TextureAtlas system** - Generated XML works with your existing code  

## Quick Start

### 1. Basic Usage in Code

```csharp
using Hearthvale.GameCode.Tools;

// Create configuration
var config = AtlasConfigManager.CreateSampleConfig(
    @"Content/images/sprites/Character.png",        // Source spritesheet
    @"Content/images/xml/character-atlas.xml",      // Output XML
    "images/sprites/Character"                      // Content path for XML
);

// Customize settings
config.GridWidth = 32;
config.GridHeight = 32;
config.NamingPattern = "Char_{row}_{col}";
config.TrimTransparency = false;

// Generate the atlas XML
string xmlContent = AtlasGenerator.GenerateAtlas(config);

// Save to file
AtlasGenerator.SaveAtlas(config, xmlContent);
```

### 2. Using Preset Configurations

```csharp
// Use a preset for RPG characters
var characterConfig = AtlasPresets.RPGCharacter32x32;
characterConfig.SpritesheetPath = @"Content/images/sprites/hero.png";
characterConfig.OutputPath = @"Content/images/xml/hero-atlas.xml";
characterConfig.TexturePath = "images/sprites/hero";

// Generate atlas
var xml = AtlasGenerator.GenerateAtlas(characterConfig);
AtlasGenerator.SaveAtlas(characterConfig, xml);
```

### 3. JSON Configuration Files

Create a configuration file (`hero-config.json`):

```json
{
  "spritesheetPath": "Content/images/sprites/hero.png",
  "outputPath": "Content/images/xml/hero-atlas.xml",
  "texturePath": "images/sprites/hero",
  "gridWidth": 32,
  "gridHeight": 32,
  "namingPattern": "Hero_{state}_{direction}_{frame}",
  "isGridBased": true,
  "trimTransparency": false,
  "transparencyThreshold": 0,
  "animations": [
    {
      "name": "Idle_Down",
      "delayMs": 100,
      "pattern": "Hero_Idle_Down_.*"
    },
    {
      "name": "Walk_Right", 
      "delayMs": 80,
      "frameNames": ["Hero_Walk_Right_1", "Hero_Walk_Right_2", "Hero_Walk_Right_3"]
    }
  ]
}
```

Then load and use it:

```csharp
var config = AtlasConfigManager.LoadConfig("hero-config.json");
var xml = AtlasGenerator.GenerateAtlas(config);
AtlasGenerator.SaveAtlas(config, xml);
```

## Available Presets

```csharp
AtlasPresets.RPGCharacter32x32    // 32x32 character spritesheets
AtlasPresets.RPGCharacter48x48    // 48x48 character spritesheets  
AtlasPresets.WeaponIcons64x64     // 64x64 weapon icons
AtlasPresets.ItemIcons32x32       // 32x32 item icons
AtlasPresets.TileSet16x16         // 16x16 tilesets
AtlasPresets.UIElements           // Variable-size UI elements
```

## Naming Patterns

Use these placeholders in `namingPattern`:

- `{row}` - Row number (0-based)
- `{col}` - Column number (0-based)  
- `{index}` - Sequential index (0-based)
- `{name}` - Defaults to "Sprite"

Examples:
- `"Sprite_{row}_{col}"` → `Sprite_0_0`, `Sprite_0_1`, etc.
- `"Char_{state}_{direction}_{frame}"` → Custom naming for characters
- `"Item_{category}_{index}"` → Custom naming for items

## Animation Configuration

### Pattern-based (Recommended)
```json
{
  "name": "Walk_Down",
  "delayMs": 80,
  "pattern": "Char_Walk_Down_.*"  // Regex pattern
}
```

### Explicit frame names
```json
{
  "name": "Attack",
  "delayMs": 120,
  "frameNames": ["Char_Attack_1", "Char_Attack_2", "Char_Attack_3"]
}
```

### Frame count method
```json
{
  "name": "Idle",
  "delayMs": 150,
  "startFrame": 0,
  "frameCount": 6
}
```

## Integration with Your Game

The generated XML files work seamlessly with your existing `TextureAtlas` system:

```csharp
// Load the generated atlas (same as before)
var atlas = TextureAtlas.FromFile(Content, "images/xml/hero-atlas.xml");

// Use sprites (same as before)
var heroSprite = atlas.CreateSprite("Hero_Idle_Down_1");

// Use animations (same as before)  
var walkAnimation = atlas.GetAnimation("Walk_Down");
var animatedHero = atlas.CreateAnimatedSprite("Walk_Down");
```

## Automating Atlas Generation

### During Development

Add this to your game's startup or development tools:

```csharp
#if DEBUG
    // Auto-generate atlases during development
    RegenerateAtlases();
#endif

private void RegenerateAtlases()
{
    var configs = new[]
    {
        ("Content/images/sprites/Character.png", "Content/images/xml/character-atlas.xml", "images/sprites/Character"),
        ("Content/images/sprites/Weapons.png", "Content/images/xml/weapon-atlas.xml", "images/sprites/Weapons"),
        // Add more as needed
    };

    foreach (var (spritesheet, output, texturePath) in configs)
    {
        if (File.Exists(spritesheet))
        {
            var config = AtlasPresets.RPGCharacter32x32; // Or appropriate preset
            config.SpritesheetPath = spritesheet;
            config.OutputPath = output;
            config.TexturePath = texturePath;

            var xml = AtlasGenerator.GenerateAtlas(config);
            AtlasGenerator.SaveAtlas(config, xml);
            
            Console.WriteLine($"Generated: {output}");
        }
    }
}
```

### Batch Processing

```csharp
// Process multiple configuration files
var configDirectory = "Content/atlas-configs/";
var configs = AtlasConfigManager.LoadBatchConfigs(configDirectory);

foreach (var kvp in configs)
{
    try
    {
        var xml = AtlasGenerator.GenerateAtlas(kvp.Value);
        AtlasGenerator.SaveAtlas(kvp.Value, xml);
        Console.WriteLine($"✓ Generated: {kvp.Key}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"✗ Failed {kvp.Key}: {ex.Message}");
    }
}
```

## Benefits

🎯 **Saves Hours of Manual Work** - No more hand-editing XML files  
🔄 **Automatic Updates** - Regenerate when spritesheets change  
🎮 **Game-Ready** - Works with your existing TextureAtlas system  
⚙️ **Configurable** - Flexible naming patterns and animation setups  
🎨 **Smart Detection** - Automatically detects sprite boundaries  
📁 **Batch Processing** - Handle multiple spritesheets at once  

## Troubleshooting

**Sprites not detected correctly?**
- Verify your spritesheet uses a consistent grid
- Adjust `gridWidth` and `gridHeight` manually if auto-detection fails
- Check that sprites have proper transparency boundaries

**Wrong sprite names?**
- Modify the `namingPattern` in your configuration
- Use `{row}`, `{col}`, `{index}` placeholders appropriately

**Animations not working?**
- Ensure animation patterns match your sprite names
- Use regex patterns like `"Char_Walk_.*"` for flexible matching
- Check that frame names exist in the generated regions

**Build errors?**
- Make sure you have the `System.Drawing.Common` package installed
- Verify the Hearthvale project builds successfully first

The Atlas Generator is now a powerful time-saving tool that automates the tedious process of creating spritesheet atlas XML files!