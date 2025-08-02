using System.IO;
using System.Text.Json;

public static class InventorySerializer
{
    public static PlayerInventory LoadInventory(string filePath)
    {
        if (!File.Exists(filePath))
            return new PlayerInventory(); // Return empty if no save exists

        var json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<PlayerInventory>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }
}