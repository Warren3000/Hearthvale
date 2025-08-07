using Hearthvale.GameCode.Entities.Interfaces;
using Hearthvale.GameCode.Entities.Players;
using Hearthvale.GameCode.Managers;
using Hearthvale.GameCode.Utils;
using Hearthvale.Scenes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Tiled;
using MonoGameLibrary.Graphics;
using System;
using System.Collections.Generic;

namespace Hearthvale.GameCode.Entities.Characters;

public abstract class Character : IDamageable, IMovable, IAnimatable, IDialog
{
    protected AnimatedSprite _sprite;
    protected Vector2 _position;
    protected bool _facingRight = true;
    protected string _currentAnimationName;
    protected int _maxHealth;
    protected int _currentHealth;
    public string DialogText { get; set; } = "Hello, adventurer!";
    public virtual AnimatedSprite Sprite => _sprite;
    public virtual Vector2 Position => _position;
    public virtual int Health => _currentHealth;
    public virtual int MaxHealth => _maxHealth;
    public virtual bool IsDefeated => _currentHealth <= 0;
    public bool FacingRight
    {
        get => _facingRight;
        set => _facingRight = value;
    }
    public string CurrentAnimationName
    {
        get => _currentAnimationName;
        set
        {
            if (_sprite != null)
            {
                _currentAnimationName = value;
                // If you have a way to get the Animation object, set it here
                // For example, if you have an atlas:
                // _sprite.Animation = _atlas.GetAnimation(value);
            }
        }
    }
    public virtual bool TakeDamage(int amount, Vector2? knockback = null)
    {
        bool wasDefeated = IsDefeated;
        _currentHealth = Math.Max(0, _currentHealth - amount);
        Flash();
        return !wasDefeated && IsDefeated;
    }

    public virtual void Heal(int amount)
    {
        _currentHealth = Math.Min(_maxHealth, _currentHealth + amount);
    }

    public virtual void SetPosition(Vector2 pos)
    {
        _position = pos;
        _sprite.Position = pos;
    }
    public void ClampToBounds(Rectangle bounds)
    {
        float clampedX = MathHelper.Clamp(Position.X, bounds.Left, bounds.Right - Sprite.Width);
        float clampedY = MathHelper.Clamp(Position.Y, bounds.Top, bounds.Bottom - Sprite.Height);
        SetPosition(new Vector2(clampedX, clampedY));
    }
    public abstract void Flash();

    public virtual Rectangle Bounds => new Rectangle(
        (int)_position.X + 8,
        (int)_position.Y + 16,
        (int)_sprite.Width / 2,
        (int)_sprite.Height / 2
    );

    public Weapon EquippedWeapon { get; private set; }

    public virtual void EquipWeapon(Weapon weapon)
    {
        if (weapon == null) return;
        EquippedWeapon = weapon;
    }

    /// <summary>
    /// Gets the direction to use for attack area calculation. Override in subclasses for custom logic.
    /// </summary>
    protected virtual Vector2 GetAttackDirection()
    {
        return _facingRight ? Vector2.UnitX : -Vector2.UnitX;
    }

    /// <summary>
    /// Gets the attack area rectangle for this character.
    /// </summary>
    public virtual Rectangle GetAttackArea()
    {
        if (EquippedWeapon == null) return Rectangle.Empty;

        Vector2 origin = Position + new Vector2(Sprite.Width / 2, Sprite.Height / 2);

        // Add the visual offset to match the sprite's rotation
        const float visualRotationOffset = MathHelper.PiOver4;
        float totalRotation = EquippedWeapon.Rotation + visualRotationOffset;
        Vector2 direction = new Vector2((float)Math.Cos(totalRotation), (float)Math.Sin(totalRotation));

        float handleOffset = 8f; // Adjust this value to match your weapon's handle length
        float length = EquippedWeapon.Length - handleOffset;
        float thickness = 12f;

        Vector2 perp = new Vector2(-direction.Y, direction.X) * (thickness / 2);

        Vector2 p1 = origin + perp;
        Vector2 p2 = origin - perp;
        Vector2 p3 = origin + direction * length + perp;
        Vector2 p4 = origin + direction * length - perp;

        float minX = MathF.Min(MathF.Min(p1.X, p2.X), MathF.Min(p3.X, p4.X));
        float maxX = MathF.Max(MathF.Max(p1.X, p2.X), MathF.Max(p3.X, p4.X));
        float minY = MathF.Min(MathF.Min(p1.Y, p2.Y), MathF.Min(p3.Y, p4.Y));
        float maxY = MathF.Max(MathF.Max(p1.Y, p2.Y), MathF.Max(p3.Y, p4.Y));

        return new Rectangle((int)minX, (int)minY, (int)(maxX - minX), (int)(maxY - minY));
    }

