using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Hearthvale.GameCode.Entities.Components
{
    /// <summary>
    /// Manages the rendering logic for a character
    /// </summary>
    public class CharacterRenderComponent
    {
        private readonly Character _character;

        public CharacterRenderComponent(Character character)
        {
            _character = character;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (_character.Sprite == null) return;

            Color originalColor = _character.Sprite.Color;
            ApplyVisualEffects();

            Vector2 characterCenter = _character.Position + new Vector2(_character.Sprite.Width / 2f, _character.Sprite.Height / 1.4f);
            bool drawWeaponBehind = _character.GetShouldDrawWeaponBehind();

            if (drawWeaponBehind)
                DrawWeaponBehind(spriteBatch, characterCenter);
            else
                DrawWeaponInFront(spriteBatch, characterCenter);

            DrawCharacter(spriteBatch);
            _character.Sprite.Color = originalColor;
        }

        private void DrawCharacter(SpriteBatch spriteBatch)
        {
            _character.Sprite.Draw(spriteBatch, _character.Position);
        }

        private void DrawWeaponBehind(SpriteBatch spriteBatch, Vector2 characterCenter)
        {
            // Also update this method to use the public accessor
            if (_character.GetShouldDrawWeaponBehind() && _character.EquippedWeapon != null)
            {
                _character.WeaponComponent.DrawWeapon(spriteBatch, characterCenter, true);
            }
        }

        private void DrawWeaponInFront(SpriteBatch spriteBatch, Vector2 characterCenter)
        {
            // And this method too
            if (!_character.GetShouldDrawWeaponBehind() && _character.EquippedWeapon != null)
            {
                _character.WeaponComponent.DrawWeaponInFront(spriteBatch, characterCenter);
            }
        }

        private void ApplyVisualEffects()
        {
            if (_character.IsDefeated && _character.Sprite != null)
            {
                _character.Sprite.Color = Color.White * 0.5f;
            }
        }

        public void DrawDebug(SpriteBatch spriteBatch, Texture2D pixel)
        {
            // Determine debug color based on character type
            bool isPlayer = _character.GetType().Name == "Player";
            Color color = isPlayer ? Color.LimeGreen * 0.5f : Color.Red * 0.5f;

            // Draw bounds
            DrawBoundingBox(spriteBatch, pixel, _character.Bounds, color);
        }

        private void DrawBoundingBox(SpriteBatch spriteBatch, Texture2D pixel, Rectangle rect, Color color)
        {
            // Top
            spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, 1), color);
            // Left  
            spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, 1, rect.Height), color);
            // Right
            spriteBatch.Draw(pixel, new Rectangle(rect.Right - 1, rect.Y, 1, rect.Height), color);
            // Bottom
            spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Bottom - 1, rect.Width, 1), color);
        }
    }
}