using Hearthvale.GameCode.Entities.Interfaces;
using Hearthvale.GameCode.Entities.Players;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Graphics;
using System;

namespace Hearthvale.GameCode.Entities.Characters;

public abstract class Character : IDamageable, IMovable, IAnimatable
{
    protected AnimatedSprite _sprite;
    protected Vector2 _position;
    protected bool _facingRight = true;
    protected string _currentAnimationName;
    protected int _maxHealth;
    protected int _currentHealth;

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
}