using Hearthvale.GameCode.Managers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

/// <summary>
/// Types of pressure plates available in dungeons.
/// </summary>
public enum PressurePlateType
{
    Momentary,  // Active only while weight is on it
    Latch,      // Stays active after being triggered
    Combo       // Part of a combination puzzle
}

/// <summary>
/// Interactive pressure plate element that activates when stepped on.
/// </summary>
public class DungeonPressurePlate : IActivatorElement
{
    public string Id { get; }
    public bool IsActive { get; private set; }
    public PressurePlateType Type { get; }
    public int Column { get; }
    public int Row { get; }
    public int InactiveTileId { get; }
    public int ActiveTileId { get; }
    public float WeightRequired { get; }

    private float _currentWeight;
    private bool _wasTriggered;

    public event Action OnActivated;

    /// <summary>
    /// Creates a new dungeon pressure plate.
    /// </summary>
    /// <param name="id">Unique identifier for the pressure plate.</param>
    /// <param name="column">Column position in the tilemap.</param>
    /// <param name="row">Row position in the tilemap.</param>
    /// <param name="inactiveTileId">Tile ID when plate is inactive.</param>
    /// <param name="activeTileId">Tile ID when plate is active.</param>
    /// <param name="type">Type of pressure plate behavior.</param>
    /// <param name="weightRequired">Minimum weight required to activate.</param>
    public DungeonPressurePlate(string id, int column, int row, int inactiveTileId, int activeTileId,
        PressurePlateType type = PressurePlateType.Momentary, float weightRequired = 1f)
    {
        Id = id;
        Column = column;
        Row = row;
        InactiveTileId = inactiveTileId;
        ActiveTileId = activeTileId;
        Type = type;
        WeightRequired = weightRequired;
    }

    public void Activate()
    {
        if (!IsActive && _currentWeight >= WeightRequired)
        {
            IsActive = true;
            _wasTriggered = true;
            OnActivated?.Invoke();
        }
    }

    public void Deactivate()
    {
        if (Type == PressurePlateType.Momentary)
        {
            IsActive = false;
        }
    }

    public void Update(GameTime gameTime)
    {
        bool shouldBeActive = _currentWeight >= WeightRequired;

        switch (Type)
        {
            case PressurePlateType.Momentary:
                if (shouldBeActive && !IsActive)
                {
                    Activate();
                }
                else if (!shouldBeActive && IsActive)
                {
                    Deactivate();
                }
                break;

            case PressurePlateType.Latch:
                if (shouldBeActive && !_wasTriggered)
                {
                    Activate();
                }
                break;

            case PressurePlateType.Combo:
                if (shouldBeActive && !IsActive)
                {
                    Activate();
                }
                break;
        }
    }

    public void DrawDebug(SpriteBatch spriteBatch, Texture2D pixel)
    {
        var color = IsActive ? Color.Purple : Color.DarkBlue;
        var rect = new Rectangle(Column * 32, Row * 32, 32, 32);
        spriteBatch.Draw(pixel, rect, color * 0.6f);
    }

    /// <summary>
    /// Sets the current weight on the pressure plate.
    /// </summary>
    /// <param name="weight">The weight currently on the plate.</param>
    public void SetWeight(float weight)
    {
        _currentWeight = weight;
    }

    /// <summary>
    /// Adds weight to the pressure plate.
    /// </summary>
    /// <param name="weight">The weight to add.</param>
    public void AddWeight(float weight)
    {
        _currentWeight += weight;
    }

    /// <summary>
    /// Removes weight from the pressure plate.
    /// </summary>
    /// <param name="weight">The weight to remove.</param>
    public void RemoveWeight(float weight)
    {
        _currentWeight = Math.Max(0, _currentWeight - weight);
    }

    /// <summary>
    /// Checks if an entity at the given position would activate this plate.
    /// </summary>
    /// <param name="x">X position in world coordinates.</param>
    /// <param name="y">Y position in world coordinates.</param>
    /// <param name="tileSize">Size of a tile in pixels.</param>
    /// <returns>True if the position overlaps with this plate.</returns>
    public bool IsPositionOnPlate(float x, float y, int tileSize = 32)
    {
        var plateX = Column * tileSize;
        var plateY = Row * tileSize;

        return x >= plateX && x < plateX + tileSize &&
               y >= plateY && y < plateY + tileSize;
    }

    /// <summary>
    /// Resets the pressure plate to its initial state.
    /// </summary>
    public void Reset()
    {
        IsActive = false;
        _currentWeight = 0f;
        _wasTriggered = false;
    }
}