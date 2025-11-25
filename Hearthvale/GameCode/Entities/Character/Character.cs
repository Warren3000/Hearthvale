using Hearthvale.GameCode.Data.Models;
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
    #region Bounds
    private const int DefaultBoundsWidth = 12;
    private const int DefaultBoundsHeight = 12;
    private AttackShapeDefinition _collisionBodyOverride;
    protected Rectangle? _definedCollisionBox;

    /// <summary>
    /// Sets a fixed collision box relative to the sprite's top-left corner.
    /// </summary>
    public void SetCollisionBox(Rectangle box)
    {
        _definedCollisionBox = box;
    }

    /// <summary>
    /// Returns an axis-aligned rectangle that tightly encloses the visible sprite pixels at the current position.
    /// </summary>
    public virtual Rectangle GetTightSpriteBounds() => GetSpriteBoundsAt(Position);

    /// <summary>
    /// Computes the tight bounds for this character's sprite as if it were rendered at the supplied position.
    /// </summary>
    public virtual Rectangle GetSpriteBoundsAt(Vector2 position)
    {
        var sprite = Sprite;
        if (sprite?.Region?.Texture == null)
        {
            return CreateCenteredBounds(position, DefaultBoundsWidth, DefaultBoundsHeight);
        }

        // Default to a centered box based on the sprite size if no specific collision box is defined
        // This replaces the expensive pixel-perfect analysis
        int width = (int)(sprite.Width * 0.5f); // Use 50% of sprite width
        int height = (int)(sprite.Height * 0.4f); // Use 40% of sprite height
        
        // Ensure minimum size
        width = Math.Max(width, DefaultBoundsWidth);
        height = Math.Max(height, DefaultBoundsHeight);

        // Center at the bottom of the sprite (feet)
        int left = (int)(position.X - width / 2f);
        int top = (int)(position.Y + sprite.Height / 2f - height); // Align bottom with sprite bottom

        return new Rectangle(left, top, width, height);
    }

    private static Rectangle CreateCenteredBounds(Vector2 position, int width, int height)
    {
        int left = (int)Math.Round(position.X - width / 2f);
        int top = (int)Math.Round(position.Y - height / 2f);
        return new Rectangle(left, top, width, height);
    }

    private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

    /// <summary>
    /// Mirrors the origin used during rendering so bounds align with the visual sprite placement.
    /// </summary>
    protected virtual Vector2 CalculateRenderOrigin(AnimatedSprite sprite)
    {
        if (sprite?.Region == null)
        {
            return Vector2.Zero;
        }

        float originX = sprite.Width / 2f;
        float originY = sprite.Height / 2f - 1f;

        return new Vector2(originX, originY);
    }
    #endregion
    #region Fields

    // Components
    protected CharacterHealthComponent _healthComponent;
    protected CharacterCollisionComponent _collisionComponent;
    protected CharacterWeaponComponent _weaponComponent;
    protected CharacterMovementComponent _movementComponent;
    protected CharacterAnimationComponent _animationComponent;
    protected CharacterRenderComponent _renderComponent;
    protected CharacterAIComponent _aiComponent;

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

    public virtual Rectangle Bounds => GetCollisionBoundsAt(Position);
    public bool HasCollisionOverride => _collisionBodyOverride != null;

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

        // Draw the sprite centered on the character's position (blue cross)
        if (Sprite != null)
        {
            // Use sprite analyzer logic: center the character in the content area
            var origin = new Vector2(Sprite.Width / 2f, Sprite.Height / 2f - 1f); // -1 for top padding
            spriteBatch.Draw(
                Sprite.Region.Texture,
                Position,
                Sprite.Region.SourceRectangle,
                Sprite.Color * opacity,
                Sprite.Rotation,
                origin,
                Sprite.Scale,
                Sprite.Effects,
                Sprite.LayerDepth
            );
        }
        else
        {
            _renderComponent?.Draw(spriteBatch);
        }
    }

    #endregion

    #region Combat

    public virtual Rectangle GetCombatBounds() => GetCollisionBoundsAt(Position);

    public Rectangle GetCollisionBoundsAt(Vector2 position)
    {
        if (_collisionBodyOverride != null)
        {
            return BuildCollisionBoundsFromShape(_collisionBodyOverride, position);
        }

        if (_definedCollisionBox.HasValue)
        {
            var sprite = Sprite;
            if (sprite != null)
            {
                var origin = CalculateRenderOrigin(sprite);
                // Top-left of sprite in world space
                // Note: We ignore rotation for the simple collision box as it's usually AABB
                var topLeft = position - origin;

                return new Rectangle(
                    (int)(topLeft.X + _definedCollisionBox.Value.X),
                    (int)(topLeft.Y + _definedCollisionBox.Value.Y),
                    _definedCollisionBox.Value.Width,
                    _definedCollisionBox.Value.Height
                );
            }
        }

        return GetSpriteBoundsAt(position);
    }

    internal void SetCollisionBodyOverride(AttackShapeDefinition overrideShape)
    {
        _collisionBodyOverride = overrideShape;
    }

    internal void ClearCollisionBodyOverride()
    {
        _collisionBodyOverride = null;
    }

    private Rectangle BuildCollisionBoundsFromShape(AttackShapeDefinition shape, Vector2 position)
    {
        if (shape == null)
        {
            return GetSpriteBoundsAt(position);
        }

        Rectangle defaultBounds = GetSpriteBoundsAt(position);
        float width = shape.Width ?? shape.Length ?? defaultBounds.Width;
        float height = shape.Height ?? shape.Length ?? defaultBounds.Height;

        // Clamp to reasonable minimums to avoid degenerate rectangles
        width = MathF.Max(1f, width);
        height = MathF.Max(1f, height);

        var direction = MovementComponent?.FacingDirection.ToVector() ?? Vector2.Zero;
        if (direction.LengthSquared() > 0.0001f)
        {
            direction.Normalize();
        }
        else
        {
            direction = Vector2.UnitY;
        }

        Vector2 perpendicular = new Vector2(-direction.Y, direction.X);

        float forwardOffset = shape.ForwardOffset ?? 0f;
        float lateralOffset = shape.LateralOffset ?? 0f;
        float verticalOffset = shape.VerticalOffset ?? 0f;

        Vector2 offset = direction * forwardOffset + perpendicular * lateralOffset + new Vector2(0f, verticalOffset);
        Vector2 center = position + offset;

        float halfWidth = width * 0.5f;
        float halfHeight = height * 0.5f;

        float leftF = center.X - halfWidth;
        float topF = center.Y - halfHeight;
        float rightF = center.X + halfWidth;
        float bottomF = center.Y + halfHeight;

        int left = (int)MathF.Floor(leftF);
        int top = (int)MathF.Floor(topF);
        int right = (int)MathF.Ceiling(rightF);
        int bottom = (int)MathF.Ceiling(bottomF);

        int rectWidth = Math.Max(1, right - left);
        int rectHeight = Math.Max(1, bottom - top);

        return new Rectangle(left, top, rectWidth, rectHeight);
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
        return MovementComponent.FacingDirection.IsUpwardFacing();
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
            new Vector2(bounds.Left, Bounds.Bottom)
        };
    }

    #endregion


}