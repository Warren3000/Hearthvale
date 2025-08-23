using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Graphics;
using System;
using System.Collections.Generic;
using Hearthvale.GameCode.Data;
using Microsoft.Xna.Framework.Audio;
using Hearthvale.GameCode.Utils;

namespace Hearthvale.GameCode.Entities;
public class Weapon
{
    public string Name { get; }
    public AnimatedSprite Sprite { get; }
    public float Rotation { get; set; } = 0f; // Radians
    public Vector2 Position { get; set; }
    public Vector2 ManualOffset { get; set; } = Vector2.Zero;
    public Vector2 Offset { get; set; } = Vector2.Zero;
    public TextureAtlas _atlas { get; set; }
    private readonly TextureAtlas _projectileAtlas;
    public int Level { get; private set; } = 1;
    public int XP { get; private set; }
    public int XpToNextLevel => 10 * (Level + 1);
    public int Damage { get; private set; }
    public float Length { get; private set; }
    public float Scale
    {
        get => Sprite != null ? Sprite.Scale.X : 1.0f;
        set
        {
            if (Sprite != null)
            {
                Sprite.Scale = new Vector2(value, value);
                Length = Sprite.Region.Height * value;
            }
        }
    }
    public List<Vector2> HitPolygon { get; set; } = new List<Vector2>
    {
        new Vector2(0, 0),      // handle/base
        new Vector2(0, -20),    // tip
        new Vector2(8, -16),    // right edge
        new Vector2(8, -8),     // right base
        new Vector2(-8, -8),    // left base
        new Vector2(-8, -16),   // left edge
    };

    // Unified debug/damage visualization parameters
    public const float DefaultHandleOffset = 8;
    public const float DefaultHitThickness = 16f;
    public const int DefaultDebugArcSegments = 24;

    // Visual art faces ~45° and is drawn upright; compensate consistently everywhere
    public static readonly float ArtRotationOffset = MathHelper.PiOver2 + MathHelper.PiOver4;

    private enum SwingState { Idle, WindingUp, Slashing }
    private SwingState _currentSwingState = SwingState.Idle;
    private float _swingTimer = 0f;
    private const float WindUpDuration = 0.15f;
    private const float SlashDuration = 0.1f;
    private float _baseRotation = 0f;
    private bool _swingClockwise = true;
    private string _currentAnimation = "Idle";

    // Track the region we successfully bound to (important when names vary)
    private readonly string _resolvedRegionName;

    public bool IsSlashing => _currentSwingState == SwingState.Slashing;

    // Keep SetAnimation before first usage to avoid any tooling/partial compilation issues
    public void SetAnimation(string animationName)
    {
        if (Sprite == null) return;

        string specificAnimationName = $"{Name}_{animationName}";
        string baseAnimationName = animationName;

        Animation newAnimation = null;

        if (_atlas.HasAnimation(specificAnimationName))
        {
            newAnimation = _atlas.GetAnimation(specificAnimationName);
        }
        else if (_atlas.HasAnimation(baseAnimationName))
        {
            newAnimation = _atlas.GetAnimation(baseAnimationName);
        }
        else
        {
            try
            {
                var fallbackKey = _resolvedRegionName ?? Name;
                var fallbackRegion = _atlas.GetRegion(fallbackKey);
                newAnimation = new Animation(new List<TextureRegion> { fallbackRegion }, TimeSpan.FromSeconds(0.2));
            }
            catch
            {
                Log.Warn(LogArea.Weapon, $"[Weapon] No animation/region fallback for '{Name}'.");
                return;
            }
        }

        if (Sprite.Animation != newAnimation)
        {
            Sprite.Animation = newAnimation;
        }
        _currentAnimation = animationName;
    }

