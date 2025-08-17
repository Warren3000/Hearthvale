using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Graphics;
using System;
using System.Collections.Generic;

namespace Hearthvale.GameCode.Utils
{
    /// <summary>
    /// Utility for analyzing sprite textures to find actual content bounds
    /// </summary>
    public static class SpriteAnalyzer
    {
        // Cache analyzed bounds to avoid expensive calculations
        private static readonly Dictionary<string, Rectangle> _contentBoundsCache = new();
        // Cache texture data to avoid repeated GetData calls
        private static readonly Dictionary<Texture2D, Color[]> _textureDataCache = new();

        /// <summary>
        /// Gets content bounds of a texture region by analyzing non-transparent pixels
        /// </summary>
        public static Rectangle GetContentBounds(Texture2D texture, Rectangle sourceRect)
        {
            if (texture == null)
                return Rectangle.Empty;

            // Create a cache key based on texture hash and source rectangle
            string cacheKey = $"{texture.GetHashCode()}_{sourceRect}";
            
            // Return cached result if available
            if (_contentBoundsCache.TryGetValue(cacheKey, out Rectangle cachedBounds))
                return cachedBounds;
            
            // Get texture data (from cache if possible)
            Color[] textureData;
            if (!_textureDataCache.TryGetValue(texture, out textureData))
            {
                textureData = new Color[texture.Width * texture.Height];
                texture.GetData(textureData);
                _textureDataCache[texture] = textureData;
            }
            
            // Find the bounds of non-transparent pixels
            int left = sourceRect.Width;
            int top = sourceRect.Height;
            int right = 0;
            int bottom = 0;
            bool foundPixels = false;
            
            for (int y = sourceRect.Top; y < sourceRect.Bottom; y++)
            {
                for (int x = sourceRect.Left; x < sourceRect.Right; x++)
                {
                    int index = y * texture.Width + x;
                    
                    // Check if pixel is within bounds and has opacity
                    if (index >= 0 && index < textureData.Length && textureData[index].A > 25)
                    {
                        foundPixels = true;
                        left = Math.Min(left, x - sourceRect.Left);
                        top = Math.Min(top, y - sourceRect.Top);
                        right = Math.Max(right, x - sourceRect.Left);
                        bottom = Math.Max(bottom, y - sourceRect.Top);
                    }
                }
            }
            
            // Calculate bounds based on found non-transparent pixels
            Rectangle bounds;
            if (!foundPixels)
            {
                // If no non-transparent pixels found, use full source rectangle
                bounds = new Rectangle(0, 0, sourceRect.Width, sourceRect.Height);
            }
            else
            {
                bounds = new Rectangle(left, top, right - left + 1, bottom - top + 1);
            }
            
            // Cache the result
            _contentBoundsCache[cacheKey] = bounds;
            return bounds;
        }
        
        /// <summary>
        /// Clears all cached data
        /// </summary>
        public static void ClearCache()
        {
            _contentBoundsCache.Clear();
            _textureDataCache.Clear();
        }
    }
}