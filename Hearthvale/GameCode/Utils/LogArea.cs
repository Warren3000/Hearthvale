using System;
using System.Diagnostics;

namespace Hearthvale.GameCode.Utils
{
    [Flags]
    public enum LogArea
    {
        None = 0,
        Scene = 1 << 0,
        Camera = 1 << 1,
        Tilemap = 1 << 2,
        Dungeon = 1 << 3,
        Atlas = 1 << 4,
        Weapon = 1 << 5,
        Projectile = 1 << 6,
        NPC = 1 << 7,
        Player = 1 << 8,
        UI = 1 << 9,
        Probe = 1 << 10,
        Collision = 1 << 11,  // Added missing Collision log area
        General = 1 << 30,
        All = ~0
    }

    public enum LogLevel
    {
        Error = 0,
        Warn = 1,
        Info = 2,
        Verbose = 3
    }

    public static class Log
    {
        // Configure at runtime anywhere (e.g., LoadContent)
        public static LogArea EnabledAreas { get; set; } =
#if DEBUG
            LogArea.Scene | LogArea.Camera | LogArea.Dungeon | LogArea.Atlas | LogArea.Weapon | LogArea.Player | LogArea.NPC | LogArea.Collision;
#else
            LogArea.None;
#endif

        public static LogLevel MinLevel { get; set; } =
#if DEBUG
            LogLevel.Info;
#else
            LogLevel.Warn;
#endif

        [Conditional("DEBUG")]
        public static void Error(LogArea area, string message) => Write(area, LogLevel.Error, message);
        [Conditional("DEBUG")]
        public static void Warning(LogArea area, string message) => Write(area, LogLevel.Warn, message);
        [Conditional("DEBUG")]
        public static void Warn(LogArea area, string message) => Write(area, LogLevel.Warn, message);
        [Conditional("DEBUG")]
        public static void Info(LogArea area, string message) => Write(area, LogLevel.Info, message);
        [Conditional("DEBUG")]
        public static void Verbose(LogArea area, string message) => Write(area, LogLevel.Verbose, message);

        [Conditional("DEBUG")]
        private static void Write(LogArea area, LogLevel level, string message)
        {
            if ((EnabledAreas & area) == 0) return;
            if (level > MinLevel) return;
            System.Diagnostics.Debug.WriteLine($"[{area}] {message}");
        }

        // Optional: quick throttle for high-frequency logs
        private static DateTime _lastVerbose = DateTime.MinValue;
        [Conditional("DEBUG")]
        public static void VerboseThrottled(LogArea area, string message, TimeSpan interval)
        {
            if ((EnabledAreas & area) == 0) return;
            if (LogLevel.Verbose > MinLevel) return;
            var now = DateTime.UtcNow;
            if (now - _lastVerbose >= interval)
            {
                _lastVerbose = now;
                System.Diagnostics.Debug.WriteLine($"[{area}] {message}");
            }
        }
    }
}