    public Weapon(string name, WeaponStats stats, TextureAtlas atlas, TextureAtlas projectileAtlas)
    {
        Name = name;
        _atlas = atlas;
        _projectileAtlas = projectileAtlas;
        Damage = stats.BaseDamage;

        try
        {
            if (!TryResolveRegion(_atlas, name, out var resolvedName, out var region))
            {
                Log.Warn(LogArea.Weapon, $"[Weapon] Region not found in atlas for '{name}'.");
                return;
            }

            _resolvedRegionName = resolvedName;
            var animation = new Animation(new List<TextureRegion> { region }, TimeSpan.FromSeconds(0.2));

            Sprite = new AnimatedSprite(animation)
            {
                Origin = new Vector2(region.Width / 2f, region.Height)
            };

            var s = stats?.Scale ?? 1f;
            if (s <= 0f) s = 1f;
            Scale = s;

            Length = region.Height * Scale;
            SetAnimation("Idle");

            Log.Info(LogArea.Weapon, $"[Weapon] Using atlas region '{_resolvedRegionName}' for '{Name}'.");
        }
        catch (Exception ex)
        {
            Log.Error(LogArea.Weapon, $"[Weapon] Exception creating weapon '{name}': {ex.Message}");
        }
    }

    public void PlayLevelUpEffect(SoundEffect soundEffect = null)
    {
        Sprite?.Flash(Color.Yellow, 0.5);
        soundEffect?.Play();
    }

    public void GainXP(int amount, SoundEffect levelUpSound = null)
    {
        XP += amount;
        while (XP >= XpToNextLevel)
        {
            XP -= XpToNextLevel;
            Level++;
            Damage++;
            PlayLevelUpEffect(levelUpSound);
        }
    }

    public void Update(GameTime gameTime)
    {
        Sprite?.Update(gameTime);

        if (_currentSwingState == SwingState.Idle) return;

        _swingTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
        float direction = _swingClockwise ? 1 : -1;

        if (_currentSwingState == SwingState.WindingUp)
        {
            if (_swingTimer >= WindUpDuration)
            {
                _swingTimer = 0f;
                _currentSwingState = SwingState.Slashing;
            }
            else
            {
                float windUpAngle = MathHelper.PiOver2;
                Rotation = MathHelper.Lerp(_baseRotation, _baseRotation - windUpAngle * direction, _swingTimer / WindUpDuration);
            }
        }
        else if (_currentSwingState == SwingState.Slashing)
        {
            if (_swingTimer >= SlashDuration)
            {
                _currentSwingState = SwingState.Idle;
                Rotation = _baseRotation;
                SetAnimation("Idle");
            }
            else
            {
                float startAngle = _baseRotation - MathHelper.PiOver2 * direction;
                float endAngle = _baseRotation + MathHelper.PiOver2 * direction;
                Rotation = MathHelper.Lerp(startAngle, endAngle, _swingTimer / SlashDuration);
            }
        }
    }

    public void Draw(SpriteBatch spriteBatch, Vector2 ownerCenterPosition)
    {
        if (Sprite == null) return;

        // Calculate the final position by applying offsets to the owner's center.
        Position = ownerCenterPosition + Offset + ManualOffset;

        // Set sprite position using its origin
        Sprite.Position = Position;

        // Icons face ~45° in the sheet, compensate visually
        Sprite.Rotation = Rotation + ArtRotationOffset;

        Sprite.Draw(spriteBatch, Sprite.Position);
    }

    public List<Vector2> GetTransformedHitPolygon(Vector2 ownerCenter)
    {
        var transformed = new List<Vector2>(HitPolygon?.Count ?? 0);
        if (HitPolygon == null || HitPolygon.Count == 0) return transformed;

        // The hit polygon should be centered at the same position as the sprite
        Vector2 origin = ownerCenter + Offset + ManualOffset;

        // Use the actual sprite rotation for the hitbox
        float totalRotation = Sprite != null ? Sprite.Rotation : (Rotation + ArtRotationOffset);

        foreach (var pt in HitPolygon)
        {
            var scaled = pt * Scale;
            var rotated = Vector2.Transform(scaled, Matrix.CreateRotationZ(totalRotation));
            transformed.Add(origin + rotated);
        }
        return transformed;
    }

    public Vector2 GetWorldOrigin(Vector2 ownerCenter)
    {
        // Return the actual position where the weapon is drawn
        return ownerCenter + Offset + ManualOffset;
    }

