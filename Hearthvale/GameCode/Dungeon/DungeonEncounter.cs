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
        // Only draw if debug drawing is enabled
        if (!DebugManager.Instance.DebugDrawEnabled || !DebugManager.Instance.ShowDungeonElements)
            return;

        // Draw a placeholder rectangle for the encounter area
        var color = IsActive ? Color.Purple : Color.DarkOrchid;
        var rect = new Rectangle(0, 0, 64, 64); // Placeholder size
        spriteBatch.Draw(pixel, rect, color * 0.4f);
    }
}