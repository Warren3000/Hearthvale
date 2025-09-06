using Hearthvale.GameCode.Dungeon;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using MonoGame.Extended.Tiled;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hearthvale.GameCode.Managers.Dungeon
{
    /// <summary>
    /// Adds auto-loot placement (0-1 per room) after loading a dungeon.
    /// Rooms are read from a Tiled object layer named "Rooms" (rectangle objects).
    /// </summary>
    public class AutoLootDungeonManager : DungeonManager
    {
        private readonly DungeonLootDistribution _distribution = new();
        private readonly string[] _lootTableIds;
        private readonly float _roomLootChance;
        private readonly float _trapChance;
        private readonly string[] _trapIds;

        public AutoLootDungeonManager(
            string[] lootTableIds,
            float roomLootChance = 0.6f,
            float trapChance = 0.25f,
            string[] trapIds = null)
        {
            _lootTableIds = lootTableIds ?? throw new ArgumentNullException(nameof(lootTableIds));
            if (_lootTableIds.Length == 0)
                throw new ArgumentException("At least one loot table ID is required.", nameof(lootTableIds));

            _roomLootChance = Math.Clamp(roomLootChance, 0f, 1f);
            _trapChance = Math.Clamp(trapChance, 0f, 1f);
            _trapIds = trapIds;
        }

        public override TiledMap LoadDungeonFromFile(ContentManager content, string filename)
        {
            var map = base.LoadDungeonFromFile(content, filename);

            var rooms = ExtractRooms(map);
            if (rooms.Count == 0)
                return map;

            var generated = _distribution.DistributeLoot(
                rooms: rooms,
                lootTableIds: _lootTableIds,
                trapChance: _trapChance,
                trapIds: _trapIds,
                roomLootChance: _roomLootChance);

            // Ensure IDs are unique vs existing elements
            var existingIds = new HashSet<string>(GetAllElements().Select(e => e.Id), StringComparer.OrdinalIgnoreCase);
            foreach (var loot in generated)
            {
                if (existingIds.Contains(loot.Id))
                {
                    var uniqueId = $"loot_{Guid.NewGuid():N}";
                    AddElement(new DungeonLoot(uniqueId, loot.LootTable.Id, loot.Column, loot.Row, loot.IsTrapped, loot.TrapId));
                    existingIds.Add(uniqueId);
                }
                else
                {
                    AddElement(loot);
                    existingIds.Add(loot.Id);
                }
            }

            return map;
        }

        private static List<Rectangle> ExtractRooms(TiledMap map)
        {
            var rooms = new List<Rectangle>();
            if (map == null) return rooms;

            // Expect a Tiled object layer named "Rooms" with rectangle objects
            foreach (var objectLayer in map.ObjectLayers)
            {
                if (!objectLayer.Name.Equals("Rooms", StringComparison.OrdinalIgnoreCase))
                    continue;

                foreach (var obj in objectLayer.Objects)
                {
                    if (obj == null) continue;

                    int tileW = map.TileWidth;
                    int tileH = map.TileHeight;

                    int col = (int)Math.Floor(obj.Position.X / tileW);
                    int row = (int)Math.Floor(obj.Position.Y / tileH);
                    int width = (int)Math.Ceiling(obj.Size.Width / tileW);
                    int height = (int)Math.Ceiling(obj.Size.Height / tileH);

                    if (width > 0 && height > 0)
                        rooms.Add(new Rectangle(col, row, width, height));
                }
            }

            return rooms;
        }
    }
}