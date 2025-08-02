using System.Collections.Generic;
using Hearthvale.GameCode.Data;

public class PlayerInventory
{
    public List<Item> Items { get; set; } = new();

    public Item? EquippedWeapon { get; set; }
    public Item? EquippedArmor { get; set; }
    public Item? EquippedAccessory { get; set; }
}