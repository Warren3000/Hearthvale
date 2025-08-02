using System.IO;
using System.Text.Json;

public static class InventorySerializer
{
    public static void SaveInventory(PlayerInventory inventory, string filePath)
    {
        var json = JsonSerializer.Serialize(inventory, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(filePath, json);
    }

    public static PlayerInventory LoadInventory(string filePath)
    {
        if (!File.Exists(filePath))
            return new PlayerInventory();

        var json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<PlayerInventory>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }
}