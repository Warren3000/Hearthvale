using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Collections.Generic;
using Hearthvale.GameCode.Utils;

namespace Hearthvale.GameCode.Systems;

/// <summary>Configures logging once at startup.</summary>
public sealed class LoggingSystem : IGameSystem
{


    private class LogConfig
    {
        public List<string> EnabledAreas { get; set; }
        public string MinLevel { get; set; }
    }

    // Preset groups for easy toggling
    private static readonly Dictionary<string, LogArea> PresetGroups = new()
    {
        { "All", LogArea.All },
        { "None", LogArea.None },
        { "DebugGameplay", LogArea.Scene | LogArea.Player | LogArea.NPC | LogArea.Dungeon | LogArea.Weapon },
        { "DebugGraphics", LogArea.Camera | LogArea.Atlas | LogArea.Tilemap | LogArea.UI },
        // Add more custom groups as needed
    };

    public void Initialize()
    {
        try
        {
            var configPath = Path.Combine("Content", "logconfig.json");
            if (File.Exists(configPath))
            {
                var json = File.ReadAllText(configPath);
                var config = JsonSerializer.Deserialize<LogConfig>(json);
                if (config != null)
                {
                    LogArea enabled = LogArea.None;
                    if (config.EnabledAreas != null)
                    {
                        foreach (var area in config.EnabledAreas)
                        {
                            if (PresetGroups.TryGetValue(area, out var preset))
                            {
                                enabled |= preset;
                            }
                            else if (Enum.TryParse<LogArea>(area, out var parsed))
                            {
                                enabled |= parsed;
                            }
                        }
                    }
                    Log.EnabledAreas = enabled;
                    if (Enum.TryParse<LogLevel>(config.MinLevel, out var minLevel))
                        Log.MinLevel = minLevel;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Info(LogArea.General, $"Failed to load log config: {ex.Message}");
        }
        Log.Info(LogArea.General, "Logging system initialized.");
    }

    public void Update(Microsoft.Xna.Framework.GameTime gameTime) { }
}