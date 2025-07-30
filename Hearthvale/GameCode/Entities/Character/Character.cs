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

    public virtual void TakeDamage(int amount, Vector2? knockback = null)
    {
        _currentHealth = Math.Max(0, _currentHealth - amount);
        Flash();
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

    public virtual void Draw(SpriteBatch spriteBatch)
    {
        _sprite.Draw(spriteBatch, _position);
    }
}