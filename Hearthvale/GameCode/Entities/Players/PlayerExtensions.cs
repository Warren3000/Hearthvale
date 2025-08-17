using Hearthvale.GameCode.Utils;
using Microsoft.Xna.Framework;

namespace Hearthvale.GameCode.Entities.Players
{
    public static class PlayerExtensions
    {
        // World-space visual center (ignores transparent padding in the sprite)
        public static Vector2 GetVisualCenter(this Player player, byte alphaThreshold = 8)
        {
            var sprite = player.Sprite;
            if (sprite?.Region == null)
            {
                return player.Position;
            }

            // Visual center from region-local pixels, scaled to world space
            Vector2 localCenter = SpriteBounds.GetVisualCenterOffset(sprite.Region, alphaThreshold);
            Vector2 scaled = new Vector2(localCenter.X * sprite.Scale.X, localCenter.Y * sprite.Scale.Y);
            return player.Position + scaled;
        }
    }
}