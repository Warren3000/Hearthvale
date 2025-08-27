using Hearthvale.GameCode.Entities.Components;
using Hearthvale.GameCode.Entities.Interfaces;
using Hearthvale.GameCode.Entities.NPCs;
using Hearthvale.GameCode.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hearthvale.GameCode.Entities;

/// <summary>
/// Base class for all characters (player, NPCs, etc.)
/// </summary>
public abstract class Character : IDamageable, IMovable, IAnimatable, IDialog
{
    #region Fields

    // Components
    protected CharacterHealthComponent _healthComponent;
    protected CharacterCollisionComponent _collisionComponent;
    protected CharacterWeaponComponent _weaponComponent;
    protected CharacterMovementComponent _movementComponent;
    protected CharacterAnimationComponent _animationComponent;
    protected CharacterRenderComponent _renderComponent;
    protected CharacterAIComponent _aiComponent;

    // Cached obstacles for collision
    private List<Rectangle> _cachedObstacles = new List<Rectangle>();

    #endregion

    #region Properties

    public string DialogText { get; set; } = "";

    // Sprite property delegates to animation component or NPC animation provider
    public virtual AnimatedSprite Sprite
    {
        get
        {
            if (_animationComponent?.Sprite != null)
                return _animationComponent.Sprite;
            if (this is INpcAnimationProvider npcProvider)
                return npcProvider.GetAnimationSprite();
            return null;
        }
        protected set { }
    }

    public virtual Vector2 Position => _movementComponent?.Position ?? Vector2.Zero;
    public CharacterAIComponent AIComponent => _aiComponent;
    public virtual int Health => _healthComponent?.CurrentHealth ?? 0;
    public virtual int MaxHealth => _healthComponent?.MaxHealth ?? 0;
    public virtual bool IsDefeated => _healthComponent?.IsDefeated ?? false;
    public bool IsAttacking { get; set; }

    // Component accessors for subclasses
    public CharacterHealthComponent HealthComponent => _healthComponent;
    public CharacterCollisionComponent CollisionComponent => _collisionComponent;
    public CharacterWeaponComponent WeaponComponent => _weaponComponent;
    public CharacterMovementComponent MovementComponent => _movementComponent;
    public CharacterAnimationComponent AnimationComponent => _animationComponent;

    public bool FacingRight
    {
        get => _movementComponent?.FacingRight ?? true;
        set { if (_movementComponent != null) _movementComponent.FacingRight = value; }
    }

    public Weapon EquippedWeapon => _weaponComponent?.EquippedWeapon;
    public bool IsKnockedBack => _collisionComponent?.IsKnockedBack ?? false;

    public virtual Rectangle Bounds => this.GetTightSpriteBounds();

    #endregion

    #region Initialization

    protected virtual void InitializeComponents()
    {
        _healthComponent = new CharacterHealthComponent(this, 100);
        _collisionComponent = new CharacterCollisionComponent(this);
        _weaponComponent = new CharacterWeaponComponent(this);
        _movementComponent = new CharacterMovementComponent(this, Vector2.Zero);
        _animationComponent = new CharacterAnimationComponent(this, null);
        _renderComponent = new CharacterRenderComponent(this);
        _aiComponent = new CharacterAIComponent(this, _movementComponent);
        _aiComponent.IsEnabled = false; // Disabled by default for player
    }
    public void SetAIControlled(bool enabled, NpcAiType aiType = NpcAiType.Wander)
    {
        if (_aiComponent != null)
        {
            _aiComponent.IsEnabled = enabled;
            _aiComponent.AiType = aiType;
        }
    }
    protected virtual void InitializeHealth(int maxHealth)
    {
        _healthComponent = new CharacterHealthComponent(this, maxHealth);
    }

    #endregion

    #region Interface Implementations

    // IDamageable
    public virtual bool TakeDamage(int amount, Vector2? knockback = null)
    {
        bool justDefeated = _healthComponent.TakeDamage(amount, knockback);

        if (knockback.HasValue)
        {
            _collisionComponent.SetKnockback(knockback.Value);
        }

        return justDefeated;
    }

    public virtual void Heal(int amount) => _healthComponent.Heal(amount);

    // IMovable
    public virtual void SetPosition(Vector2 pos)
    {
        _movementComponent?.SetPosition(pos);
    }

    public void ClampToBounds(Rectangle bounds)
    {
        _movementComponent?.ClampToBounds(bounds);
    }

    #endregion

    #region Weapon Management

