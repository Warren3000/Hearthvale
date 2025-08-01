using Hearthvale.GameCode.Entities.Interfaces;
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

        float attackReach = EquippedWeapon.Length;
        int attackWidth, attackHeight;
        Vector2 attackCenter = Position + new Vector2(Sprite.Width / 2, Sprite.Height / 2);
        Vector2 offset = Vector2.Zero;
        Vector2 direction = GetAttackDirection();

        if (Math.Abs(direction.X) > Math.Abs(direction.Y))
        {
            attackWidth = 32;
            attackHeight = (int)Sprite.Height;
            offset.X = (direction.X > 0 ? 1 : -1) * attackReach;
        }
        else
        {
            attackWidth = (int)Sprite.Width;
            attackHeight = 32;
            offset.Y = (direction.Y > 0 ? 1 : -1) * attackReach;
        }

        int x = (int)(attackCenter.X + offset.X - attackWidth / 2);
        int y = (int)(attackCenter.Y + offset.Y - attackHeight / 2);

        return new Rectangle(x, y, attackWidth, attackHeight);
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
        Color originalColor = _sprite.Color;
        Color weaponOriginalColor = EquippedWeapon != null ? EquippedWeapon.Sprite.Color : Color.White;
        float alpha = 1f;
        if (IsDefeated)
        {
            alpha = 0.5f;
        }
        _sprite.Color = Color.White * alpha;
        if (EquippedWeapon != null)
            EquippedWeapon.Sprite.Color = Color.White * alpha;

        bool drawWeaponBehind = ShouldDrawWeaponBehind();

        if (drawWeaponBehind)
        {
            EquippedWeapon?.Draw(spriteBatch, Position);
            _sprite.Draw(spriteBatch, Position);
        }
        else
        {
            _sprite.Draw(spriteBatch, Position);
            EquippedWeapon?.Draw(spriteBatch, Position);
        }
        _sprite.Color = originalColor;
        if (EquippedWeapon != null)
            EquippedWeapon.Sprite.Color = weaponOriginalColor;
    }
}