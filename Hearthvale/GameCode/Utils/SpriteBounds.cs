using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Graphics;

namespace Hearthvale.GameCode.Utils
{
    public static class SpriteBounds
    {
        // Computes tight bounds around non-alpha pixels for a TextureRegion
        public static bool TryGetOpaqueBounds(TextureRegion region, byte alphaThreshold, out Rectangle bounds)
        {
            bounds = Rectangle.Empty;
            if (region?.Texture == null) return false;

            Rectangle src = region.SourceRectangle;
            if (src.Width <= 0 || src.Height <= 0) return false;

            var pixels = new Color[src.Width * src.Height];
            region.Texture.GetData(0, src, pixels, 0, pixels.Length);

            int minX = int.MaxValue, minY = int.MaxValue;
            int maxX = int.MinValue, maxY = int.MinValue;
            bool any = false;

            for (int y = 0; y < src.Height; y++)
            {
                int row = y * src.Width;
                for (int x = 0; x < src.Width; x++)
                {
                    if (pixels[row + x].A >= alphaThreshold)
                    {
                        any = true;
                        if (x < minX) minX = x;
                        if (y < minY) minY = y;
                        if (x > maxX) maxX = x;
                        if (y > maxY) maxY = y;
                    }
                }
            }

            if (!any) return false;

            bounds = new Rectangle(minX, minY, maxX - minX + 1, maxY - minY + 1);
            return true;
        }

        // Returns the visual center offset from the region top-left in region-local pixels
        public static Vector2 GetVisualCenterOffset(TextureRegion region, byte alphaThreshold = 8)
        {
            if (TryGetOpaqueBounds(region, alphaThreshold, out var b))
            {
                return new Vector2(b.Left + b.Width / 2f, b.Top + b.Height / 2f);
            }

            // Fallback to geometric center of the region if opaque bounds fail
            return new Vector2(region.Width / 2f, region.Height / 2f);
        }
    }
}