    public void DrawHitPolygon(SpriteBatch spriteBatch, Texture2D pixel, Vector2 ownerCenter, Color color)
    {
        var poly = GetTransformedHitPolygon(ownerCenter);
        for (int i = 0; i < poly.Count; i++)
        {
            var a = poly[i];
            var b = poly[(i + 1) % poly.Count];
            DrawLine(spriteBatch, pixel, a, b, color);
        }
    }

    // Unified debug/damage visualization parameters
    public readonly struct SwingDebugSpec
    {
        public readonly float StartAngle;
        public readonly float EndAngle;
        public readonly float HandleOffset;
        public readonly float BladeLength;
        public readonly float Thickness;

        public SwingDebugSpec(float startAngle, float endAngle, float handleOffset, float bladeLength, float thickness)
        {
            StartAngle = startAngle;
            EndAngle = endAngle;
            HandleOffset = handleOffset;
            BladeLength = bladeLength;
            Thickness = thickness;
        }
    }

    /// <summary>
    /// Returns a unified set of parameters for drawing/debugging the swing arc.
    /// </summary>
    public SwingDebugSpec GetDebugSwingSpec(float? halfArcOverrideRadians = null)
    {
        float handleOffset = DefaultHandleOffset;
        float bladeLength = MathF.Max(0f, Length - handleOffset);
        float thickness = DefaultHitThickness;

        float halfArc = halfArcOverrideRadians ?? MathHelper.PiOver2;

        // Center arc on the blade axis, not the art's X-axis
        // Blade points along (0,-1) in local, so subtract Pi/2 from the sprite rotation
        float bladeAxis = (Sprite != null ? Sprite.Rotation : (Rotation + ArtRotationOffset)) - MathHelper.PiOver2;

        float start = bladeAxis - halfArc;
        float end = bladeAxis + halfArc;

        return new SwingDebugSpec(start, end, handleOffset, bladeLength, thickness);
    }

    private void DrawLine(SpriteBatch spriteBatch, Texture2D pixel, Vector2 a, Vector2 b, Color color)
    {
        var distance = Vector2.Distance(a, b);
        var angle = (float)Math.Atan2(b.Y - a.Y, b.X - a.X);
        spriteBatch.Draw(pixel, a, null, color, angle, Vector2.Zero, new Vector2(distance, 1), SpriteEffects.None, 0);
    }

    public void StartSwing(bool clockwise)
    {
        if (_currentSwingState == SwingState.Idle && Sprite != null)
        {
            _currentSwingState = SwingState.WindingUp;
            _swingTimer = 0f;
            _swingClockwise = clockwise;
            _baseRotation = Rotation;
            SetAnimation("Swing");
        }
    }

    public Projectile Fire(Vector2 direction, Vector2 spawnPosition)
    {
        if (_projectileAtlas == null) return null;

        var projectileAnimation = _projectileAtlas.GetAnimation("Arrow-Wooden-Attack");
        if (projectileAnimation == null) return null;

        var dir = direction;
        if (dir.LengthSquared() < 0.0001f) dir = new Vector2((float)Math.Cos(Rotation), (float)Math.Sin(Rotation));
        dir.Normalize();

        var velocity = dir * 500f;
        return new Projectile(projectileAnimation, spawnPosition, velocity, Damage);
    }

    // Robustly resolve a region considering common naming variations (hyphen/underscore/space)
    private static bool TryResolveRegion(TextureAtlas atlas, string baseKey, out string resolvedName, out TextureRegion region)
    {
        resolvedName = null;
        region = null;

        if (string.IsNullOrWhiteSpace(baseKey)) return false;

        var candidates = new List<string>
        {
            baseKey,
            baseKey.Trim(),
            baseKey.Replace(' ', '_'),
            baseKey.Replace(' ', '-'),
            baseKey.Replace('_', '-'),
            baseKey.Replace('-', '_')
        };

        foreach (var key in candidates)
        {
            try
            {
                var r = atlas.GetRegion(key);
                if (r != null)
                {
                    resolvedName = key;
                    region = r;
                    return true;
                }
            }
            catch { }
        }

        return false;
    }
}