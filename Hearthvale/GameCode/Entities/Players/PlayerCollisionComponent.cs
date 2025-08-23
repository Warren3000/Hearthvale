using Hearthvale.GameCode.Entities.NPCs;
using Hearthvale.GameCode.Entities.Players;
using Hearthvale.GameCode.Managers;
using Hearthvale.GameCode.Utils;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Hearthvale.GameCode.Entities.Components
{
    public class PlayerCollisionComponent
    {
        private readonly Player _player;
        

        public PlayerCollisionComponent(Player player)
        {
            _player = player;
        }

        public bool IsPositionBlocked(Vector2 position, IEnumerable<Rectangle> obstacles)
        {
            // Check if candidate position would collide with any obstacle
            Rectangle candidateBounds = new Rectangle(
                (int)position.X + 8,
                (int)position.Y + 16,
                (int)_player.Bounds.Width / 2,
                (int)_player.Bounds.Height / 2
            );

            foreach (var obstacle in obstacles)
            {
                if (candidateBounds.Intersects(obstacle))
                    return true;
            }

            // Check against tilemap walls
            if (_player.CollisionComponent.Tilemap != null && TilesetManager.Instance.WallTileset != null)
            {
                var tilemap = _player.CollisionComponent.Tilemap;
                int leftTile = candidateBounds.Left / (int)tilemap.TileWidth;
                int rightTile = (candidateBounds.Right - 1) / (int)tilemap.TileWidth;
                int topTile = candidateBounds.Top / (int)tilemap.TileHeight;
                int bottomTile = (candidateBounds.Bottom - 1) / (int)tilemap.TileHeight;

                for (int col = leftTile; col <= rightTile; col++)
                {
                    for (int row = topTile; row <= bottomTile; row++)
                    {
                        if (col >= 0 && col < tilemap.Columns && row >= 0 && row < tilemap.Rows)
                        {
                            if (tilemap.GetTileset(col, row) == TilesetManager.Instance.WallTileset &&
                                AutotileMapper.IsWallTile(tilemap.GetTileId(col, row)))
                            {
                                return true; // Collision with a wall
                            }
                        }
                    }
                }
            }

            return false;
        }
    }
}