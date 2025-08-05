using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

/// <summary>
/// Types of doors available in dungeons.
/// </summary>
public enum DoorType
{
    Normal,         // Opens/closes normally
    Locked,         // Requires a key
    PressurePlate,  // Activated by pressure plates
    Timed,          // Closes after a duration
    OneWay          // Can only be opened from one side
}

/// <summary>
/// Interactive door element that can block or allow passage.
/// </summary>
public class DungeonDoor : IDungeonElement
{
    public string Id { get; }
    public bool IsActive { get; private set; }
    public DoorType Type { get; }
    public int Column { get; }
    public int Row { get; }
    public int LockedTileId { get; }
    public int UnlockedTileId { get; }
    public string KeyRequired { get; }
    public bool IsOpen { get; private set; }

    private float _closeTimer;
    private const float DefaultCloseTime = 5f;

    /// <summary>
    /// Creates a new dungeon door.
    /// </summary>
    /// <param name="id">Unique identifier for the door.</param>
    /// <param name="column">Column position in the tilemap.</param>
    /// <param name="row">Row position in the tilemap.</param>
    /// <param name="lockedTileId">Tile ID when door is closed/locked.</param>
    /// <param name="unlockedTileId">Tile ID when door is open.</param>
    /// <param name="type">Type of door behavior.</param>
    /// <param name="keyRequired">Key item required to unlock (if applicable).</param>
    public DungeonDoor(string id, int column, int row, int lockedTileId, int unlockedTileId,
        DoorType type = DoorType.Normal, string keyRequired = null)
    {
        Id = id;
        Column = column;
        Row = row;
        LockedTileId = lockedTileId;
        UnlockedTileId = unlockedTileId;
        Type = type;
        KeyRequired = keyRequired;
    }

    public void Activate()
    {
        switch (Type)
        {
            case DoorType.Normal:
            case DoorType.PressurePlate:
                IsOpen = !IsOpen;
                break;
            case DoorType.Timed:
                if (!IsOpen)
                {
                    IsOpen = true;
                    _closeTimer = DefaultCloseTime;
                }
                break;
            case DoorType.Locked:
                // TODO: Check if player has required key
                break;
            case DoorType.OneWay:
                // TODO: Check direction and allow opening
                break;
        }

        IsActive = IsOpen;
    }

    public void Deactivate()
    {
        IsOpen = false;
        IsActive = false;
        _closeTimer = 0f;
    }

    public void Update(GameTime gameTime)
    {
        if (Type == DoorType.Timed && IsOpen)
        {
            _closeTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_closeTimer <= 0f)
            {
                IsOpen = false;
                IsActive = false;
            }
        }
    }

    public void DrawDebug(SpriteBatch spriteBatch, Texture2D pixel)
    {
        var color = IsOpen ? Color.Green : Color.Brown;
        var rect = new Rectangle(Column * 32, Row * 32, 32, 32);
        spriteBatch.Draw(pixel, rect, color * 0.7f);
    }

    /// <summary>
    /// Attempts to open the door with the specified key.
    /// </summary>
    /// <param name="keyId">The key item ID.</param>
    /// <returns>True if the door was successfully opened.</returns>
    public bool TryUnlock(string keyId)
    {
        if (Type == DoorType.Locked && KeyRequired == keyId)
        {
            IsOpen = true;
            IsActive = true;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Checks if the door blocks movement.
    /// </summary>
    /// <returns>True if the door blocks movement.</returns>
    public bool BlocksMovement()
    {
        return !IsOpen;
    }
}