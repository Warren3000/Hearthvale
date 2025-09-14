# Atlas Generator UI User Guide

## Overview

The Atlas Generator UI is a Windows Forms application that provides a graphical interface for creating texture atlas XML files from spritesheet images. This tool automates the tedious process of manually defining sprite regions and animations.

## Features

- **Visual spritesheet preview** with grid overlay and region detection
- **Grid-based sprite detection** with auto-detection capabilities
- **Animation configuration** with frame sequencing and timing
- **Batch processing** for multiple spritesheets
- **Configuration presets** for common spritesheet formats
- **Real-time XML preview** of generated atlas files
- **Save/Load configurations** for reusable setups

## Getting Started

### Running the Application

1. Build the project: `dotnet build` from the `AtlasGeneratorUI` directory
2. Run the executable: `bin\Debug\net8.0-windows\AtlasGeneratorUI.exe`

### Basic Workflow

1. **Load a Spritesheet**: Click "Browse" next to "Spritesheet" to select your image file
2. **Configure Grid Settings**: Set grid width/height or use auto-detection (0x0)
3. **Set Output Path**: Specify where to save the generated XML file
4. **Preview Results**: Click "Preview" to see detected sprite regions
5. **Generate Atlas**: Click "Generate Atlas" to create the XML file

## Interface Overview

### Main Window Layout

The main window is divided into four panels:

#### 1. Configuration Panel (Top Left)
- **Preset**: Quick selection of common configurations
- **Spritesheet**: Path to the source image file
- **Output XML**: Where to save the generated atlas file
- **Texture Path**: Relative path for the texture reference in XML
- **Grid Size**: Width and height of each sprite (0 = auto-detect)
- **Naming Pattern**: How to name detected sprites (supports {row}, {col}, {index})
- **Transparency**: Options for trimming transparent pixels

#### 2. Animation Panel (Bottom Left)
- **Animation List**: Shows configured animations
- **Add/Edit/Remove**: Buttons to manage animations
- Each animation can specify:
  - Frame names or patterns
  - Frame delay timing
  - Start frame and frame count

#### 3. Preview Panel (Top Right)
- **Visual Preview**: Shows the loaded spritesheet
- **Show Grid**: Toggle grid overlay display
- **Show Regions**: Toggle detected region highlights
- **Zoom Control**: Adjust preview zoom level

#### 4. Output Panel (Bottom Right)
- **XML Preview**: Real-time view of generated XML
- **Generate Atlas**: Creates the final XML file
- **Configuration Controls**: Save/Load settings
- **Progress/Status**: Shows generation progress and messages

## Configuration Presets

The application includes several built-in presets for common spritesheet formats:

### RPG Character 32x32
- Grid Size: 32x32 pixels
- Naming: Character_row_col
- Transparency trimming enabled

### RPG Character 48x48
- Grid Size: 48x48 pixels
- Naming: Character_row_col
- Transparency trimming enabled

### Weapon Icons 64x64
- Grid Size: 64x64 pixels
- Naming: Weapon_row_col
- Transparency trimming enabled

### Item Icons 32x32
- Grid Size: 32x32 pixels
- Naming: Item_row_col
- Transparency trimming enabled

### Tileset 16x16
- Grid Size: 16x16 pixels
- Naming: Tile_row_col
- Transparency trimming disabled

### UI Elements
- Grid Size: Auto-detect
- Naming: UI_row_col
- Higher transparency threshold

## Animation Configuration

### Animation Dialog

Click "Add" in the Animation panel to open the Animation Configuration dialog:

#### Fields:
- **Name**: Unique identifier for the animation
- **Frame Delay (ms)**: Time between frames in milliseconds
- **Pattern (regex)**: Regular expression to match sprite names
- **Start Frame**: Starting frame index (0-based)
- **Frame Count**: Number of frames in the animation
- **Frame Names**: Explicit list of frame names to include

#### Animation Methods:

1. **Explicit Frame Names**: Manually add frame names to the list
2. **Pattern Matching**: Use regex patterns to automatically select frames
3. **Index Range**: Specify start frame and count for sequential frames

