using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

/// <summary>
/// Represents a loot table for generating random items.
/// </summary>
public class LootTable
{
    public string Id { get; }
    public List<LootEntry> Entries { get; }

    public LootTable(string id)
    {
        Id = id;
        Entries = new List<LootEntry>();
    }

    /// <summary>
    /// Generates loot based on the table entries.
    /// </summary>
    /// <returns>List of generated loot items.</returns>
    public List<string> GenerateLoot()
    {
        var loot = new List<string>();
        var random = new System.Random();

        foreach (var entry in Entries)
        {
            if (random.NextDouble() <= entry.DropChance)
            {
                loot.Add(entry.ItemId);
            }
        }

        return loot;
    }
}

/// <summary>
/// Represents a single entry in a loot table.
/// </summary>
public class LootEntry
{
    public string ItemId { get; }
    public double DropChance { get; }
    public int MinQuantity { get; }
    public int MaxQuantity { get; }

    public LootEntry(string itemId, double dropChance, int minQuantity = 1, int maxQuantity = 1)
    {
        ItemId = itemId;
        DropChance = dropChance;
        MinQuantity = minQuantity;
        MaxQuantity = maxQuantity;
    }
}

/// <summary>
/// Interactive loot container that can be opened to reveal items.
/// </summary>
public class DungeonLoot : IDungeonElement
{
    public string Id { get; }
    public bool IsActive { get; private set; }
    public LootTable LootTable { get; }
    public int Column { get; }
    public int Row { get; }
    public bool IsOpened { get; private set; }
    public bool IsTrapped { get; }
    public string TrapId { get; }

    private List<string> _generatedLoot;

    /// <summary>
    /// Creates a new dungeon loot container.
    /// </summary>
    /// <param name="id">Unique identifier for the loot container.</param>
    /// <param name="lootTableId">ID of the loot table to use.</param>
    /// <param name="column">Column position in the tilemap.</param>
    /// <param name="row">Row position in the tilemap.</param>
    /// <param name="isTrapped">Whether the container is trapped.</param>
    /// <param name="trapId">ID of the associated trap (if trapped).</param>
    public DungeonLoot(string id, string lootTableId, int column = 0, int row = 0,
        bool isTrapped = false, string trapId = null)
    {
        Id = id;
        Column = column;
        Row = row;
        IsTrapped = isTrapped;
        TrapId = trapId;

        // TODO: Load LootTable from data source using lootTableId
        // For now, create a default empty table
        LootTable = new LootTable(lootTableId);
    }

    public void Activate()
    {
        if (!IsOpened)
        {
            IsOpened = true;
            IsActive = true;
            _generatedLoot = LootTable?.GenerateLoot() ?? new List<string>();

            // Trigger trap if container is trapped
            if (IsTrapped && !string.IsNullOrEmpty(TrapId))
            {
                var trap = DungeonManager.Instance.GetElement<DungeonTrap>(TrapId);
                trap?.Activate();
            }
        }
    }

    public void Deactivate()
    {
        // Loot containers typically don't deactivate once opened
    }

    public void Update(GameTime gameTime)
    {
        // Loot containers don't need continuous updates once opened
    }

    public void DrawDebug(SpriteBatch spriteBatch, Texture2D pixel)
    {
        var color = IsOpened ? Color.Gold : Color.Brown;
        if (IsTrapped && !IsOpened)
            color = Color.DarkRed;

        var rect = new Rectangle(Column * 32, Row * 32, 32, 32);
        spriteBatch.Draw(pixel, rect, color * 0.8f);
    }

    /// <summary>
    /// Attempts to open the loot container.
    /// </summary>
    /// <returns>List of items found in the container, or null if already opened.</returns>
    public List<string> TryOpen()
    {
        if (IsOpened)
            return null;

        Activate();
        return new List<string>(_generatedLoot);
    }

    /// <summary>
    /// Gets the generated loot items.
    /// </summary>
    /// <returns>List of loot item IDs.</returns>
    public List<string> GetLoot()
    {
        return IsOpened ? new List<string>(_generatedLoot) : new List<string>();
    }

    /// <summary>
    /// Checks if a position overlaps with this loot container.
    /// </summary>
    /// <param name="x">X position in world coordinates.</param>
    /// <param name="y">Y position in world coordinates.</param>
    /// <param name="tileSize">Size of a tile in pixels.</param>
    /// <returns>True if the position overlaps with this container.</returns>
    public bool IsPositionOnContainer(float x, float y, int tileSize = 32)
    {
        var containerX = Column * tileSize;
        var containerY = Row * tileSize;

        return x >= containerX && x < containerX + tileSize &&
               y >= containerY && y < containerY + tileSize;
    }
}