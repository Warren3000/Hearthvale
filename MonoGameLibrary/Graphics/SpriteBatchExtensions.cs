using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
