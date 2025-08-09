using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Hearthvale.GameCode.Managers
{
    /// <summary>
    /// Manages the player's score and score display.
    /// </summary>
    public class ScoreManager
    {
        private static ScoreManager _instance;
        public static ScoreManager Instance => _instance ?? throw new System.InvalidOperationException("ScoreManager not initialized. Call Initialize first.");

        private int _score;
        private Vector2 _position;
        private Vector2 _origin;
        private SpriteFont _font;
        private float _fontScale = 0.8f;
        private Texture2D _whitePixel;

        public int Score => _score;

        private ScoreManager(SpriteFont font, Vector2 position, Vector2 origin)
        {
            _font = font;
            _position = position;
            _origin = origin;
            _score = 0;

            // Create white pixel texture for drawing backgrounds
            _whitePixel = new Texture2D(font.Texture.GraphicsDevice, 1, 1);
            _whitePixel.SetData(new[] { Color.White });
        }

        /// <summary>
        /// Initializes the singleton instance. Call this once at startup.
        /// </summary>
        public static void Initialize(SpriteFont font, Vector2 position, Vector2 origin)
        {
            _instance = new ScoreManager(font, position, origin);
        }

        public void Add(int amount)
        {
            _score += amount;
        }

        public void Set(int value)
        {
            _score = value;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            // Position score in top-right corner, away from health bar
            var viewport = spriteBatch.GraphicsDevice.Viewport;
            var scoreText = $"Score: {_score}";
            var textSize = _font.MeasureString(scoreText) * _fontScale;
            
            // Position with margin from top-right corner
            var position = new Vector2(
                viewport.Width - textSize.X - 30f, // Increased margin from 20f to 30f
                25f // Increased margin from 20f to 25f for better spacing
            );

            // Add background panel for better visibility
            var panelPadding = new Vector2(10, 6); // Increased padding
            var panelSize = textSize + panelPadding * 2;
            var panelPos = position - panelPadding;
            
            // Draw semi-transparent background
            var panelRect = new Rectangle((int)panelPos.X, (int)panelPos.Y, (int)panelSize.X, (int)panelSize.Y);
            spriteBatch.Draw(_whitePixel, panelRect, Color.Black * 0.6f); // Increased opacity
            
            // Draw border
            DrawBorder(spriteBatch, _whitePixel, panelRect, Color.White * 0.4f, 1);

            // Draw text without shadow first to test
            spriteBatch.DrawString(_font, scoreText, position, Color.Gold, 0f, Vector2.Zero, _fontScale, SpriteEffects.None, 0f);
            
            // If the above works, then add the shadow back:
            // var shadowPos = position + new Vector2(1, 1);
            // spriteBatch.DrawString(_font, scoreText, shadowPos, Color.Black * 0.7f, 0f, Vector2.Zero, _fontScale, SpriteEffects.None, 0f);
            // spriteBatch.DrawString(_font, scoreText, position, Color.Gold, 0f, Vector2.Zero, _fontScale, SpriteEffects.None, 0f);
        }

        private void DrawBorder(SpriteBatch spriteBatch, Texture2D pixel, Rectangle rect, Color color, int thickness)
        {
            // Top
            spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
            // Bottom
            spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
            // Left
            spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
            // Right
            spriteBatch.Draw(pixel, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
        }
    }
}