using Microsoft.Xna.Framework;

public class DungeonLoot : IDungeonElement
{
    public string Id { get; }
    public bool IsActive { get; private set; }
    public LootTable LootTable { get; }

    public void Activate() { /* Reveal loot */ }
    public void Deactivate() { /* Hide loot */ }
    public void Update(GameTime gameTime) { /* Loot logic */ }
}