# Atlas Generator UI

A Windows Forms application for automatically generating MonoGame texture atlas XML files from spritesheet images.

## Quick Start

```bash
# Build the application
cd AtlasGeneratorUI
dotnet build

# Run the application
bin\Debug\net8.0-windows\AtlasGeneratorUI.exe
```

## Features

- 🖼️ **Visual Preview** - See your spritesheet with grid overlay and region detection
- 🎯 **Auto-Detection** - Automatically detect sprite boundaries from grid patterns
- 🎬 **Animation Support** - Configure frame sequences and timing
- ⚡ **Batch Processing** - Process multiple spritesheets at once
- 📐 **Presets** - Built-in configurations for common formats
- 💾 **Save/Load** - Reusable configuration files

## Project Structure

```
AtlasGeneratorUI/
├── Program.cs              # Application entry point
├── MainForm.cs             # Main UI form with layout
├── MainForm.Events.cs      # Event handlers and logic
├── AnimationDialog.cs      # Animation configuration dialog
├── BatchProcessDialog.cs   # Batch processing dialog
├── UI_USER_GUIDE.md       # Comprehensive user documentation
└── AtlasGeneratorUI.csproj # Project configuration
```

## Dependencies

- .NET 8.0 Windows
- System.Drawing.Common 8.0.0
- System.Text.Json 9.0.0
- Reference to main Hearthvale project (for AtlasGenerator)

## Usage Examples

### Basic Atlas Generation
1. Load a spritesheet image
2. Set grid size (or use auto-detection with 0x0)
3. Choose output path
4. Click "Generate Atlas"

### Animation Configuration
1. Click "Add" in the Animation panel
2. Configure frame names, timing, and patterns
3. Preview in the XML output

### Batch Processing
1. Go to Tools → Batch Process
2. Select source directory with spritesheets
3. Choose output directory
4. Select a preset and click "Process All"

## Integration

Generated XML files are compatible with MonoGame's TextureAtlas system:

```csharp
var atlas = TextureAtlas.FromFile(GraphicsDevice, Content, "atlas.xml");
var sprite = atlas.GetRegion("Sprite_0_0");
```

## Development

The UI application builds on the core AtlasGenerator functionality and provides:
- Rich graphical interface for configuration
- Real-time preview of sprite detection
- Visual feedback for atlas generation
- Workflow tools for game development

See `UI_USER_GUIDE.md` for detailed usage instructions and features.