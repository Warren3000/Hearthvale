using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace MonoGameLibrary.Graphics
{
    public class Tileset
    {
        private readonly TextureRegion[] _tiles;
        public int Columns { get; }
        public int Rows { get; }

        public int TileWidth { get; }
        public int TileHeight { get; }
        public int Count => _tiles.Length;

        /// <summary>
        /// Creates a new tileset based on the given texture region with the specified
        /// tile width and height.
        /// </summary>
        /// <param name="textureRegion">The texture region that contains the tiles for the tileset.</param>
        /// <param name="tileWidth">The width of each tile in the tileset.</param>
        /// <param name="tileHeight">The height of each tile in the tileset.</param>
        public Tileset(TextureRegion textureRegion, int tileWidth, int tileHeight)
        {
            TileWidth = tileWidth;
            TileHeight = tileHeight;

            // Use Math.Ceiling to prevent off-by-one errors from texture dimensions
            Columns = (int)Math.Ceiling((double)textureRegion.Width / tileWidth);
            Rows = (int)Math.Ceiling((double)textureRegion.Height / tileHeight);

            int tileCount = Rows * Columns;
            _tiles = new TextureRegion[tileCount];

            for (int i = 0; i < tileCount; i++)
            {
                int x = (i % Columns) * tileWidth;
                int y = (i / Columns) * tileHeight;

                // Ensure the sub-region does not exceed the bounds of the main texture region
                int width = Math.Min(tileWidth, textureRegion.Width - x);
                int height = Math.Min(tileHeight, textureRegion.Height - y);

                if (width > 0 && height > 0)
                {
                    _tiles[i] = new TextureRegion(textureRegion.Texture,
                        textureRegion.SourceRectangle.X + x,
                        textureRegion.SourceRectangle.Y + y,
                        width,
                        height);
                }
            }
        }

        /// <summary>
        /// Gets the texture region for the tile from this tileset at the given index.
        /// </summary>
        /// <param name="index">The index of the texture region in this tile set.</param>
        /// <returns>The texture region for the tile form this tileset at the given index.</returns>
        public TextureRegion GetTile(int index)
        {
            if (index >= 0 && index < _tiles.Length)
                return _tiles[index];

            // Return a default or empty tile if the index is out of bounds
            // This prevents crashes if an invalid tile ID is used.
            return _tiles.Length > 0 ? _tiles[0] : null;
        }

        /// <summary>
        /// Gets the texture region for the tile from this tileset at the given location.
        /// </summary>
        /// <param name="column">The column in this tileset of the texture region.</param>
        /// <param name="row">The row in this tileset of the texture region.</param>
        /// <returns>The texture region for the tile from this tileset at given location.</returns>
        public TextureRegion GetTile(int column, int row)
        {
            int index = row * Columns + column;
            return GetTile(index);
        }

        public int GetTileId(int column, int row)
        {
            int index = row * Columns + column;
            if (index >= 0 && index < _tiles.Length)
                return index;
            return -1;
        }
    }
}