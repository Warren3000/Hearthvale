using Hearthvale.GameCode.Entities.Interfaces;
using Hearthvale.GameCode.Entities.Players;
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
    public int MaxHealth => _maxHealth;
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

            if (Tilemap != null && WallTileId != -1)
            {
                Rectangle candidateBounds = new Rectangle(
                    (int)nextPosition.X + 8,
                    (int)nextPosition.Y + 16,
                    (int)Sprite.Width / 2,
                    (int)Sprite.Height / 2
                );

                bool collided = false;
                int leftTile = candidateBounds.Left / (int)Tilemap.TileWidth;
                int rightTile = (candidateBounds.Right - 1) / (int)Tilemap.TileWidth;
                int topTile = candidateBounds.Top / (int)Tilemap.TileHeight;
                int bottomTile = (candidateBounds.Bottom - 1) / (int)Tilemap.TileHeight;

                for (int col = leftTile; col <= rightTile; col++)
                {
                    for (int row = topTile; row <= bottomTile; row++)
                    {
                        int tileId = Tilemap.GetTileId(col, row);
                        if (tileId == WallTileId)
                        {
                            collided = true;
                            break;
                        }
                    }
                    if (collided) break;
                }

                if (collided)
                {
                    Vector2 oldPosition = Position;

                    // Try X axis
                    Vector2 testX = new Vector2(nextPosition.X, oldPosition.Y);
                    Rectangle boundsX = new Rectangle(
                        (int)testX.X + 8,
                        (int)testX.Y + 16,
                        (int)Sprite.Width / 2,
                        (int)Sprite.Height / 2
                    );
                    bool xBlocked = false;
                    leftTile = boundsX.Left / (int)Tilemap.TileWidth;
                    rightTile = (boundsX.Right - 1) / (int)Tilemap.TileWidth;
                    topTile = boundsX.Top / (int)Tilemap.TileHeight;
                    bottomTile = (boundsX.Bottom - 1) / (int)Tilemap.TileHeight;
                    for (int col = leftTile; col <= rightTile; col++)
                    {
                        for (int row = topTile; row <= bottomTile; row++)
                        {
                            int tileId = Tilemap.GetTileId(col, row);
                            if (tileId == WallTileId)
                            {
                                xBlocked = true;
                                break;
                            }
                        }
                        if (xBlocked) break;
                    }

                    // Try Y axis
                    Vector2 testY = new Vector2(oldPosition.X, nextPosition.Y);
                    Rectangle boundsY = new Rectangle(
                        (int)testY.X + 8,
                        (int)testY.Y + 16,
                        (int)Sprite.Width / 2,
                        (int)Sprite.Height / 2
                    );
                    bool yBlocked = false;
                    leftTile = boundsY.Left / (int)Tilemap.TileWidth;
                    rightTile = (boundsY.Right - 1) / (int)Tilemap.TileWidth;
                    topTile = boundsY.Top / (int)Tilemap.TileHeight;
                    bottomTile = (boundsY.Bottom - 1) / (int)Tilemap.TileHeight;
                    for (int col = leftTile; col <= rightTile; col++)
                    {
                        for (int row = topTile; row <= bottomTile; row++)
                        {
                            int tileId = Tilemap.GetTileId(col, row);
                            if (tileId == WallTileId)
                            {
                                yBlocked = true;
                                break;
                            }
                        }
                        if (yBlocked) break;
                    }

                    if (xBlocked) _knockbackVelocity.X = -_knockbackVelocity.X * BounceDamping;
                    if (yBlocked) _knockbackVelocity.Y = -_knockbackVelocity.Y * BounceDamping;

                    SetPosition(oldPosition);

                    if (_knockbackVelocity.LengthSquared() < 1f)
                    {
                        _knockbackVelocity = Vector2.Zero;
                        _knockbackTimer = 0;
                    }
                }
                else
                {
                    SetPosition(nextPosition);
                }
            }
            else
            {
                SetPosition(Position + _knockbackVelocity * elapsed);
            }

            if (_knockbackTimer <= 0)
            {
                _knockbackVelocity = Vector2.Zero;
            }
        }
    }

    /// <summary>
    /// Checks if the character's bounds would overlap any of the provided rectangles.
    /// </summary>
    /// <param name="candidatePosition">The position to test.</param>
    /// <param name="obstacleBounds">A collection of rectangles representing obstacles.</param>
    /// <returns>True if collision detected, false otherwise.</returns>
    public bool WouldCollide(Vector2 candidatePosition, IEnumerable<Rectangle> obstacleBounds)
    {
        Rectangle candidateRect = new Rectangle(
            (int)candidatePosition.X + 8,
            (int)candidatePosition.Y + 16,
            (int)Sprite.Width / 2,
            (int)Sprite.Height / 2
        );

        foreach (var rect in obstacleBounds)
        {
            if (candidateRect.Intersects(rect))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Attempts to set the character's position, blocking movement if it would collide with any obstacles.
    /// </summary>
    /// <param name="pos">The desired position.</param>
    /// <param name="obstacleBounds">A collection of rectangles representing obstacles.</param>
    /// <returns>True if movement succeeded, false if blocked.</returns>
    public bool TrySetPosition(Vector2 pos, IEnumerable<Rectangle> obstacleBounds)
    {
        if (WouldCollide(pos, obstacleBounds))
            return false;

        SetPosition(pos);
        return true;
    }
}