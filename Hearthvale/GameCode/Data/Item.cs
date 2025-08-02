using System.Text.Json.Serialization;

public class Item
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Type { get; set; } // e.g., "Consumable", "Weapon", "Armor", "Key", "Misc"
    public int Value { get; set; } // Gold or currency value
    public string Rarity { get; set; } // e.g., "Common", "Uncommon", "Rare", "Epic"
    public string Description { get; set; }
    public int? Power { get; set; } // For weapons, potions, etc.
    public int? Defense { get; set; } // For armor
    public int? HealAmount { get; set; } // For consumables
    public string Effect { get; set; } // e.g., "RestoreMana", "UnlockDoor", "Fireball"
}