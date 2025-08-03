using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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

    public void DrawDebug(SpriteBatch spriteBatch, Texture2D pixel)
    {
        // Example debug draw: draw a simple colored rectangle
        // You may want to adjust position/size as needed for your game
        var color = IsActive ? Color.Red : Color.Gray;
        spriteBatch.Draw(pixel, new Rectangle(0, 0, 32, 32), color);
    }
}

public enum TrapType
{
    Spikes,
    Arrows,
    Pitfall,
    PoisonGas
}