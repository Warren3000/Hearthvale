using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Hearthvale.GameCode.Entities;
using Microsoft.Xna.Framework.Graphics;

public class DungeonEncounter : IDungeonElement
{
    public string Id { get; }
    public bool IsActive { get; private set; }
    public List<Enemy> Enemies { get; } = new();

    public DungeonEncounter(string id)
    {
        Id = id;
    }

    public void Activate() { /* Spawn enemies */ }
    public void Deactivate() { /* End encounter */ }
    public void Update(GameTime gameTime) { /* Encounter logic */ }

    public void DrawDebug(SpriteBatch spriteBatch, Texture2D pixel)
    {
        // Implement debug drawing logic here if needed
    }
}