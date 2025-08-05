using Microsoft.Xna.Framework;
using MonoGameLibrary.Graphics;
using System.Collections.Generic;

namespace Hearthvale.GameCode.Utils
{
    public static class MapUtils
    {
        public static List<Rectangle> GetWallRectangles(Tilemap tilemap, int originalWallTileId)
        {
            var wallRects = new List<Rectangle>();

            for (int row = 0; row < tilemap.Rows; row++)
            {
                for (int col = 0; col < tilemap.Columns; col++)
                {
                    int tileId = tilemap.GetTileId(col, row);
                    // Use AutotileMapper to check if this is any type of wall tile
                    if (AutotileMapper.IsWallTile(tileId))
                    {
                        wallRects.Add(new Rectangle(
                            (int)(col * tilemap.TileWidth),
                            (int)(row * tilemap.TileHeight),
                            (int)tilemap.TileWidth,
                            (int)tilemap.TileHeight
                        ));
                    }
                }
            }

            return wallRects;
        }

        /// <summary>
        /// Checks if a tile ID represents a floor tile using XML configuration ONLY
        /// </summary>
        public static bool IsFloorTile(int tileId)
        {
            return AutotileMapper.IsFloorTile(tileId);
        }

        /// <summary>
        /// Gets the appropriate wall tile ID to use for collision detection
        /// This accounts for autotiling where the original wall ID might have changed
        /// </summary>
        public static int GetEffectiveWallTileId(int originalWallTileId)
        {
            // After autotiling, we need to check all possible wall tile IDs
            // The AutotileMapper.IsWallTile method handles this for us
            return originalWallTileId; // We'll use this as a reference, but actual checking uses IsWallTile
        }
    }
}