    /// <summary>
    /// Determines if the weapon should be drawn behind the character. Override in subclasses for custom logic.
    /// </summary>
    protected virtual bool ShouldDrawWeaponBehind()
    {
        return false;
    }

    public virtual void Draw(SpriteBatch spriteBatch)
    {
        if (_sprite != null)
        {
            Color originalColor = _sprite.Color;
            float alpha = 1f;
            if (IsDefeated)
            {
                alpha = 0.5f;
            }
            _sprite.Color = Color.White * alpha;

            bool drawWeaponBehind = ShouldDrawWeaponBehind();
            // Calculate the center of the character to pass to the weapon
            Vector2 characterCenter = Position + new Vector2(Sprite.Width / 2f, Sprite.Height / 1.4f);

            if (drawWeaponBehind)
            {
                EquippedWeapon?.Draw(spriteBatch, characterCenter);
                _sprite.Draw(spriteBatch, Position);
            }
            else
            {
                _sprite.Draw(spriteBatch, Position);
                EquippedWeapon?.Draw(spriteBatch, characterCenter);
            }
            _sprite.Color = originalColor;
        }
        else
        {
            // If the character sprite is null, still try to draw the weapon
            // We don't have a character position, so we can't calculate a center.
            // The weapon will draw at its last known position.
            EquippedWeapon?.Draw(spriteBatch, Vector2.Zero);
        }
    }

