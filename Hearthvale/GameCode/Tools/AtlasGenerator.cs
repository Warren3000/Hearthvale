using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using DrawingRectangle = System.Drawing.Rectangle;
using XnaRectangle = Microsoft.Xna.Framework.Rectangle;

namespace Hearthvale.GameCode.Tools
{
    /// <summary>
    /// Tool for automatically generating texture atlas XML definitions from spritesheet images
    /// </summary>
    public class AtlasGenerator
    {
        /// <summary>
        /// Configuration for atlas generation
        /// </summary>
        public class AtlasConfig
        {
            public string SpritesheetPath { get; set; }
            public string OutputPath { get; set; }
            public string TexturePath { get; set; }
            public int GridWidth { get; set; } = 0; // 0 = auto-detect
            public int GridHeight { get; set; } = 0; // 0 = auto-detect
            public string NamingPattern { get; set; } = "{name}_{index}";
            public List<AnimationConfig> Animations { get; set; } = new List<AnimationConfig>();
            public bool TrimTransparency { get; set; } = true;
            public int TransparencyThreshold { get; set; } = 0;
            public bool IsGridBased { get; set; } = true;
            public int MarginLeft { get; set; } = 0;
            public int MarginTop { get; set; } = 0;
        }

        /// <summary>
        /// Configuration for animations within the atlas
        /// </summary>
        public class AnimationConfig
        {
            public string Name { get; set; }
            public List<string> FrameNames { get; set; } = new List<string>();
            public int DelayMs { get; set; } = 100;
            public int StartFrame { get; set; } = 0;
            public int FrameCount { get; set; } = 1;
            public string Pattern { get; set; } // For pattern-based frame selection
        }

        /// <summary>
        /// Represents a detected sprite region
        /// </summary>
        public class SpriteRegion
        {
            public string Name { get; set; }
            public DrawingRectangle Bounds { get; set; }
            public DrawingRectangle ContentBounds { get; set; }
        }

        /// <summary>
        /// Detects sprite regions from the provided configuration without generating the full XML.
        /// </summary>
        /// <param name="config">Atlas generation configuration</param>
        /// <returns>A list of detected sprite regions</returns>
        public static List<SpriteRegion> DetectRegions(AtlasConfig config)
        {
            if (!File.Exists(config.SpritesheetPath))
                throw new FileNotFoundException($"Spritesheet not found: {config.SpritesheetPath}");

            using (var bitmap = new Bitmap(config.SpritesheetPath))
            {
                // If animations are defined, generate regions based on animation frame names
                if (config.Animations != null && config.Animations.Any())
                {
                    return GenerateRegionsFromAnimations(bitmap, config);
                }

                return config.IsGridBased
                    ? DetectGridBasedSprites(bitmap, config)
                    : DetectPackedSprites(bitmap, config);
            }
        }

