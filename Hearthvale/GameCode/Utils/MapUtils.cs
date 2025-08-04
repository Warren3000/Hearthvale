
using Microsoft.Xna.Framework;
using MonoGameLibrary.Graphics;
using System.Collections.Generic;

namespace Hearthvale.GameCode.Utils
{
    public static class MapUtils
    {
        public static List<Rectangle> GetWallRectangles(Tilemap tilemap, int wallTileId)
        {
            var wallRects = new List<Rectangle>();
            for (int row = 0; row < tilemap.Rows; row++)
            {
                for (int col = 0; col < tilemap.Columns; col++)
                {
                    if (tilemap.GetTileId(col, row) == wallTileId)
                    {
                        int x = (int)(col * tilemap.TileWidth);
                        int y = (int)(row * tilemap.TileHeight);
                        wallRects.Add(new Rectangle(x, y, (int)tilemap.TileWidth, (int)tilemap.TileHeight));
                    }
                }
            }
            return wallRects;
        }
    }
}