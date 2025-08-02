using System.Collections.Generic;
using Hearthvale.GameCode.Data;

public class LootTable
{
    public List<Item> Items { get; set; } = new();
    // You can expand with item rarity, weights, etc.
}