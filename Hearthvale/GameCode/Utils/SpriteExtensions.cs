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
        /// Gets the content bounds of a sprite
        /// </summary>
        public static Rectangle GetContentBounds(this AnimatedSprite sprite)
        {
            if (sprite?.Region?.Texture == null)
                return Rectangle.Empty;
                
            return SpriteAnalyzer.GetContentBounds(
                sprite.Region.Texture, 
                sprite.Region.SourceRectangle);
        }
        
        /// <summary>
        /// Gets the position to render a sprite, adjusted for content bounds
        /// </summary>
        [System.Obsolete("Atlas now top-left aligned; use logical position directly.")]
        public static Vector2 GetContentPosition(this AnimatedSprite sprite, Vector2 logicalPosition)
            => logicalPosition;
        
        ///// <summary>
        ///// Gets tight sprite bounds for collision detection
        ///// </summary>
        //public static Rectangle GetTightSpriteBounds(this Character character)
        //{
        //    if (character?.Sprite == null)
        //        return Rectangle.Empty;
                
        //    Rectangle contentBounds = character.Sprite.GetContentBounds();
            
        //    // Return bounds based on actual content, at character position
        //    return new Rectangle(
        //        (int)character.Position.X + contentBounds.X,
        //        (int)character.Position.Y + contentBounds.Y,
        //        contentBounds.Width,
        //        contentBounds.Height);
        //}
    }
}