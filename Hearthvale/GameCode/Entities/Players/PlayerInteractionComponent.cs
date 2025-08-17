using Microsoft.Xna.Framework;

namespace Hearthvale.GameCode.Entities.Players.Components
{
    public class PlayerInteractionComponent
    {
        private readonly Player _player;

        public PlayerInteractionComponent(Player player)
        {
            _player = player;
        }

        public bool IsNearTile(int column, int row, float tileWidth, float tileHeight)
        {
            // Calculate the center of the target tile in world coordinates.
            Vector2 tileCenter = new Vector2(
                column * tileWidth + tileWidth / 2,
                row * tileHeight + tileHeight / 2
            );

            // Use the player's bounds for a more accurate center position.
            Vector2 playerCenter = _player.Bounds.Center.ToVector2();

            // Define the maximum distance for interaction. Let's use the tile's width as a radius.
            float interactionRadius = tileWidth;

            // Check if the distance between the player and the tile is within the interaction radius.
            return Vector2.Distance(playerCenter, tileCenter) <= interactionRadius;
        }
    }
}