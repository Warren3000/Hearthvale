using Hearthvale.GameCode.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Graphics;
using System;
using System.Collections.Generic;

namespace Hearthvale.GameCode.Entities
{
    public static class CharacterHitboxExtensions
    {
        // Tight AABB in world space from current sprite frame's opaque pixels
        public static Rectangle GetTightSpriteBounds(this Character character, byte alphaThreshold = 25)
        {
            // Atlas convention:
            //  - 1px transparent padding on left and top (skip for collision)
            //  - Sprite may be smaller than 32x32; right/bottom is transparent if unused
            //  - Sprite is always left/top aligned in cell
            var sprite = character?.Sprite;
            if (sprite?.Region == null || sprite.Region.Texture == null)
                return Rectangle.Empty;

            // Analyze the current frame's opaque region (relative to the frame's source rectangle)
            Rectangle content = SpriteAnalyzer.GetContentBounds(sprite.Region.Texture, sprite.Region.SourceRectangle);

            // Offset by 1px left/top padding (atlas convention)
            int offsetX = 1;
            int offsetY = 1;

            // The content rect is relative to the frame, which is already left/top aligned in the cell
            // So world position = logical position + content.X + 1, content.Y + 1
            Vector2 pos = character.Position;
            Vector2 origin = sprite.Origin; // usually zero
            var scale = sprite.Scale;

            float worldX = pos.X - origin.X * scale.X + (content.X + offsetX) * scale.X;
            float worldY = pos.Y - origin.Y * scale.Y + (content.Y + offsetY) * scale.Y;
            int worldW = (int)(content.Width * scale.X);
            int worldH = (int)(content.Height * scale.Y);

            return new Rectangle((int)worldX, (int)worldY, worldW, worldH);
        }
    }
}