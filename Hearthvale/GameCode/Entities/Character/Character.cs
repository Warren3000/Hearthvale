using Hearthvale.GameCode.Entities.Interfaces;
using Hearthvale.GameCode.Entities.Players;
using Hearthvale.GameCode.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
    public int WallTileId { get; set; }

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

            // Check for collision and handle bouncing
            if (!TryMoveWithBounce(nextPosition, elapsed))
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
    /// Attempts to move to the new position, handling wall and obstacle bouncing.
    /// Returns true if movement was successful (with or without bouncing), false if completely blocked.
    /// </summary>
    private bool TryMoveWithBounce(Vector2 nextPosition, float elapsed)
    {
        Vector2 currentPos = Position;
        Rectangle nextBounds = GetBoundsAtPosition(nextPosition);
        
        // Check for wall collision first
        bool hitWall = false;
        Vector2 wallBounce = Vector2.Zero;
        
        if (Tilemap != null && WallTileId != -1)
        {
            hitWall = CheckWallCollisionAndBounce(nextBounds, out wallBounce);
        }
        
        // Check for obstacle collision
        bool hitObstacle = false;
        Vector2 obstacleBounce = Vector2.Zero;
        
        // Get all obstacle rectangles (this should be provided by the game scene)
        var obstacles = GetObstacleRectangles();
        if (obstacles != null)
        {
            hitObstacle = CheckObstacleCollisionAndBounce(nextBounds, obstacles, out obstacleBounce);
        }

        // Apply bouncing
        if (hitWall || hitObstacle)
        {
            // Combine bounce vectors if we hit multiple things
            Vector2 totalBounce = wallBounce + obstacleBounce;
            
            // Apply bounce to velocity with damping
            _knockbackVelocity += totalBounce * BounceDamping;
            
            // Try to move in a safe direction
            Vector2 safePosition = FindSafePosition(currentPos, nextPosition);
            SetPosition(safePosition);
            
            // If velocity is too small after bouncing, stop knockback
            if (_knockbackVelocity.LengthSquared() < 10f)
            {
                return false;
            }
            
            return true;
        }
        else
        {
            // No collision, move normally
            SetPosition(nextPosition);
            return true;
        }
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
    /// Checks for wall collision and calculates bounce vector.
    /// </summary>
    private bool CheckWallCollisionAndBounce(Rectangle bounds, out Vector2 bounceVector)
    {
        bounceVector = Vector2.Zero;
        
        int leftTile = bounds.Left / (int)Tilemap.TileWidth;
        int rightTile = (bounds.Right - 1) / (int)Tilemap.TileWidth;
        int topTile = bounds.Top / (int)Tilemap.TileHeight;
        int bottomTile = (bounds.Bottom - 1) / (int)Tilemap.TileHeight;

        bool hitWall = false;
        bool hitHorizontalWall = false;
        bool hitVerticalWall = false;

        for (int col = leftTile; col <= rightTile; col++)
        {
            for (int row = topTile; row <= bottomTile; row++)
            {
                if (col >= 0 && col < Tilemap.Columns && row >= 0 && row < Tilemap.Rows)
                {
                    int tileId = Tilemap.GetTileId(col, row);
                    // Use AutotileMapper to check if this is any type of wall tile
                    if (AutotileMapper.IsWallTile(tileId))
                    {
                        hitWall = true;
                        
                        // Determine which side of the wall we hit
                        Rectangle wallRect = new Rectangle(
                            col * (int)Tilemap.TileWidth,
                            row * (int)Tilemap.TileHeight,
                            (int)Tilemap.TileWidth,
                            (int)Tilemap.TileHeight
                        );
                        
                        // Check overlap amounts to determine bounce direction
                        int overlapLeft = bounds.Right - wallRect.Left;
                        int overlapRight = wallRect.Right - bounds.Left;
                        int overlapTop = bounds.Bottom - wallRect.Top;
                        int overlapBottom = wallRect.Bottom - bounds.Top;
                        
                        // Determine primary collision direction
                        int minHorizontal = Math.Min(overlapLeft, overlapRight);
                        int minVertical = Math.Min(overlapTop, overlapBottom);
                        
                        if (minHorizontal < minVertical)
                        {
                            hitVerticalWall = true;
                        }
                        else
                        {
                            hitHorizontalWall = true;
                        }
                    }
                }
            }
        }

        if (hitWall)
        {
            if (hitVerticalWall)
                bounceVector.X = -_knockbackVelocity.X * 1.5f; // Reverse and amplify X
            if (hitHorizontalWall)
                bounceVector.Y = -_knockbackVelocity.Y * 1.5f; // Reverse and amplify Y
        }

        return hitWall;
    }

    /// <summary>
    /// Checks for obstacle collision and calculates bounce vector.
    /// </summary>
    private bool CheckObstacleCollisionAndBounce(Rectangle bounds, IEnumerable<Rectangle> obstacles, out Vector2 bounceVector)
    {
        bounceVector = Vector2.Zero;
        bool hitObstacle = false;
        
        foreach (var obstacle in obstacles)
        {
            if (bounds.Intersects(obstacle))
            {
                hitObstacle = true;
                
                // Calculate bounce direction based on overlap
                Vector2 obstacleCenter = obstacle.Center.ToVector2();
                Vector2 characterCenter = bounds.Center.ToVector2();
                Vector2 direction = characterCenter - obstacleCenter;
                
                if (direction.LengthSquared() > 0)
                {
                    direction.Normalize();
                    bounceVector += direction * _knockbackVelocity.Length() * 0.8f;
                }
            }
        }
        
        return hitObstacle;
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
            
            bool safe = true;
            
            // Check walls
            if (Tilemap != null && WallTileId != -1)
            {
                if (CheckWallCollisionAndBounce(testBounds, out _))
                    safe = false;
            }
            
            // Check obstacles
            var obstacles = GetObstacleRectangles();
            if (obstacles != null && safe)
            {
                if (CheckObstacleCollisionAndBounce(testBounds, obstacles, out _))
                    safe = false;
            }
            
            if (safe)
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