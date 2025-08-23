using Hearthvale.GameCode.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Graphics;

namespace Hearthvale.GameCode.Entities
{
    public static class CharacterHitboxExtensions
    {
        // Tight AABB in world space from current sprite frame's opaque pixels
        public static Rectangle GetTightSpriteBounds(this Character character, byte alphaThreshold = 25)
        {
            var sprite = character?.Sprite;
            if (sprite == null)
                return Rectangle.Empty;

            // Get the actual position where the sprite is rendered
            Vector2 actualPosition = character.Position;
            
            // Use SpriteAnalyzer to get the content bounds
            Rectangle contentBounds = Rectangle.Empty;
            if (sprite.Region?.Texture != null)
            {
                contentBounds = SpriteAnalyzer.GetContentBounds(
                    sprite.Region.Texture,
                    sprite.Region.SourceRectangle
                );
            }
            
            // If we couldn't analyze the sprite, fall back to full sprite bounds
            if (contentBounds.IsEmpty)
            {
                int w = sprite.Region?.Width ?? (int)sprite.Width;
                int h = sprite.Region?.Height ?? (int)sprite.Height;
                contentBounds = new Rectangle(0, 0, w, h);
            }

            // Apply scale to the content bounds
            var scale = sprite.Scale;
            int scaledWidth = (int)(contentBounds.Width * scale.X);
            int scaledHeight = (int)(contentBounds.Height * scale.Y);

            // Get sprite origin - this is the pivot point for rendering
            Vector2 origin = sprite.Origin;
            
            // Calculate the world position, accounting for sprite effects and origin
            var effects = sprite.Effects;
            bool flipX = (effects & SpriteEffects.FlipHorizontally) != 0;

            // The sprite is drawn at position with origin as the pivot
            // So the top-left of the sprite is at: position - origin
            float spriteTopLeftX = actualPosition.X - (origin.X * scale.X);
            float spriteTopLeftY = actualPosition.Y - (origin.Y * scale.Y);

            // Now calculate where the content bounds are within the sprite
            float worldX, worldY;
            
            if (flipX)
            {
                // When flipped horizontally, content is mirrored around the sprite center
                float fullSpriteWidth = (sprite.Region?.Width ?? sprite.Width) * scale.X;
                
                // Calculate the mirrored position of the content
                float mirroredContentX = (sprite.Region?.Width ?? sprite.Width) - contentBounds.Right;
                worldX = spriteTopLeftX + (mirroredContentX * scale.X);
                worldY = spriteTopLeftY + (contentBounds.Y * scale.Y);
            }
            else
            {
                // Normal positioning - content bounds relative to sprite top-left
                worldX = spriteTopLeftX + (contentBounds.X * scale.X);
                worldY = spriteTopLeftY + (contentBounds.Y * scale.Y);
            }

            return new Rectangle(
                (int)worldX,
                (int)worldY,
                scaledWidth,
                scaledHeight
            );
        }
    }
}