    public virtual void EquipWeapon(Weapon weapon) => _weaponComponent.EquipWeapon(weapon);
    public virtual void UnequipWeapon() => _weaponComponent.UnequipWeapon();
    public virtual Rectangle GetAttackArea() => _weaponComponent.GetAttackArea();

    #endregion

    #region Collision and Movement

    public void UpdateKnockback(GameTime gameTime) => _collisionComponent.UpdateKnockback(gameTime);
    public virtual Vector2 GetVelocity() => _movementComponent?.GetVelocity() ?? Vector2.Zero;

    #endregion

    #region Drawing

    /// <summary>
    /// Gets the current opacity for rendering. Override in subclasses that need fade effects.
    /// </summary>
    protected virtual float GetRenderOpacity() => 1f;

    public virtual void Draw(SpriteBatch spriteBatch)
    {
        var opacity = GetRenderOpacity();
        if (opacity <= 0f)
            return; // Skip drawing if fully transparent
            
        _renderComponent?.Draw(spriteBatch);
    }

    #endregion

    #region Obstacle and Tilemap

    public void SetTilemap(Tilemap tilemap) => CollisionComponent.Tilemap = tilemap;
    public virtual IEnumerable<Rectangle> GetObstacleRectangles() => _cachedObstacles;
    public void SetObstacleRectangles(IEnumerable<Rectangle> obstacles) => _cachedObstacles = obstacles.ToList();

    #endregion

    #region Combat

    public virtual Rectangle GetCombatBounds()
    {
        // Use tight sprite bounds for combat calculations
        return this.GetTightSpriteBounds();
    }

    #endregion

    #region Protected/Virtual/Abstract Methods

    public abstract void Flash();

    /// <summary>
    /// Updates the equipped weapon's position and rotation based on character state
    /// </summary>
    protected virtual void UpdateWeapon(GameTime gameTime)
    {
        _weaponComponent?.Update(gameTime, GetWeaponTarget());
    }

    /// <summary>
    /// Gets the weapon target for aiming. Override in subclasses that need targeting.
    /// </summary>
    protected virtual Vector2? GetWeaponTarget()
    {
        return null;
    }

    /// <summary>
    /// Gets the attack direction for this character. Override in subclasses.
    /// </summary>
    protected abstract Vector2 GetAttackDirection();

    /// <summary>
    /// Gets whether the weapon should be drawn behind the character (when facing north)
    /// </summary>
    public bool GetShouldDrawWeaponBehind()
    {
        return ShouldDrawWeaponBehind();
    }

    protected bool ShouldDrawWeaponBehind()
    {
        return MovementComponent.FacingDirection == CardinalDirection.North;
    }

    /// <summary>
    /// Gets the tight polygon bounds for this character based on sprite outline
    /// </summary>
    public virtual List<Vector2> GetPolygonBounds()
    {
        if (Sprite?.Region?.Texture == null)
            return GetFallbackPolygonBounds();

        var polygon = new List<Vector2>
        {
            new Vector2(Bounds.Left, Bounds.Top),
            new Vector2(Bounds.Right, Bounds.Top),
            new Vector2(Bounds.Right, Bounds.Bottom),
            new Vector2(Bounds.Left, Bounds.Bottom)
        };

        if (Sprite.Rotation != 0)
        {
            Vector2 center = new Vector2(
                Bounds.Left + Bounds.Width / 2,
                Bounds.Top + Bounds.Height / 2
            );
            polygon = RotatePolygon(polygon, Sprite.Rotation, center);
        }

        return polygon;
    }
    /// <summary>
    /// Rotates a polygon around a center point
    /// </summary>
    public List<Vector2> RotatePolygon(List<Vector2> polygon, float rotation, Vector2 center)
    {
        var rotatedPolygon = new List<Vector2>();
        var cos = MathF.Cos(rotation);
        var sin = MathF.Sin(rotation);

        foreach (var vertex in polygon)
        {
            var translated = vertex - center;
            var rotated = new Vector2(
                translated.X * cos - translated.Y * sin,
                translated.X * sin + translated.Y * cos
            );
            rotatedPolygon.Add(rotated + center);
        }

        return rotatedPolygon;
    }

    /// <summary>
    /// Gets fallback polygon bounds when sprite data is not available
    /// </summary>
    private List<Vector2> GetFallbackPolygonBounds()
    {
        var bounds = Bounds;
        return new List<Vector2>
        {
            new Vector2(bounds.Left, bounds.Top),
            new Vector2(bounds.Right, bounds.Top),
            new Vector2(bounds.Right, bounds.Bottom),
            new Vector2(bounds.Left, bounds.Bottom)
        };
    }

    #endregion


}