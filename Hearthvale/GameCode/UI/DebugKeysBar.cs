using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace Hearthvale.GameCode.UI
{
    public class DebugKeysBar
    {
        private readonly SpriteFont _font;
        public Texture2D _whitePixel;
        private readonly List<(string Key, string Description)> _keys;
        private readonly int _keyWidth = 48;
        private readonly int _keyHeight = 48;
        private readonly int _spacing = 64;

        public DebugKeysBar(SpriteFont font, Texture2D whitePixel, List<(string Key, string Description)> keys)
        {
            _font = font;
            _whitePixel = whitePixel;
            _keys = keys;
        }

        public void Draw(SpriteBatch spriteBatch, int screenWidth, int screenHeight)
        {
            const float backgroundLayer = 0.97f;
            const float textLayer = 0.99f;

            int totalWidth = _keys.Count * _keyWidth + (_keys.Count - 1) * _spacing;
            int startX = (screenWidth - totalWidth) / 2;
            int y = screenHeight - _keyHeight - 32;

            // First pass: draw all key backgrounds and borders
            for (int i = 0; i < _keys.Count; i++)
            {
                int x = startX + i * (_keyWidth + _spacing);
                var rect = new Rectangle(x, y, _keyWidth, _keyHeight);

                // Draw key background
                spriteBatch.Draw(_whitePixel, rect, null, Color.Black * 0.7f, 0f, Vector2.Zero, SpriteEffects.None, backgroundLayer);
                // Draw border
                spriteBatch.Draw(_whitePixel, new Rectangle(x, y, _keyWidth, 2), null, Color.White * 0.7f, 0f, Vector2.Zero, SpriteEffects.None, backgroundLayer); // Top
                spriteBatch.Draw(_whitePixel, new Rectangle(x, y + _keyHeight - 2, _keyWidth, 2), null, Color.White * 0.7f, 0f, Vector2.Zero, SpriteEffects.None, backgroundLayer); // Bottom
                spriteBatch.Draw(_whitePixel, new Rectangle(x, y, 2, _keyHeight), null, Color.White * 0.7f, 0f, Vector2.Zero, SpriteEffects.None, backgroundLayer); // Left
                spriteBatch.Draw(_whitePixel, new Rectangle(x + _keyWidth - 2, y, 2, _keyHeight), null, Color.White * 0.7f, 0f, Vector2.Zero, SpriteEffects.None, backgroundLayer); // Right
            }

            // Second pass: draw all key labels and descriptions
            for (int i = 0; i < _keys.Count; i++)
            {
                int x = startX + i * (_keyWidth + _spacing);
                var keyText = _keys[i].Key;
                var keySize = _font.MeasureString(keyText);
                var keyPos = new Vector2(x + (_keyWidth - keySize.X) / 2, y + 6);
                // Key label shadow
                spriteBatch.DrawString(_font, keyText, keyPos + Vector2.One, Color.Black * 0.6f, 0f, Vector2.Zero, 1f, SpriteEffects.None, textLayer);
                // Key label
                spriteBatch.DrawString(_font, keyText, keyPos, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, textLayer);

                // Description (below key)
                var descText = _keys[i].Description;
                var descSize = _font.MeasureString(descText) * 0.6f;
                var descPos = new Vector2(x + (_keyWidth - descSize.X) / 2, y + _keyHeight - descSize.Y + 14);
                // Description shadow
                spriteBatch.DrawString(_font, descText, descPos + Vector2.One, Color.Black * 0.5f, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, textLayer);
                // Description
                spriteBatch.DrawString(_font, descText, descPos, Color.Yellow, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, textLayer);
            }
        }
    }
}