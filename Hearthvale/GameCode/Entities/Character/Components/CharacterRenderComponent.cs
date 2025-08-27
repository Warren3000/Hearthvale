using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Hearthvale.GameCode.Utils;
using System;
using Hearthvale.GameCode.Entities.NPCs;

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

            var opacity = 1f;
            if (_character is NPC npc)
            {
                // Use reflection or make _fadeOpacity accessible
                opacity = npc.GetType().GetField("_fadeOpacity", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.GetValue(npc) as float? ?? 1f;
            }

            // Skip drawing if fully transparent
            if (opacity <= 0f) return;

            // Store original color
            var originalColor = _character.Sprite.Color;
            
            // Apply opacity to sprite
            _character.Sprite.Color = originalColor * opacity;
            
            // Calculate character center based on the actual tight bounds for accurate weapon positioning
            Rectangle tightBounds = _character.GetTightSpriteBounds();
            Vector2 characterCenter = new Vector2(
                tightBounds.Left + tightBounds.Width / 2f,
                tightBounds.Top + tightBounds.Height / 2f
            );
            
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
            if (_character.Sprite == null || (_character.IsDefeated && _character.HealthComponent.IsReadyToRemove))
                return;

            // Draw the sprite using its internal position
            _character.Sprite.Draw(spriteBatch);
        }
        public void DrawWeapon(SpriteBatch spriteBatch, Vector2 characterCenter, bool drawBehind)
        {
            if (_character.EquippedWeapon == null) return;

            // The characterCenter should already be calculated from tight bounds in CharacterRenderComponent
            _character.EquippedWeapon.Draw(spriteBatch, characterCenter);
        }
        private void DrawWeaponBehind(SpriteBatch spriteBatch, Vector2 characterCenter)
        {
            // Also update this method to use the public accessor
            if (_character.GetShouldDrawWeaponBehind() && _character.EquippedWeapon != null)
            {
                DrawWeapon(spriteBatch, characterCenter, true);
            }
        }

        private void DrawWeaponInFront(SpriteBatch spriteBatch, Vector2 characterCenter)
        {
            // And this method too
            if (!_character.GetShouldDrawWeaponBehind() && _character.EquippedWeapon != null)
            {
                DrawWeapon(spriteBatch, characterCenter, false);
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