    public virtual void DrawDebug(SpriteBatch spriteBatch, Texture2D pixel)
    {
        // Draw bounds in green for player, red for NPC (override as needed)
        Color color = this is Player ? Color.LimeGreen * 0.5f : Color.Red * 0.5f;
        var rect = Bounds;
        // Top
        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, 1), color);
        // Left
        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, 1, rect.Height), color);
        // Right
        spriteBatch.Draw(pixel, new Rectangle(rect.Right - 1, rect.Y, 1, rect.Height), color);
        // Bottom
        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Bottom - 1, rect.Width, 1), color);
    }

    // Knockback fields
    public Vector2 _knockbackVelocity;
    protected float _knockbackTimer;
    protected const float KnockbackDuration = 0.2f;
    protected const float BounceDamping = 0.5f;

    // For wall collision
    public Tilemap Tilemap { get; set; }

    public bool IsKnockedBack => _knockbackTimer > 0;

    public virtual void SetKnockback(Vector2 velocity)
    {
        _knockbackVelocity = velocity;
        _knockbackTimer = KnockbackDuration;
    }

    /// <summary>
    /// Call this from your Update method in Player/NPC.
    /// </summary>
    public void UpdateKnockback(GameTime gameTime)
    {
        if (_knockbackTimer > 0)
        {
            float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _knockbackTimer -= elapsed;
            Vector2 nextPosition = Position + _knockbackVelocity * elapsed;

            // Add NaN protection
            if (float.IsNaN(nextPosition.X) || float.IsNaN(nextPosition.Y))
            {
                System.Diagnostics.Debug.WriteLine($"❌ KNOCKBACK NaN: Position={Position}, velocity={_knockbackVelocity}, elapsed={elapsed}");
                _knockbackVelocity = Vector2.Zero;
                _knockbackTimer = 0;
                return;
            }

            // Use wall sliding for knockback movement
            if (!TryMoveWithWallSliding(nextPosition, elapsed))
            {
                // If we can't move, stop knockback
                _knockbackVelocity = Vector2.Zero;
                _knockbackTimer = 0;
            }

            if (_knockbackTimer <= 0)
            {
                _knockbackVelocity = Vector2.Zero;
            }
        }
    }

    /// <summary>
    /// Attempts to move to the new position with wall sliding support.
    /// Returns true if movement was successful (with or without sliding), false if completely blocked.
    /// </summary>
    private bool TryMoveWithWallSliding(Vector2 nextPosition, float elapsed)
    {
        Vector2 currentPos = Position;
        Vector2 movement = nextPosition - currentPos;
        
        // If no movement, return true (no collision)
        if (movement.LengthSquared() < 0.001f)
            return true;

        // Try full movement first
        Rectangle nextBounds = GetBoundsAtPosition(nextPosition);
        if (!IsPositionBlocked(nextBounds))
        {
            SetPosition(nextPosition);
            return true;
        }

        // If full movement is blocked, try sliding along walls
        
        // Try horizontal movement only (slide along vertical walls)
        Vector2 horizontalTarget = new Vector2(nextPosition.X, currentPos.Y);
        Rectangle horizontalBounds = GetBoundsAtPosition(horizontalTarget);
        if (!IsPositionBlocked(horizontalBounds))
        {
            SetPosition(horizontalTarget);
            
            // Apply some bounce effect for knockback
            if (_knockbackTimer > 0)
            {
                _knockbackVelocity = new Vector2(_knockbackVelocity.X, -_knockbackVelocity.Y * BounceDamping);
            }
            return true;
        }

        // Try vertical movement only (slide along horizontal walls)
        Vector2 verticalTarget = new Vector2(currentPos.X, nextPosition.Y);
        Rectangle verticalBounds = GetBoundsAtPosition(verticalTarget);
        if (!IsPositionBlocked(verticalBounds))
        {
            SetPosition(verticalTarget);
            
            // Apply some bounce effect for knockback
            if (_knockbackTimer > 0)
            {
                _knockbackVelocity = new Vector2(-_knockbackVelocity.X * BounceDamping, _knockbackVelocity.Y);
            }
            return true;
        }

        // If both individual axes are blocked, apply bounce and try to find a safe position
        if (_knockbackTimer > 0)
        {
            // Reverse and dampen velocity for realistic bounce
            _knockbackVelocity = -_knockbackVelocity * BounceDamping;
            
            // If velocity is too small after bouncing, stop knockback
            if (_knockbackVelocity.LengthSquared() < 10f)
            {
                return false;
            }
        }

        // Try to find a safe position nearby
        Vector2 safePosition = FindSafePosition(currentPos, nextPosition);
        if (safePosition != currentPos)
        {
            SetPosition(safePosition);
            return true;
        }

        // Completely blocked
        return false;
    }

    /// <summary>
    /// Checks if a position is blocked by walls or obstacles
    /// </summary>
    private bool IsPositionBlocked(Rectangle bounds)
    {
        // Check wall collision
        if (Tilemap != null && TilesetManager.Instance.WallTileset != null)
        {
            if (CheckWallCollision(bounds))
                return true;
        }
        
        // Check obstacle collision
        var obstacles = GetObstacleRectangles();
        if (obstacles != null)
        {
            foreach (var obstacle in obstacles)
            {
                if (bounds.Intersects(obstacle))
                    return true;
            }
        }
        
        return false;
    }

    /// <summary>
    /// Gets the character's bounds at a specific position.
    /// </summary>
    private Rectangle GetBoundsAtPosition(Vector2 position)
    {
        return new Rectangle(
            (int)position.X + 8,
            (int)position.Y + 16,
            (int)Sprite.Width / 2,
            (int)Sprite.Height / 2
        );
    }

    /// <summary>
    /// Checks for wall collision using Aether collision system integration
    /// </summary>
    private bool CheckWallCollision(Rectangle bounds)
    {
        int leftTile = bounds.Left / (int)Tilemap.TileWidth;
        int rightTile = (bounds.Right - 1) / (int)Tilemap.TileWidth;
        int topTile = bounds.Top / (int)Tilemap.TileHeight;
        int bottomTile = (bounds.Bottom - 1) / (int)Tilemap.TileHeight;

        for (int col = leftTile; col <= rightTile; col++)
        {
            for (int row = topTile; row <= bottomTile; row++)
            {
                if (col >= 0 && col < Tilemap.Columns && row >= 0 && row < Tilemap.Rows)
                {
                    var tileTileset = Tilemap.GetTileset(col, row);
                    var tileId = Tilemap.GetTileId(col, row);
                    if (tileTileset == TilesetManager.Instance.WallTileset && AutotileMapper.IsWallTile(tileId))
                    { 
                        return true;
                    }
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Finds a safe position when collision is detected.
    /// </summary>
    private Vector2 FindSafePosition(Vector2 currentPos, Vector2 targetPos)
    {
        // Try to find a position that doesn't collide
        Vector2 direction = targetPos - currentPos;
        
        // Try stepping back along the movement direction
        for (float step = 0.1f; step <= 1.0f; step += 0.1f)
        {
            Vector2 testPos = currentPos + direction * (1.0f - step);
            Rectangle testBounds = GetBoundsAtPosition(testPos);
            
            if (!IsPositionBlocked(testBounds))
                return testPos;
        }
        
        // Fallback to current position
        return currentPos;
    }

    /// <summary>
    /// Override this in derived classes to provide obstacle rectangles for collision detection.
    /// This should include dungeon elements, other characters, etc.
    /// </summary>
    protected virtual IEnumerable<Rectangle> GetObstacleRectangles()
    {
        return null; // Base implementation returns no obstacles
    }
}