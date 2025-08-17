using Hearthvale.GameCode.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Graphics;

namespace Hearthvale.GameCode.Tools
{
    /// <summary>
    /// Development tool for analyzing and visualizing sprite bounds
    /// </summary>
    public class SpriteAnalysisTool
    {
        private readonly Texture2D _pixel;

        public SpriteAnalysisTool(GraphicsDevice graphicsDevice)
        {
            _pixel = new Texture2D(graphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });
        }

        /// <summary>
        /// Visualizes the content bounds of a sprite
        /// </summary>
        public void VisualizeSpriteBounds(SpriteBatch spriteBatch, AnimatedSprite sprite, Vector2 position)
        {
            if (sprite?.Region?.Texture == null)
                return;

            // Draw the full sprite bounds
            var fullBounds = new Rectangle(
                (int)position.X,
                (int)position.Y,
                (int)sprite.Width,
                (int)sprite.Height
            );
            DrawRectangleOutline(spriteBatch, fullBounds, Color.Yellow);

            // Get the actual content bounds
            Rectangle contentBounds = SpriteAnalyzer.GetContentBounds(
                sprite.Region.Texture,
                sprite.Region.SourceRectangle
            );

            // Draw the content bounds
            var actualBounds = new Rectangle(
                (int)position.X + contentBounds.X,
                (int)position.Y + contentBounds.Y,
                contentBounds.Width,
                contentBounds.Height
            );
            DrawRectangleOutline(spriteBatch, actualBounds, Color.Lime);

            // Draw the content position
            Vector2 contentPosition = sprite.GetContentPosition(position);
            DrawPoint(spriteBatch, contentPosition, Color.Red, 3);
        }

        private void DrawRectangleOutline(SpriteBatch spriteBatch, Rectangle rect, Color color)
        {
            // Top line
            spriteBatch.Draw(_pixel, new Rectangle(rect.Left, rect.Top, rect.Width, 1), color);
            // Left line
            spriteBatch.Draw(_pixel, new Rectangle(rect.Left, rect.Top, 1, rect.Height), color);
            // Right line
            spriteBatch.Draw(_pixel, new Rectangle(rect.Right, rect.Top, 1, rect.Height), color);
            // Bottom line
            spriteBatch.Draw(_pixel, new Rectangle(rect.Left, rect.Bottom, rect.Width + 1, 1), color);
        }

        private void DrawPoint(SpriteBatch spriteBatch, Vector2 position, Color color, int size = 2)
        {
            spriteBatch.Draw(_pixel,
                new Rectangle((int)position.X - size / 2, (int)position.Y - size / 2, size, size),
                color);
        }
    }
}