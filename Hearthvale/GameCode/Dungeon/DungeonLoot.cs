using Microsoft.Xna.Framework;

public class DungeonLoot : IDungeonElement
{
    public string Id { get; }
    public bool IsActive { get; private set; }
    public LootTable LootTable { get; }

    public DungeonLoot(string id, string lootTableId)
    {
        Id = id;
        // In a real implementation, you would load the LootTable from a data source
        // using lootTableId. For now, we'll leave it null.
        LootTable = null;
    }

    public void Activate() { /* Reveal loot */ }
    public void Deactivate() { /* Hide loot */ }
    public void Update(GameTime gameTime) { /* Loot logic */ }
}