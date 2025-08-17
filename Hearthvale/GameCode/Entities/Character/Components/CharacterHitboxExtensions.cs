using Hearthvale.GameCode.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Hearthvale.GameCode.Entities
{
    public static class CharacterHitboxExtensions
    {
        // Tight AABB in world space from current sprite frame's opaque pixels
        public static Rectangle GetTightSpriteBounds(this Character character, byte alphaThreshold = 8)
        {
            var sprite = character?.Sprite;
            var region = sprite?.Region;
            if (sprite == null)
                return Rectangle.Empty;

            // 1) Region-local tight bounds from non-alpha pixels; fallback to full region if missing
            Rectangle localOpaque;
            if (region == null || !SpriteBounds.TryGetOpaqueBounds(region, alphaThreshold, out localOpaque))
            {
                int w = region?.Width ?? (int)sprite.Width;
                int h = region?.Height ?? (int)sprite.Height;
                localOpaque = new Rectangle(0, 0, w, h);
            }

            // 2) Convert region-local rectangle to world space using the same draw transform:
            //    world = Position + (local - Origin) * Scale, honoring SpriteEffects flips.
            var scale = sprite.Scale;
            var origin = sprite.Origin;
            var pos = sprite.Position;
            var effects = sprite.Effects;

            bool flipX = (effects & SpriteEffects.FlipHorizontally) != 0;
            bool flipY = (effects & SpriteEffects.FlipVertically) != 0;

            // Rect edges in region-local pixels
            float l = localOpaque.Left;
            float r = localOpaque.Right;
            float t = localOpaque.Top;
            float b = localOpaque.Bottom;

            // Map X
            float x0 = flipX ? pos.X + (origin.X - r) * scale.X
                             : pos.X + (l - origin.X) * scale.X;
            float x1 = flipX ? pos.X + (origin.X - l) * scale.X
                             : pos.X + (r - origin.X) * scale.X;

            // Map Y
            float y0 = flipY ? pos.Y + (origin.Y - b) * scale.Y
                             : pos.Y + (t - origin.Y) * scale.Y;
            float y1 = flipY ? pos.Y + (origin.Y - t) * scale.Y
                             : pos.Y + (b - origin.Y) * scale.Y;

            int x = (int)System.Math.Floor(System.Math.Min(x0, x1));
            int y = (int)System.Math.Floor(System.Math.Min(y0, y1));
            int wWorld = (int)System.Math.Ceiling(System.Math.Abs(x1 - x0));
            int hWorld = (int)System.Math.Ceiling(System.Math.Abs(y1 - y0));

            return new Rectangle(x, y, wWorld, hWorld);
        }
    }
}