### Example Animations:

```
Walk Animation:
- Name: "walk"
- Pattern: "Character_0_.*"  (matches all sprites in row 0)
- Frame Delay: 150ms

Attack Animation:
- Name: "attack"
- Frame Names: ["Character_1_0", "Character_1_1", "Character_1_2"]
- Frame Delay: 100ms
```

## Batch Processing

### Batch Process Dialog

Access via Tools → Batch Process to process multiple spritesheets:

#### Setup:
1. **Source Directory**: Folder containing spritesheet images
2. **Output Directory**: Where to save generated XML files
3. **Scan for Images**: Finds all image files in source directory
4. **Preset Selection**: Choose configuration preset for all files
5. **Overwrite Option**: Replace existing XML files

#### Process:
1. Select source and output directories
2. Click "Scan for Images" to find spritesheets
3. Choose a preset configuration
4. Click "Process All" to generate atlases for all images
5. Monitor progress in the log panel

## Advanced Features

### Naming Patterns

The naming pattern supports several placeholders:

- `{row}`: Row index (0-based)
- `{col}`: Column index (0-based)
- `{index}`: Sequential index (row * cols + col)
- `{name}`: Generic sprite name

Examples:
- `Sprite_{row}_{col}` → "Sprite_0_0", "Sprite_0_1", etc.
- `Character_{index}` → "Character_0", "Character_1", etc.
- `Tile_{row}x{col}` → "Tile_0x0", "Tile_0x1", etc.

### Auto-Detection

When grid size is set to 0x0, the application attempts to automatically detect sprite boundaries by:

1. Analyzing transparency patterns
2. Looking for repeating structures
3. Finding consistent spacing between sprites

This works best with:
- Consistent grid layouts
- Clear sprite boundaries
- Minimal padding between sprites

### Transparency Handling

- **Trim Transparency**: Removes transparent edges from sprite regions
- **Transparency Threshold**: Alpha value below which pixels are considered transparent (0-255)
- **Content Bounds**: Actual sprite content after trimming

## File Formats

### Supported Input Formats
- PNG (recommended)
- JPG/JPEG
- BMP
- GIF

### Generated XML Format

The application generates XML files compatible with the MonoGame TextureAtlas system:

```xml
<?xml version="1.0" encoding="utf-8"?>
<TextureAtlas>
  <Texture>spritesheet.png</Texture>
  <Regions>
    <Region name="Sprite_0_0" x="0" y="0" width="32" height="32" />
    <Region name="Sprite_0_1" x="32" y="0" width="32" height="32" />
  </Regions>
  <Animations>
    <Animation name="walk" delay="150">
      <Frame region="Sprite_0_0" />
      <Frame region="Sprite_0_1" />
    </Animation>
  </Animations>
</TextureAtlas>
```

## Tips and Best Practices

### For Best Results:
1. **Use consistent grid layouts** for auto-detection
2. **Save configurations** for reusable setups
3. **Preview before generating** to verify sprite detection
4. **Use descriptive naming patterns** for easier identification
5. **Test with small files first** before batch processing

### Troubleshooting:
- **Grid not detected**: Manually set grid size or adjust transparency threshold
- **Wrong regions**: Check transparency settings and grid alignment
- **Animation issues**: Verify frame names match detected sprites
- **Performance**: Use lower zoom levels for large spritesheets

### Workflow Integration:
1. Create and test configuration with a sample spritesheet
2. Save the configuration for future use
3. Use batch processing for similar spritesheets
4. Integrate generated XML files into your MonoGame project

## Integration with MonoGame

The generated XML files work with the existing MonoGame TextureAtlas system:

```csharp
// Load the atlas
var atlas = TextureAtlas.FromFile(GraphicsDevice, Content, "path/to/atlas.xml");

// Get a sprite region
var sprite = atlas.GetRegion("Sprite_0_0");

// Play an animation
var walkAnimation = atlas.GetAnimation("walk");
```

This UI tool seamlessly integrates with your existing MonoGame development workflow, making sprite management much more efficient.