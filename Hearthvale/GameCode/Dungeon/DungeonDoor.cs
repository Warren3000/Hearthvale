using Microsoft.Xna.Framework;
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

    /// <summary>
    /// Event triggered when the door's state changes, indicating the tilemap should be updated.
    /// Parameters are: column, row, newTileId.
    /// </summary>
    public event Action<int, int, int> OnTileChanged;

    public DungeonDoor(string id, int column, int row, int lockedTileId, int unlockedTileId)
    {
        Id = id;
        Column = column;
        Row = row;
        LockedTileId = lockedTileId;
        UnlockedTileId = unlockedTileId;
        IsLocked = true; // Doors start locked by default
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
}