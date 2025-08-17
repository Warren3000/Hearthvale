using Hearthvale.GameCode.Entities.Interfaces;
using Hearthvale.GameCode.Entities.Players;
using Hearthvale.GameCode.Entities.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Graphics;
using System.Collections.Generic;
using System.Linq;
using Hearthvale.GameCode.Utils;
using System;

namespace Hearthvale.GameCode.Entities;

public abstract class Character : IDamageable, IMovable, IAnimatable, IDialog
{
    // Expose components to subclasses
    protected CharacterHealthComponent _healthComponent;
    protected CharacterCollisionComponent _collisionComponent;
    protected CharacterWeaponComponent _weaponComponent;
    protected CharacterMovementComponent _movementComponent;
    protected CharacterAnimationComponent _animationComponent;
    protected CharacterRenderComponent _renderComponent;

    public string DialogText { get; set; } = "Hello, adventurer!";

    // Properties - Delegate to components
    public virtual AnimatedSprite Sprite => _animationComponent?.Sprite;
    public virtual Vector2 Position => _movementComponent?.Position ?? Vector2.Zero;
    public virtual int Health => _healthComponent?.CurrentHealth ?? 0;
    public virtual int MaxHealth => _healthComponent?.MaxHealth ?? 0;
    public virtual float MovementSpeed => _movementComponent?.MovementSpeed ?? 0;
    public virtual bool IsDefeated => _healthComponent?.IsDefeated ?? false;

    // Component-specific accessors for subclasses
    public CharacterHealthComponent HealthComponent => _healthComponent;
    public CharacterCollisionComponent CollisionComponent => _collisionComponent;
    public CharacterWeaponComponent WeaponComponent => _weaponComponent;
    public CharacterMovementComponent MovementComponent => _movementComponent;
    public CharacterAnimationComponent AnimationComponent => _animationComponent;
    public CharacterRenderComponent RenderComponent => _renderComponent;

    public bool FacingRight
    {
        get => _movementComponent?.FacingRight ?? true;
        set { if (_movementComponent != null) _movementComponent.FacingRight = value; }
    }

    public string CurrentAnimationName
    {
        get => _animationComponent?.CurrentAnimationName;
        set { if (_animationComponent != null) _animationComponent.SetAnimation(value); }
    }

    public Weapon EquippedWeapon => _weaponComponent?.EquippedWeapon;

    // Component access for specialized behaviors
    public bool IsKnockedBack => _collisionComponent?.IsKnockedBack ?? false;

    protected Character()
    {
        InitializeComponents();
    }

    protected virtual void InitializeComponents()
    {
        _healthComponent = new CharacterHealthComponent(this, 100);
        _collisionComponent = new CharacterCollisionComponent(this);
        _weaponComponent = new CharacterWeaponComponent(this);
        _movementComponent = new CharacterMovementComponent(this, Vector2.Zero);
        _animationComponent = new CharacterAnimationComponent(this, null);
        _renderComponent = new CharacterRenderComponent(this);
    }

    protected virtual void InitializeHealth(int maxHealth)
    {
        _healthComponent = new CharacterHealthComponent(this, maxHealth);
    }

    // IDamageable implementation
    public virtual bool TakeDamage(int amount, Vector2? knockback = null)
    {
        bool justDefeated = _healthComponent.TakeDamage(amount, knockback);

        if (knockback.HasValue)
        {
            _collisionComponent.SetKnockback(knockback.Value);
        }

        return justDefeated;
    }

    public virtual bool Revive() => _healthComponent.Revive();
    public virtual void Heal(int amount) => _healthComponent.Heal(amount);

    // IMovable implementation
    public virtual void SetPosition(Vector2 pos)
    {
        _movementComponent?.SetPosition(pos);
    }

    public void ClampToBounds(Rectangle bounds)
    {
        _movementComponent?.ClampToBounds(bounds);
    }

    // Weapon management
    public virtual void EquipWeapon(Weapon weapon) => _weaponComponent.EquipWeapon(weapon);
    public virtual Rectangle GetAttackArea() => _weaponComponent.GetAttackArea();

    // Collision and movement
    public void UpdateKnockback(GameTime gameTime) => _collisionComponent.UpdateKnockback(gameTime);
    public virtual void SetKnockback(Vector2 velocity) => _collisionComponent.SetKnockback(velocity);
    public virtual Vector2 GetKnockbackVelocity() => _collisionComponent.GetKnockbackVelocity();
    public virtual Rectangle Bounds => this.GetTightSpriteBounds();

    // Abstract and virtual methods
    public abstract void Flash();
    protected virtual Vector2 GetLastMovementDirection()
    {
        // Return the last movement vector from the movement component
        return MovementComponent?.LastMovementVector ?? Vector2.Zero;
    }
    
    // Update GetAttackDirection to use cardinal directions
    protected virtual Vector2 GetAttackDirection()
    {
        CardinalDirection facing = MovementComponent?.FacingDirection ?? CardinalDirection.South;
        return facing.ToVector();
    }