        /// <summary>
        /// Generates regions based on animation frame names and grid positions
        /// </summary>
        private static List<SpriteRegion> GenerateRegionsFromAnimations(Bitmap bitmap, AtlasConfig config)
        {
            var regions = new List<SpriteRegion>();

            var marginLeft = Math.Max(0, config.MarginLeft);
            var marginTop = Math.Max(0, config.MarginTop);

            // Auto-detect grid size if not specified
            int gridWidth = config.GridWidth;
            int gridHeight = config.GridHeight;

            if (gridWidth == 0 || gridHeight == 0)
            {
                var autoDetected = AutoDetectGridSize(bitmap, config.TransparencyThreshold);
                gridWidth = autoDetected.Width;
                gridHeight = autoDetected.Height;
            }

            if (gridWidth == 0 || gridHeight == 0)
                throw new InvalidOperationException("Could not auto-detect grid size. Please specify GridWidth and GridHeight manually.");

            int usableWidth = Math.Max(0, bitmap.Width - marginLeft);
            int usableHeight = Math.Max(0, bitmap.Height - marginTop);
            int cols = Math.Max(1, (usableWidth + gridWidth - 1) / gridWidth);
            int rows = Math.Max(1, (usableHeight + gridHeight - 1) / gridHeight);
            
            // Collect all unique frame names from all animations
            var allFrameNames = new HashSet<string>();
            foreach (var animation in config.Animations)
            {
                if (animation.FrameNames != null)
                {
                    foreach (var frameName in animation.FrameNames)
                    {
                        allFrameNames.Add(frameName);
                    }
                }
            }

            // Generate regions for each frame name
            int frameIndex = 0;
            foreach (var frameName in allFrameNames.OrderBy(name => name))
            {
                int row = frameIndex / cols;
                int col = frameIndex % cols;

                if (row >= rows)
                {
                    break;
                }
                
                int x = marginLeft + col * gridWidth;
                int y = marginTop + row * gridHeight;
                int width = Math.Min(gridWidth, bitmap.Width - x);
                int height = Math.Min(gridHeight, bitmap.Height - y);

                if (width <= 0 || height <= 0)
                {
                    frameIndex++;
                    continue;
                }

                var bounds = new DrawingRectangle(x, y, width, height);
                
                // Check if this region contains non-transparent pixels
                if (!IsRegionEmpty(bitmap, bounds, config.TransparencyThreshold))
                {
                    var contentBounds = config.TrimTransparency
                        ? GetContentBounds(bitmap, bounds, config.TransparencyThreshold)
                        : bounds;

                    var region = new SpriteRegion
                    {
                        Name = frameName, // Use the frame name directly as the region name
                        Bounds = bounds,
                        ContentBounds = contentBounds
                    };

                    regions.Add(region);
                }
                
                frameIndex++;
            }

            return regions;
        }

        /// <summary>
        /// Generates texture atlas XML from the provided configuration
        /// </summary>
        /// <param name="config">Atlas generation configuration</param>
        /// <returns>Generated XML as string</returns>
        public static string GenerateAtlas(AtlasConfig config)
        {
            var regions = DetectRegions(config);
            return GenerateXml(config, regions);
        }

        /// <summary>
        /// Detects sprites in a grid-based spritesheet
        /// </summary>
        private static List<SpriteRegion> DetectGridBasedSprites(Bitmap bitmap, AtlasConfig config)
        {
            var regions = new List<SpriteRegion>();

            var marginLeft = Math.Max(0, config.MarginLeft);
            var marginTop = Math.Max(0, config.MarginTop);

            // Auto-detect grid size if not specified
            int gridWidth = config.GridWidth;
            int gridHeight = config.GridHeight;

            if (gridWidth == 0 || gridHeight == 0)
            {
                var autoDetected = AutoDetectGridSize(bitmap, config.TransparencyThreshold);
                gridWidth = autoDetected.Width;
                gridHeight = autoDetected.Height;
            }

            if (gridWidth == 0 || gridHeight == 0)
                throw new InvalidOperationException("Could not auto-detect grid size. Please specify GridWidth and GridHeight manually.");

            int usableWidth = Math.Max(0, bitmap.Width - marginLeft);
            int usableHeight = Math.Max(0, bitmap.Height - marginTop);
            int cols = Math.Max(1, (usableWidth + gridWidth - 1) / gridWidth);
            int rows = Math.Max(1, (usableHeight + gridHeight - 1) / gridHeight);

            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    int x = marginLeft + col * gridWidth;
                    int y = marginTop + row * gridHeight;
                    int width = Math.Min(gridWidth, bitmap.Width - x);
                    int height = Math.Min(gridHeight, bitmap.Height - y);

                    if (width <= 0 || height <= 0)
                    {
                        continue;
                    }

                    var bounds = new DrawingRectangle(x, y, width, height);
                    
                    // Check if this region contains non-transparent pixels
                    if (!IsRegionEmpty(bitmap, bounds, config.TransparencyThreshold))
                    {
                        var contentBounds = config.TrimTransparency 
                            ? GetContentBounds(bitmap, bounds, config.TransparencyThreshold)
                            : bounds;

                        var region = new SpriteRegion
                        {
                            Name = GenerateRegionName(config.NamingPattern, row * cols + col, row, col),
                            Bounds = bounds,
                            ContentBounds = contentBounds
                        };

                        regions.Add(region);
                    }
                }
            }

