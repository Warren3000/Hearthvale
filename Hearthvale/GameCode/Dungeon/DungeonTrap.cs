using Microsoft.Xna.Framework;
using System;

public class DungeonTrap : IDungeonElement
{
    public string Id { get; }
    public bool IsActive { get; private set; }
    public TrapType Type { get; }

    public DungeonTrap(string id, TrapType type)
    {
        Id = id;
        Type = type;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;

    public void Update(GameTime gameTime) { /* Trap logic */ }
}

public enum TrapType
{
    Spikes,
    Arrows,
    Pitfall,
    PoisonGas
}