    // Update ShouldDrawWeaponBehind to check for North direction
    public bool GetShouldDrawWeaponBehind()
    {
        return ShouldDrawWeaponBehind();
    }
    protected bool ShouldDrawWeaponBehind()
    {
        // Only draw weapon behind when facing north (up)
        return MovementComponent.FacingDirection == CardinalDirection.North ;
    }

    /// <summary>
    /// Gets the tight polygon bounds for this character based on sprite outline
    /// </summary>
    public virtual List<Vector2> GetPolygonBounds()
    {
        if (Sprite?.Region?.Texture == null)
            return GetFallbackPolygonBounds();

        // Get the orientation-aware content bounds
        Rectangle contentBounds = this.GetOrientationAwareBounds();

        // Create a polygon from the analyzed bounds
        var polygon = new List<Vector2>
    {
        new Vector2(contentBounds.Left, contentBounds.Top),
        new Vector2(contentBounds.Right, contentBounds.Top),
        new Vector2(contentBounds.Right, contentBounds.Bottom),
        new Vector2(contentBounds.Left, contentBounds.Bottom)
    };

        // Apply rotation if the sprite is rotated
        if (Sprite.Rotation != 0)
        {
            Vector2 center = new Vector2(
                contentBounds.Left + contentBounds.Width / 2,
                contentBounds.Top + contentBounds.Height / 2
            );
            polygon = RotatePolygon(polygon, Sprite.Rotation, center);
        }

        return polygon;
    }

    /// <summary>
    /// Determines the character type for polygon generation
    /// </summary>
    protected virtual CharacterType GetCharacterType()
    {
        return this.GetType().Name.Contains("Player") ? CharacterType.Player : CharacterType.NPC;
    }

    /// <summary>
    /// Rotates a polygon around a center point
    /// </summary>
    private List<Vector2> RotatePolygon(List<Vector2> polygon, float rotation, Vector2 center)
    {
        var rotatedPolygon = new List<Vector2>();
        var cos = MathF.Cos(rotation);
        var sin = MathF.Sin(rotation);
        
        foreach (var vertex in polygon)
        {
            // Translate to origin
            var translated = vertex - center;
            
            // Rotate
            var rotated = new Vector2(
                translated.X * cos - translated.Y * sin,
                translated.X * sin + translated.Y * cos
            );
            
            // Translate back
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

    /// <summary>
    /// Checks if this character's polygon bounds intersect with another polygon
    /// </summary>
    public virtual bool IntersectsWith(List<Vector2> otherPolygon)
    {
        var myPolygon = GetPolygonBounds();
        return PolygonIntersection.DoPolygonsIntersect(myPolygon, otherPolygon);
    }

    /// <summary>
    /// Checks if this character's polygon bounds intersect with a rectangle
    /// </summary>
    public virtual bool IntersectsWith(Rectangle rectangle)
    {
        var rectPolygon = new List<Vector2>
        {
            new Vector2(rectangle.Left, rectangle.Top),
            new Vector2(rectangle.Right, rectangle.Top),
            new Vector2(rectangle.Right, rectangle.Bottom),
            new Vector2(rectangle.Left, rectangle.Bottom)
        };
        
        return IntersectsWith(rectPolygon);
    }

    // Drawing - delegated to render component
    public virtual void Draw(SpriteBatch spriteBatch)
    {
        _renderComponent?.Draw(spriteBatch);
    }

    public virtual void DrawDebug(SpriteBatch spriteBatch, Texture2D pixel)
    {
        _renderComponent?.DrawDebug(spriteBatch, pixel);
    }

    // Collision configuration
    public void SetTilemap(Tilemap tilemap)
    {
        _collisionComponent.Tilemap = tilemap;
    }

    public Tilemap GetTilemap()
    {
        return _collisionComponent.Tilemap;
    }

    public virtual IEnumerable<Rectangle> GetObstacleRectangles()
    {
        return null;
    }

    // Make this accessible to collision component
    internal IEnumerable<Rectangle> GetObstacleRectanglesInternal()
    {
        return GetObstacleRectangles();
    }

    // Methods for sprite initialization
    protected virtual void InitializeSprite(AnimatedSprite sprite)
    {
        _animationComponent?.SetSprite(sprite);
    }

    /// <summary>
    /// Gets the proper hitbox rectangle accounting for sprite orientation
    /// </summary>
    public Rectangle GetOrientationAwareBounds()
    {
        // Get the current sprite
        var sprite = this.Sprite;
        if (sprite == null)
            return new Rectangle((int)Position.X, (int)Position.Y, 32, 32); // Default fallback

        // Get analyzed content bounds
        Rectangle contentBounds = this.GetTightSpriteBounds();

        // For right-facing sprites, use the bounds directly
        if (FacingRight)
            return contentBounds;

        // For left-facing sprites, we need to mirror the bounds around the sprite's center
        // Calculate sprite center X position
        float centerX = Position.X + (sprite.Width / 2f);

        // Calculate new rectangle by flipping around center
        int newLeft = (int)(2 * centerX - contentBounds.Right);

        return new Rectangle(
            newLeft,
            contentBounds.Y,
            contentBounds.Width,
            contentBounds.Height
        );
    }
}

/// <summary>
/// Enumeration for different character types to help with polygon generation
/// </summary>
public enum CharacterType
{
    Player,
    NPC
}