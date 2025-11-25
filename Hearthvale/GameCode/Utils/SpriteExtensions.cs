using Hearthvale.GameCode.Entities;
using Microsoft.Xna.Framework;
using MonoGameLibrary.Graphics;

namespace Hearthvale.GameCode.Utils
{
    /// <summary>
    /// Extension methods for working with sprites
    /// </summary>
    public static class SpriteExtensions
    {
        /// <summary>
        /// Gets the position to render a sprite, adjusted for content bounds
        /// </summary>
        [System.Obsolete("Atlas now top-left aligned; use logical position directly.")]
        public static Vector2 GetContentPosition(this AnimatedSprite sprite, Vector2 logicalPosition)
            => logicalPosition;
    }
}