            return regions;
        }

        /// <summary>
        /// Detects sprites in a packed spritesheet (not implemented yet)
        /// </summary>
        private static List<SpriteRegion> DetectPackedSprites(Bitmap bitmap, AtlasConfig config)
        {
            // TODO: Implement packed sprite detection using flood fill or similar algorithms
            throw new NotImplementedException("Packed sprite detection is not yet implemented. Use grid-based detection for now.");
        }

        /// <summary>
        /// Auto-detects the grid size of a spritesheet
        /// </summary>
        private static Size AutoDetectGridSize(Bitmap bitmap, int transparencyThreshold)
        {
            // Look for repeating patterns in the first row and column
            int width = DetectRepeatingPattern(bitmap, true, transparencyThreshold);
            int height = DetectRepeatingPattern(bitmap, false, transparencyThreshold);

            return new Size(width, height);
        }

        /// <summary>
        /// Detects repeating patterns to find sprite boundaries
        /// </summary>
        private static int DetectRepeatingPattern(Bitmap bitmap, bool horizontal, int transparencyThreshold)
        {
            var scanLine = horizontal ? bitmap.Width : bitmap.Height;
            var perpendicular = horizontal ? bitmap.Height : bitmap.Width;

            // Sample the middle line to avoid edge artifacts
            int sampleLine = perpendicular / 2;

            for (int size = 8; size <= scanLine / 2; size++)
            {
                // Check if this size creates a valid repeating pattern
                for (int pos = 0; pos < scanLine - size; pos += size)
                {
                    var pos1 = horizontal ? new System.Drawing.Point(pos, sampleLine) : new System.Drawing.Point(sampleLine, pos);
                    var pos2 = horizontal ? new System.Drawing.Point(pos + size, sampleLine) : new System.Drawing.Point(sampleLine, pos + size);
                    
                    if (pos2.X >= bitmap.Width || pos2.Y >= bitmap.Height)
                        break;

                    var pixel1 = bitmap.GetPixel(pos1.X, pos1.Y);
                    var pixel2 = bitmap.GetPixel(pos2.X, pos2.Y);

                    // Look for transparency patterns as sprite boundaries
                    bool isTransparent1 = pixel1.A <= transparencyThreshold;
                    bool isTransparent2 = pixel2.A <= transparencyThreshold;

                    if (isTransparent1 != isTransparent2)
                    {
                        // Found a pattern break
                        return size;
                    }
                }
            }

            return 0; // No pattern detected
        }

        /// <summary>
        /// Checks if a region contains only transparent pixels
        /// </summary>
        private static bool IsRegionEmpty(Bitmap bitmap, DrawingRectangle region, int transparencyThreshold)
        {
            for (int y = region.Top; y < region.Bottom && y < bitmap.Height; y++)
            {
                for (int x = region.Left; x < region.Right && x < bitmap.Width; x++)
                {
                    var pixel = bitmap.GetPixel(x, y);
                    if (pixel.A > transparencyThreshold)
                        return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Gets the actual content bounds of a sprite, trimming transparent pixels
        /// </summary>
        private static DrawingRectangle GetContentBounds(Bitmap bitmap, DrawingRectangle region, int transparencyThreshold)
        {
            int minX = region.Right;
            int minY = region.Bottom;
            int maxX = region.Left;
            int maxY = region.Top;

            bool hasContent = false;

            for (int y = region.Top; y < region.Bottom && y < bitmap.Height; y++)
            {
                for (int x = region.Left; x < region.Right && x < bitmap.Width; x++)
                {
                    var pixel = bitmap.GetPixel(x, y);
                    if (pixel.A > transparencyThreshold)
                    {
                        hasContent = true;
                        minX = Math.Min(minX, x);
                        minY = Math.Min(minY, y);
                        maxX = Math.Max(maxX, x);
                        maxY = Math.Max(maxY, y);
                    }
                }
            }

            if (!hasContent)
                return region;

            return new DrawingRectangle(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }

        /// <summary>
        /// Generates a region name based on the pattern
        /// </summary>
        private static string GenerateRegionName(string pattern, int index, int row, int col)
        {
            return pattern
                .Replace("{index}", index.ToString())
                .Replace("{row}", row.ToString())
                .Replace("{col}", col.ToString())
                .Replace("{name}", "Sprite");
        }

        /// <summary>
        /// Generates the XML atlas definition
        /// </summary>
        private static string GenerateXml(AtlasConfig config, List<SpriteRegion> regions)
        {
            var root = new XElement("TextureAtlas");

            // Add texture reference
            root.Add(new XElement("Texture", config.TexturePath));

            // Add regions
            var regionsElement = new XElement("Regions");
            foreach (var region in regions.OrderBy(r => r.Name))
            {
                var bounds = config.TrimTransparency ? region.ContentBounds : region.Bounds;
                regionsElement.Add(new XElement("Region",
                    new XAttribute("name", region.Name),
                    new XAttribute("x", bounds.X),
                    new XAttribute("y", bounds.Y),
                    new XAttribute("width", bounds.Width),
                    new XAttribute("height", bounds.Height)
                ));
            }
            root.Add(regionsElement);

            // Add animations if configured
            if (config.Animations.Any())
            {
                var animationsElement = new XElement("Animations");
                foreach (var animConfig in config.Animations)
                {
                    var animElement = new XElement("Animation",
                        new XAttribute("name", animConfig.Name),
                        new XAttribute("delay", animConfig.DelayMs));

                    var frameNames = ResolveAnimationFrames(animConfig, regions);
                    foreach (var frameName in frameNames)
                    {
                        animElement.Add(new XElement("Frame",
                            new XAttribute("region", frameName)));
                    }

                    animationsElement.Add(animElement);
                }
                root.Add(animationsElement);
            }

            var doc = new XDocument(
                new XDeclaration("1.0", "utf-8", null),
                root);

            var sb = new StringBuilder();
            using (var writer = new StringWriter(sb))
            {
                doc.Save(writer);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Resolves animation frame names based on configuration
        /// </summary>
        private static List<string> ResolveAnimationFrames(AnimationConfig animConfig, List<SpriteRegion> regions)
        {
            var frameNames = new List<string>();

            if (animConfig.FrameNames.Any())
            {
                // Use explicitly specified frame names
                frameNames.AddRange(animConfig.FrameNames);
            }
            else if (!string.IsNullOrEmpty(animConfig.Pattern))
            {
                // Use pattern matching
                var matchingRegions = regions.Where(r => 
                    r.Name.Contains(animConfig.Pattern) || 
                    System.Text.RegularExpressions.Regex.IsMatch(r.Name, animConfig.Pattern))
                    .OrderBy(r => r.Name);
                frameNames.AddRange(matchingRegions.Select(r => r.Name));
            }
            else
            {
                // Use frame count and start frame
                var orderedRegions = regions.OrderBy(r => r.Name).ToList();
                for (int i = animConfig.StartFrame; i < Math.Min(animConfig.StartFrame + animConfig.FrameCount, orderedRegions.Count); i++)
                {
                    frameNames.Add(orderedRegions[i].Name);
                }
            }

            return frameNames;
        }

        /// <summary>
        /// Saves the generated atlas XML to a file
        /// </summary>
        /// <param name="config">Atlas configuration</param>
        /// <param name="xmlContent">Generated XML content</param>
        public static void SaveAtlas(AtlasConfig config, string xmlContent)
        {
            var directory = Path.GetDirectoryName(config.OutputPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(config.OutputPath, xmlContent);
        }
    }
}