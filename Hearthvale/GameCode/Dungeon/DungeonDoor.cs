using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

public class DungeonDoor : IDungeonElement
{
    public string Id { get; }
    public bool IsLocked { get; private set; }
    public bool IsActive => !IsLocked;

    // Tilemap properties
    public int Column { get; }
    public int Row { get; }
    public int LockedTileId { get; }
    public int UnlockedTileId { get; }

    // Add this property:
    public Rectangle Bounds { get; }

    /// <summary>
    /// Event triggered when the door's state changes, indicating the tilemap should be updated.
    /// Parameters are: column, row, newTileId.
    /// </summary>
    public event Action<int, int, int> OnTileChanged;

    public DungeonDoor(string id, int column, int row, int lockedTileId, int unlockedTileId, float tileWidth = 32, float tileHeight = 32)
    {
        Id = id;
        Column = column;
        Row = row;
        LockedTileId = lockedTileId;
        UnlockedTileId = unlockedTileId;
        IsLocked = true;
        Bounds = new Rectangle((int)(Column * tileWidth), (int)(Row * tileHeight), (int)tileWidth, (int)tileHeight);
    }

    public void Activate()
    {
        if (IsLocked)
        {
            IsLocked = false;
            OnTileChanged?.Invoke(Column, Row, UnlockedTileId);
        }
    }

    public void Deactivate()
    {
        if (!IsLocked)
        {
            IsLocked = true;
            OnTileChanged?.Invoke(Column, Row, LockedTileId);
        }
    }

    public void Update(GameTime gameTime) { /* Door animation/logic */ }

    // Add this method to implement the missing interface member
    public void DrawDebug(SpriteBatch spriteBatch, Texture2D pixel)
    {
        // Draw the Bounds rectangle using the provided pixel texture
        var color = IsLocked ? Color.Red : Color.Green;
        var rect = Bounds;

        // Top
        spriteBatch.Draw(pixel, new Rectangle(rect.Left, rect.Top, rect.Width, 1), color);
        // Left
        spriteBatch.Draw(pixel, new Rectangle(rect.Left, rect.Top, 1, rect.Height), color);
        // Right
        spriteBatch.Draw(pixel, new Rectangle(rect.Right - 1, rect.Top, 1, rect.Height), color);
        // Bottom
        spriteBatch.Draw(pixel, new Rectangle(rect.Left, rect.Bottom - 1, rect.Width, 1), color);
    }
}