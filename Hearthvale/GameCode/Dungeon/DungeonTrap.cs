using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

/// <summary>
/// Types of traps available in dungeons.
/// </summary>
public enum TrapType
{
    Spikes,      // Damages entities in the area
    Arrows,      // Fires projectiles
    Pitfall,     // Causes falling damage
    PoisonGas,   // Applies poison status
    Fire,        // Causes fire damage over time
    Freeze       // Slows or freezes entities
}

/// <summary>
/// Trap state enumeration.
/// </summary>
public enum TrapState
{
    Inactive,    // Trap is not active
    Armed,       // Trap is ready to trigger
    Triggered,   // Trap is currently active
    Cooldown,    // Trap is cooling down before it can be triggered again
    Disabled     // Trap has been disabled
}

/// <summary>
/// Interactive trap element that can damage or affect entities.
/// </summary>
public class DungeonTrap : IDungeonElement
{
    public string Id { get; }
    public bool IsActive => State == TrapState.Triggered;
    public TrapType Type { get; }
    public TrapState State { get; private set; }
    public float Damage { get; }
    public float CooldownTime { get; }
    public int Column { get; }
    public int Row { get; }

    private float _cooldownTimer;
    private float _activeTimer;
    private const float DefaultActiveTime = 2f;

    /// <summary>
    /// Creates a new dungeon trap.
    /// </summary>
    /// <param name="id">Unique identifier for the trap.</param>
    /// <param name="type">Type of trap.</param>
    /// <param name="damage">Damage dealt by the trap.</param>
    /// <param name="cooldownTime">Time between trap activations.</param>
    /// <param name="column">Column position in the tilemap.</param>
    /// <param name="row">Row position in the tilemap.</param>
    public DungeonTrap(string id, TrapType type, float damage = 10f, float cooldownTime = 2f,
        int column = 0, int row = 0)
    {
        Id = id;
        Type = type;
        Damage = damage;
        CooldownTime = cooldownTime;
        Column = column;
        Row = row;
        State = TrapState.Armed;
    }

    public void Activate()
    {
        if (State == TrapState.Armed)
        {
            State = TrapState.Triggered;
            _activeTimer = DefaultActiveTime;
            TriggerTrap();
        }
    }

    public void Deactivate()
    {
        if (State == TrapState.Triggered)
        {
            State = TrapState.Cooldown;
            _cooldownTimer = CooldownTime;
        }
    }

    public void Update(GameTime gameTime)
    {
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

        switch (State)
        {
            case TrapState.Triggered:
                _activeTimer -= deltaTime;
                if (_activeTimer <= 0f)
                {
                    Deactivate();
                }
                break;

            case TrapState.Cooldown:
                _cooldownTimer -= deltaTime;
                if (_cooldownTimer <= 0f)
                {
                    State = TrapState.Armed;
                }
                break;
        }
    }

    public void DrawDebug(SpriteBatch spriteBatch, Texture2D pixel)
    {
        Color color = State switch
        {
            TrapState.Armed => Color.Red,
            TrapState.Triggered => Color.Orange,
            TrapState.Cooldown => Color.Yellow,
            TrapState.Disabled => Color.Gray,
            _ => Color.DarkRed
        };

        var rect = new Rectangle(Column * 32, Row * 32, 32, 32);
        spriteBatch.Draw(pixel, rect, color * 0.7f);
    }

    /// <summary>
    /// Triggers the trap effect based on its type.
    /// </summary>
    private void TriggerTrap()
    {
        switch (Type)
        {
            case TrapType.Spikes:
                // TODO: Deal damage to entities in area
                break;
            case TrapType.Arrows:
                // TODO: Fire projectiles
                break;
            case TrapType.Pitfall:
                // TODO: Create pit and deal falling damage
                break;
            case TrapType.PoisonGas:
                // TODO: Apply poison status effect
                break;
            case TrapType.Fire:
                // TODO: Create fire damage area
                break;
            case TrapType.Freeze:
                // TODO: Apply freeze status effect
                break;
        }
    }

    /// <summary>
    /// Disables the trap permanently.
    /// </summary>
    public void DisableTrap()
    {
        State = TrapState.Disabled;
    }

    /// <summary>
    /// Resets the trap to its initial armed state.
    /// </summary>
    public void ResetTrap()
    {
        State = TrapState.Armed;
        _cooldownTimer = 0f;
        _activeTimer = 0f;
    }

    /// <summary>
    /// Checks if an entity at the given position would trigger this trap.
    /// </summary>
    /// <param name="x">X position in world coordinates.</param>
    /// <param name="y">Y position in world coordinates.</param>
    /// <param name="tileSize">Size of a tile in pixels.</param>
    /// <returns>True if the position overlaps with this trap.</returns>
    public bool IsPositionInTrap(float x, float y, int tileSize = 32)
    {
        var trapX = Column * tileSize;
        var trapY = Row * tileSize;

        return x >= trapX && x < trapX + tileSize &&
               y >= trapY && y < trapY + tileSize;
    }

    /// <summary>
    /// Gets the trap's effect area as a rectangle.
    /// </summary>
    /// <param name="tileSize">Size of a tile in pixels.</param>
    /// <returns>Rectangle representing the trap's area.</returns>
    public Rectangle GetEffectArea(int tileSize = 32)
    {
        return new Rectangle(Column * tileSize, Row * tileSize, tileSize, tileSize);
    }
}