# Atlas Generator Tool

A powerful automation tool for generating MonoGame texture atlas XML files from spritesheets. This tool eliminates the tedious manual process of creating atlas definitions by automatically detecting sprites and generating properly formatted XML files.

## Features

- **Automatic Sprite Detection**: Detects individual sprites in grid-based spritesheets
- **Smart Grid Detection**: Automatically determines sprite dimensions when not specified
- **Animation Support**: Generates animation sequences from sprite patterns
- **Configuration-Based**: Use JSON configuration files for repeatable atlas generation
- **Preset Support**: Built-in presets for common spritesheet types (RPG characters, weapon icons, etc.)
- **MSBuild Integration**: Automatic atlas generation during build process
- **Batch Processing**: Process multiple spritesheets at once
- **Transparency Trimming**: Automatically trim transparent pixels from sprite regions

## Quick Start

### Basic Usage

Generate a simple atlas from a spritesheet:

```bash
AtlasGenerator generate hero.png hero-atlas.xml images/sprites/hero --grid 32x32
```

### Using Presets

Use a preset for common spritesheet types:

```bash
AtlasGenerator generate character.png character-atlas.xml images/sprites/character --preset rpg-character-32
```

### Using Configuration Files

Create a configuration file for more complex setups:

```bash
AtlasGenerator config create character-config.json character
```

Edit the configuration file, then generate:

```bash
AtlasGenerator generate character.png character-atlas.xml images/sprites/character --config character-config.json
```

## Configuration File Format

Configuration files use JSON format with the following structure:

```json
{
  "spritesheetPath": "path/to/spritesheet.png",
  "outputPath": "path/to/output-atlas.xml", 
  "texturePath": "content/path/to/texture",
  "gridWidth": 32,
  "gridHeight": 32,
  "namingPattern": "Char_{animation}_{direction}_{frame}",
  "isGridBased": true,
  "trimTransparency": true,
  "transparencyThreshold": 0,
  "animations": [
    {
      "name": "Idle_Down",
      "delayMs": 100,
      "pattern": "Char_Idle_Down_.*"
    }
  ]
}
```

### Configuration Properties

- **spritesheetPath**: Path to the source spritesheet image
- **outputPath**: Where to save the generated XML atlas
- **texturePath**: Content path used in the XML (for MonoGame Content Pipeline)
- **gridWidth/gridHeight**: Size of each sprite cell (0 = auto-detect)
- **namingPattern**: Pattern for naming sprites (supports {row}, {col}, {index} placeholders)
- **isGridBased**: Whether sprites are arranged in a grid (true) or packed (false)
- **trimTransparency**: Remove transparent pixels from sprite bounds
- **transparencyThreshold**: Alpha threshold for transparency detection (0-255)

### Animation Configuration

Animations can be defined in three ways:

1. **Pattern-based**: Use regex patterns to match sprite names
```json
{
  "name": "Walk_Right",
  "delayMs": 80,
  "pattern": "Char_Walk_Right_.*"
}
```

2. **Explicit frame names**: List specific sprite names
```json
{
  "name": "Attack",
  "delayMs": 120,
  "frameNames": ["Char_Attack_1", "Char_Attack_2", "Char_Attack_3"]
}
```

3. **Frame count and start**: Specify start frame and count
```json
{
  "name": "Idle",
  "delayMs": 150,
  "startFrame": 0,
  "frameCount": 6
}
```

## Available Presets

The tool includes several built-in presets for common spritesheet formats:

- **rpg-character-32**: 32x32 RPG character spritesheets
- **rpg-character-48**: 48x48 RPG character spritesheets  
- **weapon-icons-64**: 64x64 weapon icon spritesheets
- **item-icons-32**: 32x32 item icon spritesheets
- **tileset-16**: 16x16 tileset spritesheets
- **ui-elements**: Variable-size UI element spritesheets

## MSBuild Integration

To automatically generate atlases during build:

1. Add the targets file to your project:
```xml
<Import Project="$(SolutionDir)build\AtlasGeneration.targets" />
```

2. Create configuration files in `Content\atlas-configs\`

3. Build your project - atlases will be generated automatically

### MSBuild Properties

- **GenerateAtlasesOnBuild**: Enable/disable automatic generation (default: true)
- **AtlasConfigDir**: Directory containing configuration files
- **AtlasGeneratorExe**: Path to the AtlasGenerator executable

### MSBuild Targets

- **GenerateAtlases**: Generate all atlases from configuration files
- **GenerateSingleAtlas**: Generate one atlas from command-line properties
- **CreateAtlasConfigs**: Create sample configuration files
- **CleanAtlases**: Remove generated atlas files

## Command Reference

### Generate Command

```bash
AtlasGenerator generate <spritesheet> <output> <texture-path> [options]
```

**Options:**
- `--config <file>`: Use configuration file
- `--grid <width>x<height>`: Set grid size (e.g., 32x32)
- `--pattern <pattern>`: Set naming pattern
- `--preset <preset>`: Use preset configuration
- `--no-trim`: Disable transparency trimming
- `--threshold <value>`: Set transparency threshold (0-255)

### Config Command

```bash
AtlasGenerator config <sub-command>
```

**Sub-commands:**
- `create <file> <type>`: Create sample configuration (types: sample, character, weapon)
- `validate <file>`: Validate configuration file
- `list-presets`: List available presets

### Batch Command

```bash
AtlasGenerator batch <config-directory>
```

Processes all `.json` configuration files in the specified directory.

## Examples

### Character Spritesheet

For a character spritesheet with 32x32 sprites arranged in a grid:

```bash
# Using preset
AtlasGenerator generate character.png character-atlas.xml images/sprites/character --preset rpg-character-32

# Manual configuration
AtlasGenerator generate character.png character-atlas.xml images/sprites/character --grid 32x32 --pattern "Char_{row}_{col}"
```

### Weapon Icons

For weapon icons in a 64x64 grid:

```bash
AtlasGenerator generate weapons.png weapon-atlas.xml images/icons/weapons --preset weapon-icons-64
```

### Custom Configuration

1. Create configuration:
```bash
AtlasGenerator config create my-atlas.json sample
```

2. Edit `my-atlas.json` with your settings

3. Generate atlas:
```bash
AtlasGenerator generate my-spritesheet.png my-atlas.xml images/my-texture --config my-atlas.json
```

## Troubleshooting

### Common Issues

**Grid detection fails**: Specify grid size manually using `--grid`

**Wrong sprite names**: Adjust the `namingPattern` in configuration

**Missing animations**: Check animation patterns match your sprite names

**Transparency issues**: Adjust `transparencyThreshold` value

### Debugging

Use `AtlasGenerator config validate <file>` to check configuration syntax.

Enable verbose output by examining the generated XML to verify sprite regions.

## Integration with Existing Atlas System

The generated XML files are fully compatible with your existing `TextureAtlas.FromFile()` system:

```csharp
// Load generated atlas
var atlas = TextureAtlas.FromFile(Content, "images/xml/character-atlas.xml");

// Use as normal
var sprite = atlas.CreateSprite("Char_Idle_Down_1");
var animation = atlas.GetAnimation("Idle_Down");
```

## Performance Notes

- Grid-based detection is fast and reliable
- Large spritesheets may take longer to process
- Transparency trimming adds processing time but reduces memory usage
- Configuration files enable quick regeneration without parameter re-entry

## Future Enhancements

- Packed spritesheet support (non-grid layouts)
- Visual sprite boundary preview
- Automatic animation detection from sprite naming
- Integration with texture packing tools
- Support for multi-texture atlases