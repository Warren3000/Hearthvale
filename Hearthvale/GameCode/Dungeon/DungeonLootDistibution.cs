using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hearthvale.GameCode.Dungeon
{
    /// <summary>
    /// Handles the distribution of loot throughout dungeon rooms
    /// </summary>
    public class DungeonLootDistribution
    {
        private readonly Random _random;

        public DungeonLootDistribution()
        {
            _random = new Random();
        }

        /// <summary>
        /// Distributes loot containers across rooms in the dungeon (0-1 per room).
        /// </summary>
        /// <param name="rooms">List of rooms in tile coordinates.</param>
        /// <param name="lootTableIds">Available loot table IDs to use.</param>
        /// <param name="trapChance">Chance for a loot container to be trapped (0.0-1.0).</param>
        /// <param name="trapIds">Available trap IDs to use for trapped containers.</param>
        /// <param name="roomLootChance">Chance a room gets loot (0.0-1.0).</param>
        /// <returns>List of created loot containers.</returns>
        public List<DungeonLoot> DistributeLoot(
            List<Rectangle> rooms,
            string[] lootTableIds,
            float trapChance = 0.25f,
            string[] trapIds = null,
            float roomLootChance = 0.6f)
        {
            var lootContainers = new List<DungeonLoot>();
            if (rooms == null || rooms.Count == 0 || lootTableIds == null || lootTableIds.Length == 0)
                return lootContainers;

            foreach (var room in rooms)
            {
                if (_random.NextDouble() <= roomLootChance)
                {
                    var (col, row) = GetValidPositionInRoom(room);

                    string lootTableId = lootTableIds[_random.Next(lootTableIds.Length)];

                    bool isTrapped = _random.NextDouble() <= trapChance;
                    string trapId = null;

                    if (isTrapped && trapIds != null && trapIds.Length > 0)
                    {
                        trapId = trapIds[_random.Next(trapIds.Length)];
                    }

                    // Unique ID to avoid collisions with map-defined elements
                    string id = $"loot_{Guid.NewGuid():N}";

                    lootContainers.Add(new DungeonLoot(id, lootTableId, col, row, isTrapped, trapId));
                }
            }

            return lootContainers;
        }

        private (int col, int row) GetValidPositionInRoom(Rectangle room)
        {
            int padding = 1;

            int minCol = room.Left + padding;
            int maxColExclusive = Math.Max(minCol + 1, room.Right - padding);

            int minRow = room.Top + padding;
            int maxRowExclusive = Math.Max(minRow + 1, room.Bottom - padding);

            int col = _random.Next(minCol, maxColExclusive);
            int row = _random.Next(minRow, maxRowExclusive);

            return (col, row);
        }
    }
}