using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Hearthvale
{
    public class ScoreManager
    {
        private int _score;
        private Vector2 _position;
        private Vector2 _origin;
        private SpriteFont _font;

        public int Score => _score;

        public ScoreManager(SpriteFont font, Vector2 position, Vector2 origin)
        {
            _font = font;
            _position = position;
            _origin = origin;
            _score = 0;
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
