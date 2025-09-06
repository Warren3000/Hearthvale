using Hearthvale.GameCode.Managers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

/// <summary>
/// Types of levers available in dungeons.
/// </summary>
public enum LeverType
{
    SingleUse,  // Can only be pulled once
    Reusable,   // Can be pulled multiple times
    Combo       // Part of a combination puzzle
}

/// <summary>
/// Interactive lever element that can activate other dungeon elements.
/// </summary>
public class DungeonLever : IActivatorElement
{
    public string Id { get; }
    public bool IsActive { get; private set; }
    public LeverType Type { get; }
    public int Column { get; }
    public int Row { get; }
    public int InactiveTileId { get; }
    public int ActiveTileId { get; }

    private bool _hasBeenUsed;

    public event Action OnActivated;

    /// <summary>
    /// Creates a new dungeon lever.
    /// </summary>
    /// <param name="id">Unique identifier for the lever.</param>
    /// <param name="column">Column position in the tilemap.</param>
    /// <param name="row">Row position in the tilemap.</param>
    /// <param name="inactiveTileId">Tile ID when lever is inactive.</param>
    /// <param name="activeTileId">Tile ID when lever is active.</param>
    /// <param name="type">Type of lever behavior.</param>
    public DungeonLever(string id, int column, int row, int inactiveTileId, int activeTileId,
        LeverType type = LeverType.Reusable)
    {
        Id = id;
        Column = column;
        Row = row;
        InactiveTileId = inactiveTileId;
        ActiveTileId = activeTileId;
        Type = type;
    }

    public void Activate()
    {
        if (Type == LeverType.SingleUse && _hasBeenUsed)
            return;

        IsActive = !IsActive;
        _hasBeenUsed = true;

        if (IsActive)
        {
            OnActivated?.Invoke();
        }
    }

    public void Deactivate()
    {
        if (Type != LeverType.SingleUse)
        {
            IsActive = false;
        }
    }

    public void Update(GameTime gameTime)
    {
        // Levers don't need continuous updates
    }

    public void DrawDebug(SpriteBatch spriteBatch, Texture2D pixel)
    {
        // Only draw if debug drawing is enabled
        if (!DebugManager.Instance.DebugDrawEnabled || !DebugManager.Instance.ShowDungeonElements)
            return;

        var color = IsActive ? Color.Orange : Color.Gray;
        if (Type == LeverType.SingleUse && _hasBeenUsed)
            color = Color.DarkGray;

        var rect = new Rectangle(Column * 32, Row * 32, 32, 32);
        spriteBatch.Draw(pixel, rect, color * 0.6f);
    }

    /// <summary>
    /// Attempts to pull the lever.
    /// </summary>
    /// <returns>True if the lever was successfully activated.</returns>
    public bool TryPull()
    {
        if (Type == LeverType.SingleUse && _hasBeenUsed)
            return false;

        Activate();
        return true;
    }

    /// <summary>
    /// Resets the lever to its initial state (for reusable levers).
    /// </summary>
    public void Reset()
    {
        if (Type != LeverType.SingleUse)
        {
            IsActive = false;
            _hasBeenUsed = false;
        }
    }
}