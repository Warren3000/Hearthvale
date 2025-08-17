using Hearthvale.GameCode.Entities.NPCs;
using Hearthvale.GameCode.Managers;
using Hearthvale.GameCode.Utils;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Hearthvale.GameCode.Entities.Players.Components
{
    public class PlayerCollisionComponent
    {
        private readonly Player _player;
        private IEnumerable<Rectangle> _currentObstacles;
        private IEnumerable<NPC> _currentNpcs;

        public PlayerCollisionComponent(Player player)
        {
            _player = player;
        }

        public void UpdateObstacles(IEnumerable<Rectangle> obstacleRects, IEnumerable<NPC> npcs)
        {
            _currentObstacles = obstacleRects;
            _currentNpcs = npcs;
        }

        public IEnumerable<Rectangle> GetObstacleRectangles()
        {
            var obstacles = new List<Rectangle>();

            // Add static obstacles
            if (_currentObstacles != null)
            {
                obstacles.AddRange(_currentObstacles);
            }

            // Add NPC bounds (except defeated ones)
            if (_currentNpcs != null)
            {
                foreach (var npc in _currentNpcs.Where(n => !n.IsDefeated))
                {
                    obstacles.Add(npc.Bounds);
                }
            }

            return obstacles;
        }

        public bool TrySetPositionWithWallSliding(Vector2 candidate, IEnumerable<Rectangle> obstacles)
        {
            // Add NaN check at the start
            if (float.IsNaN(candidate.X) || float.IsNaN(candidate.Y))
            {
                Debug.WriteLine($"❌ CRITICAL: TrySetPositionWithWallSliding called with NaN candidate: {candidate}");
                return false;
            }

            Vector2 currentPos = _player.Position;
            Vector2 movement = candidate - currentPos;

            // If no movement, return true (no collision)
            if (movement.LengthSquared() < 0.001f)
                return true;

            // Try full movement first
            if (!IsPositionBlocked(candidate, obstacles))
            {
                _player.SetPosition(candidate);
                return true;
            }

            // If full movement is blocked, try sliding along walls

            // Try horizontal movement only (slide along vertical walls)
            Vector2 horizontalTarget = new Vector2(candidate.X, currentPos.Y);
            if (!IsPositionBlocked(horizontalTarget, obstacles))
            {
                _player.SetPosition(horizontalTarget);
                return true;
            }

            // Try vertical movement only (slide along horizontal walls)
            Vector2 verticalTarget = new Vector2(currentPos.X, candidate.Y);
            if (!IsPositionBlocked(verticalTarget, obstacles))
            {
                _player.SetPosition(verticalTarget);
                return true;
            }

            // If both individual axes are blocked, stay at current position
            return false;
        }

        public bool IsPositionBlocked(Vector2 position, IEnumerable<Rectangle> obstacles)
        {
            // Check if candidate position would collide with any obstacle
            Rectangle candidateBounds = new Rectangle(
                (int)position.X + 8,
                (int)position.Y + 16,
                (int)_player.Sprite.Width / 2,
                (int)_player.Sprite.Height / 2
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