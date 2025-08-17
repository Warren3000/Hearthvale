using MonoGameLibrary.Graphics;
using System.Collections.Generic;
using System.Reflection;

namespace Hearthvale.GameCode.Utils
{
    /// <summary>
    /// Extension methods for TextureAtlas
    /// </summary>
    public static class TextureAtlasExtensions
    {
        /// <summary>
        /// Gets all animation names from a TextureAtlas
        /// </summary>
        /// <param name="atlas">The texture atlas to get animation names from</param>
        /// <returns>Collection of animation names</returns>
        public static IEnumerable<string> GetAnimationNames(this TextureAtlas atlas)
        {
            // Use reflection to access the private _animations dictionary
            var animationsField = typeof(TextureAtlas).GetField("_animations",
                BindingFlags.NonPublic | BindingFlags.Instance);

            if (animationsField?.GetValue(atlas) is Dictionary<string, Animation> animations)
            {
                return new List<string>(animations.Keys);
            }

            // Return empty list if we can't access the dictionary
            return new List<string>();
        }
    }
}