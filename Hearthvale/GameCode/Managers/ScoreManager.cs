using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Hearthvale.GameCode.Managers
{
    /// <summary>
    /// Manages the player's score and score display.
    /// </summary>
    public class ScoreManager
    {
        private int _score;
        private readonly Vector2 _position;
        private readonly Vector2 _origin;
        private readonly SpriteFont _font;

        public int Score => _score;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScoreManager"/> class.
        /// </summary>
        /// <param name="font">The font used to draw the score.</param>
        /// <param name="position">The position to draw the score.</param>
        /// <param name="origin">The origin for score text alignment.</param>
        public ScoreManager(SpriteFont font, Vector2 position, Vector2 origin)
        {
            _font = font;
            _position = position;
            _origin = origin;
            _score = 0;
        }

        /// <summary>
        /// Adds the specified amount to the score.
        /// </summary>
        public void Add(int amount)
        {
            _score += amount;
        }

        /// <summary>
        /// Sets the score to the specified value.
        /// </summary>
        public void Set(int value)
        {
            _score = value;
        }

        /// <summary>
        /// Draws the score using the provided SpriteBatch.
        /// </summary>
        public void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.DrawString(
                _font,
                $"Score: {_score}",
                _position,
                Color.White,
                0.0f,
                _origin,
                1.0f,
                SpriteEffects.None,
                0.0f
            );
        }
    }
}
