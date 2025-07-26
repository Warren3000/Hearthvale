using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGameLibrary.Graphics
{
    public static class SpriteBatchExtensions
    {
        public static void DrawPixel(this SpriteBatch spriteBatch, Texture2D pixel, Rectangle destinationRectangle, Color color)
        {
            spriteBatch.Draw(pixel, destinationRectangle, color);
        }
    }
}
