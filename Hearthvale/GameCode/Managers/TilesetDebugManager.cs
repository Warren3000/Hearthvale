using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Graphics;
using System;

namespace Hearthvale.GameCode.Managers
{
    /// <summary>
    /// A debug utility to draw entire tilesets on the screen for verification.
    /// </summary>
    public static class TilesetDebugManager
    {
        private static SpriteFont _font;
        private static bool _initialized;
        private static bool _showTileCoordinates = false;
        public static bool ShowTileCoordinates => _showTileCoordinates;

        /// <summary>
        /// Initializes the debug viewer with the necessary assets.
        /// </summary>
        public static void Initialize(SpriteFont font)
        {
            _font = font;
            _initialized = true;
        }

        public static void ToggleShowTileCoordinates()
        {
            _showTileCoordinates = !_showTileCoordinates;
        }
        public static void DrawTileCoordinatesOverlay(SpriteBatch spriteBatch, Tilemap tilemap)
        {
            if (!_showTileCoordinates || _font == null || tilemap == null)
                return;

            int rows = tilemap.Rows;
            int cols = tilemap.Columns;
            float tileWidth = tilemap.TileWidth;
            float tileHeight = tilemap.TileHeight;

            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    var region = tilemap.GetTile(col, row);
                    if (region == null) continue;

                    string coords = $"{col},{row}";
                    float textScale = 0.25f;
                    Vector2 textSize = _font.MeasureString(coords) * textScale;
                    Vector2 tilePos = new Vector2(col * tileWidth, row * tileHeight);
                    Vector2 textPos = tilePos + new Vector2((tileWidth - textSize.X) / 2, (tileHeight - textSize.Y) / 2);

                    // Draw shadow for readability
                    spriteBatch.DrawString(_font, coords, textPos + Vector2.One, Color.Black * 0.7f, 0f, Vector2.Zero, textScale, SpriteEffects.None, 0.95f);
                    // Draw main text
                    spriteBatch.DrawString(_font, coords, textPos, Color.Yellow, 0f, Vector2.Zero, textScale, SpriteEffects.None, 1.0f);
                }
            }
        }
        /// <summary>
        /// Draws the debug view if it is enabled in the DebugManager.
        /// </summary>
        public static void Draw(SpriteBatch spriteBatch)
        {
            if (!_initialized || !DebugManager.Instance.ShowTilesetViewer)
            {
                return;
            }

            var viewport = spriteBatch.GraphicsDevice.Viewport;
            const float padding = 50f;
            const float topMargin = 100f;

            var _wallTileset = TilesetManager.Instance.WallTileset;
            var _floorTileset = TilesetManager.Instance.FloorTileset;

            // Calculate the total unscaled size required to draw both tilesets side-by-side
            float totalUnscaledWidth = (_wallTileset.Columns * _wallTileset.TileWidth) + padding + (_floorTileset.Columns * _floorTileset.TileWidth);
            float maxUnscaledHeight = Math.Max(_wallTileset.Rows * _wallTileset.TileHeight, _floorTileset.Rows * _floorTileset.TileHeight);

            // Determine the scale factor to fit them within the viewport
            float scaleX = (viewport.Width - (padding * 2)) / totalUnscaledWidth;
            float scaleY = (viewport.Height - (topMargin + padding)) / maxUnscaledHeight;
            float scale = Math.Min(scaleX, scaleY);
            scale = 4f;
            // Draw a semi-transparent background for the entire viewer
            float totalRenderedWidth = (_wallTileset.Columns * _wallTileset.TileWidth * scale) + padding + (_floorTileset.Columns * _floorTileset.TileWidth * scale);
            float maxRenderedHeight = Math.Max(_wallTileset.Rows * _wallTileset.TileHeight * scale, _floorTileset.Rows * _floorTileset.TileHeight * scale);

            // Draw wall tileset
            Vector2 wallPosition = new Vector2(padding, topMargin);
            DrawTileset(spriteBatch, _wallTileset, "Wall Tileset", wallPosition, scale);

            // Calculate position for the floor tileset
            float wallTilesetRenderWidth = _wallTileset.Columns * _wallTileset.TileWidth * scale;
            Vector2 floorPosition = new Vector2(wallPosition.X + wallTilesetRenderWidth + padding, wallPosition.Y);
            DrawTileset(spriteBatch, _floorTileset, "Floor Tileset", floorPosition, scale);
        }

        /// <summary>
        /// Draws a single tileset with its label and tile coordinates.
        /// </summary>
        private static void DrawTileset(SpriteBatch spriteBatch, Tileset tileset, string label, Vector2 position, float scale)
        {
            if (tileset == null) return;

            int rows = tileset.Rows;
            int cols = tileset.Columns;

            // Draw the label for the tileset (drawn above everything)
            spriteBatch.DrawString(_font, label, position - new Vector2(0, 30), Color.White, 0f, Vector2.Zero, 1.3f, SpriteEffects.None, 1.0f);

            // First pass: draw all tile images at layerDepth 0.5f
            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    TextureRegion tile = tileset.GetTile(col, row);
                    if (tile == null) continue;

                    float scaledTileWidth = tileset.TileWidth * scale;
                    float scaledTileHeight = tileset.TileHeight * scale;

                    var tilePosition = new Vector2(
                        position.X + col * scaledTileWidth,
                        position.Y + row * scaledTileHeight
                    );

                    var destRect = new Rectangle((int)tilePosition.X, (int)tilePosition.Y, (int)scaledTileWidth, (int)scaledTileHeight);
                    // Use layerDepth = 0.5f for tiles
                    spriteBatch.Draw(tile.Texture, destRect, tile.SourceRectangle, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.5f);
                }
            }

            // Second pass: draw all tile coordinate texts at layerDepth 0.9f
            if (_showTileCoordinates)
            {
                for (int row = 0; row < rows; row++)
                {
                    for (int col = 0; col < cols; col++)
                    {
                        TextureRegion tile = tileset.GetTile(col, row);
                        if (tile == null) continue;

                        float scaledTileWidth = tileset.TileWidth * scale;
                        float scaledTileHeight = tileset.TileHeight * scale;

                        var tilePosition = new Vector2(
                            position.X + col * scaledTileWidth,
                            position.Y + row * scaledTileHeight
                        );

                        string coords = $"{col},{row}";
                        float textScale = 1f;
                        Vector2 textSize = _font.MeasureString(coords) * textScale;
                        Vector2 textPosition = new Vector2(
                            tilePosition.X + (scaledTileWidth - textSize.X) / 2,
                            tilePosition.Y + (scaledTileHeight - textSize.Y) / 2
                        );

                        spriteBatch.DrawString(_font, coords, textPosition, Color.White, 0f, Vector2.Zero, textScale, SpriteEffects.None, 0.9f);
                    }
                }
            }
        }
    }
}