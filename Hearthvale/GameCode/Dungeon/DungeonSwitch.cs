using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

public class DungeonSwitch : IActivatorElement, IDungeonElement
{
    public string Id { get; }
    public bool IsActive { get; private set; }

    // Tilemap properties
    public int Column { get; }
    public int Row { get; }
    public int InactiveTileId { get; }
    public int ActiveTileId { get; }

    /// <summary>
    /// Event triggered when the switch is activated.
    /// </summary>
    public event Action OnActivated;

    /// <summary>
    /// Event triggered when the switch's state changes, indicating the tilemap should be updated.
    /// Parameters are: column, row, newTileId.
    /// </summary>
    public event Action<int, int, int> OnTileChanged;

    public DungeonSwitch(string id, int column, int row, int inactiveTileId, int activeTileId)
    {
        Id = id;
        Column = column;
        Row = row;
        InactiveTileId = inactiveTileId;
        ActiveTileId = activeTileId;
        IsActive = false; // Switches start inactive by default
    }

    public void Activate()
    {
        if (!IsActive)
        {
            IsActive = true;
            OnActivated?.Invoke();
            OnTileChanged?.Invoke(Column, Row, ActiveTileId);
        }
    }

    public void Deactivate()
    {
        if (IsActive)
        {
            IsActive = false;
            OnTileChanged?.Invoke(Column, Row, InactiveTileId);
        }
    }

    public void Update(GameTime gameTime) { /* Switch logic */ }

    public void DrawDebug(SpriteBatch spriteBatch, Texture2D pixel)
    {
        // Example debug drawing: draw a colored rectangle at the switch's tile position
        Color color = IsActive ? Color.LimeGreen : Color.Red;
        int tileSize = 32; // Adjust as needed for your tile size
        Rectangle rect = new Rectangle(Column * tileSize, Row * tileSize, tileSize, tileSize);
        spriteBatch.Draw(pixel, rect, color * 0.5f);
    }
}