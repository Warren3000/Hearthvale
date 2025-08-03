using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

public class DungeonPuzzle : IDungeonElement
{
    public string Id { get; }
    public bool IsActive { get; private set; }
    public PuzzleType Type { get; }
    public bool IsSolved { get; private set; }

    public DungeonPuzzle(string id, PuzzleType type)
    {
        Id = id;
        Type = type;
    }

    public void Activate() { /* Start puzzle */ }
    public void Deactivate() { /* End puzzle */ }
    public void Update(GameTime gameTime) { /* Puzzle logic */ }

    public void DrawDebug(SpriteBatch spriteBatch, Texture2D pixel)
    {
        // Example debug draw: draw a small colored rectangle if active
        if (IsActive)
        {
            // Draw a 32x32 rectangle at a logical position (replace with actual position if available)
            spriteBatch.Draw(pixel, new Rectangle(0, 0, 32, 32), Color.Yellow * 0.5f);
        }
    }
}

public enum PuzzleType
{
    BlockPush,
    LightMirror,
    Sequence,
    Riddle
}