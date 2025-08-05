using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

/// <summary>
/// Types of switches available in dungeons.
/// </summary>
public enum SwitchType
{
    Toggle,      // Stays activated until toggled again
    Timed,       // Activates for a duration then resets
    Sequence     // Part of a sequence puzzle
}

/// <summary>
/// Interactive switch element that can activate other dungeon elements.
/// </summary>
public class DungeonSwitch : IActivatorElement
{
    public string Id { get; }
    public bool IsActive { get; private set; }
    public SwitchType Type { get; }
    public int Column { get; }
    public int Row { get; }
    public int InactiveTileId { get; }
    public int ActiveTileId { get; }
    public float Duration { get; }

    private float _activeTimer;
    private bool _canActivate = true;

    public event Action OnActivated;

    /// <summary>
    /// Creates a new dungeon switch.
    /// </summary>
    /// <param name="id">Unique identifier for the switch.</param>
    /// <param name="column">Column position in the tilemap.</param>
    /// <param name="row">Row position in the tilemap.</param>
    /// <param name="inactiveTileId">Tile ID when switch is inactive.</param>
    /// <param name="activeTileId">Tile ID when switch is active.</param>
    /// <param name="type">Type of switch behavior.</param>
    /// <param name="duration">Duration for timed switches (in seconds).</param>
    public DungeonSwitch(string id, int column, int row, int inactiveTileId, int activeTileId,
        SwitchType type = SwitchType.Toggle, float duration = 0f)
    {
        Id = id;
        Column = column;
        Row = row;
        InactiveTileId = inactiveTileId;
        ActiveTileId = activeTileId;
        Type = type;
        Duration = duration;
    }

    public void Activate()
    {
        if (!_canActivate) return;

        switch (Type)
        {
            case SwitchType.Toggle:
                IsActive = !IsActive;
                break;
            case SwitchType.Timed:
                if (!IsActive)
                {
                    IsActive = true;
                    _activeTimer = Duration;
                }
                break;
            case SwitchType.Sequence:
                IsActive = true;
                break;
        }

        if (IsActive)
        {
            OnActivated?.Invoke();
        }
    }

    public void Deactivate()
    {
        IsActive = false;
        _activeTimer = 0f;
    }

    public void Update(GameTime gameTime)
    {
        if (Type == SwitchType.Timed && IsActive)
        {
            _activeTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_activeTimer <= 0f)
            {
                IsActive = false;
            }
        }
    }

    public void DrawDebug(SpriteBatch spriteBatch, Texture2D pixel)
    {
        // Only draw if debug drawing is enabled
        if (!DebugManager.Instance.DebugDrawEnabled || !DebugManager.Instance.ShowDungeonElements)
            return;

        var color = IsActive ? Color.Green : Color.Red;
        var rect = new Rectangle(Column * 32, Row * 32, 32, 32);
        spriteBatch.Draw(pixel, rect, color * 0.5f);
    }

    /// <summary>
    /// Attempts to interact with the switch (e.g., player activation).
    /// </summary>
    /// <returns>True if the switch was successfully activated.</returns>
    public bool TryActivate()
    {
        if (_canActivate)
        {
            Activate();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Sets whether the switch can be activated.
    /// </summary>
    public void SetCanActivate(bool canActivate)
    {
        _canActivate = canActivate;
    }
}