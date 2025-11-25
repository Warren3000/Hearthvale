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
    
    private float _baseScale = 1.0f;
    
    // Dynamic hitbox generated from texture
    private List<Vector2> _generatedHitPolygon;
    private Rectangle _opaqueRegionBounds;
    
    public List<Vector2> HitPolygon 
    { 
        get => _generatedHitPolygon ?? GetDefaultHitPolygon();
        private set => _generatedHitPolygon = value;
    }
    
    // Fallback hitbox if generation fails
    private List<Vector2> GetDefaultHitPolygon()
    {
        return new List<Vector2>
        {
            new Vector2(0, 0),      // handle/base
            new Vector2(0, -20),    // tip
            new Vector2(8, -16),    // right edge
            new Vector2(8, -8),     // right base
            new Vector2(-8, -8),    // left base
            new Vector2(-8, -16),   // left edge
        };
    }

    // Visual art faces ~45° and is drawn upright; compensate consistently everywhere
    public static readonly float ArtRotationOffset = MathHelper.Pi;
    private const float HitPolygonRotationOffset = MathHelper.Pi + MathHelper.PiOver2;

    private enum SwingState { Idle, WindingUp, Slashing, Recovering }
    private SwingState _currentSwingState = SwingState.Idle;
    private float _swingTimer = 0f;
    private float _currentWindUpDuration;
    private float _currentSlashDuration;
    private float _currentRecoveryDuration;
    private float _swingSpeedMultiplier = 1f;
    private float? _pendingSwingSpeedMultiplier;
    private float _baseRotation = 0f;
    private bool _swingClockwise = true;
    private float _swingDirectionSign = 1f;
    private string _currentAnimation = "Idle";
    private WeaponSwingProfile _currentSwingProfile = WeaponSwingProfile.Default;
    private WeaponSwingProfile _pendingSwingProfile;
    private float _currentWindUpAngle;
    private float _currentSlashArc;
    private float _currentRecoveryAngle;
    private float _windUpTargetRotation;
    private float _activeEndRotation;
    private float _recoveryStartRotation;

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
            _baseScale = s;

            Length = region.Height * Scale;
            SetAnimation("Idle");
            ApplySwingProfile(WeaponSwingProfile.Default);

            // Generate a per-weapon hit polygon from the texture data so swing detection
            // reflects the actual animated blade rather than a generic arc.
            GenerateHitboxFromTexture(region);

            Log.Info(LogArea.Weapon, $"[Weapon] Using atlas region '{_resolvedRegionName}' for '{Name}'.");
        }
        catch (Exception ex)
        {
            Log.Error(LogArea.Weapon, $"[Weapon] Exception creating weapon '{name}': {ex.Message}");
        }
    }

    private void GenerateHitboxFromTexture(TextureRegion region)
    {
        if (region?.Texture == null)
        {
            return;
        }

        try
        {
            var options = new WeaponHitboxOptions
            {
                AlphaThreshold = 8,
                InflatePixels = 1,
                UseOrientedBoundingBox = true,
                PivotMode = HitboxPivotMode.BottomCenter,
                NormalizeYUp = true
            };

            var generatedPolygon = WeaponHitboxGenerator.GenerateBoundingPolygon(
                region.Texture,
                region.SourceRectangle,
                options,
                out _opaqueRegionBounds
            );

            if (generatedPolygon != null && generatedPolygon.Count >= 3)
            {
                HitPolygon = generatedPolygon;
                Log.Info(LogArea.Weapon, $"[Weapon] Generated hitbox for '{Name}' with {generatedPolygon.Count} vertices");
            }
            else
            {
                Log.Warn(LogArea.Weapon, $"[Weapon] Failed to generate hitbox for '{Name}', falling back to default polygon.");
            }
        }
        catch (Exception ex)
        {
            Log.Error(LogArea.Weapon, $"[Weapon] Exception generating hitbox for '{Name}': {ex.Message}");
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

        float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds * _swingSpeedMultiplier;
        _swingTimer += elapsed;
        switch (_currentSwingState)
        {
            case SwingState.WindingUp:
                if (_currentWindUpDuration <= float.Epsilon)
                {
                    SetSwingState(DeterminePostWindUpState());
                    return;
                }

                float windUpT = MathHelper.Clamp(_swingTimer / _currentWindUpDuration, 0f, 1f);
                Rotation = MathHelper.Lerp(_baseRotation, _windUpTargetRotation, windUpT);

                if (_swingTimer >= _currentWindUpDuration)
                {
                    SetSwingState(DeterminePostWindUpState());
                }
                break;

            case SwingState.Slashing:
                if (_currentSlashDuration <= float.Epsilon)
                {
                    SetSwingState(DeterminePostSlashState());
                    return;
                }

                float slashT = MathHelper.Clamp(_swingTimer / _currentSlashDuration, 0f, 1f);
                Rotation = MathHelper.Lerp(_windUpTargetRotation, _activeEndRotation, slashT);

                if (_swingTimer >= _currentSlashDuration)
                {
                    SetSwingState(DeterminePostSlashState());
                }
                break;

            case SwingState.Recovering:
                if (_currentRecoveryDuration <= float.Epsilon)
                {
                    SetSwingState(SwingState.Idle);
                    return;
                }

                float recoveryT = MathHelper.Clamp(_swingTimer / _currentRecoveryDuration, 0f, 1f);
                Rotation = MathHelper.Lerp(_recoveryStartRotation, _baseRotation, recoveryT);

                if (_swingTimer >= _currentRecoveryDuration)
                {
                    SetSwingState(SwingState.Idle);
                }
                break;
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

    public List<Vector2> GetTransformedHitPolygon(Vector2 ownerCenter)
    {
        var transformed = new List<Vector2>(HitPolygon?.Count ?? 0);
        if (HitPolygon == null || HitPolygon.Count == 0)
            return transformed;

        Vector2 origin = ownerCenter + Offset + ManualOffset;
        float uniformScale = Scale;
        float totalRotation = Rotation + HitPolygonRotationOffset;
        Matrix rotationMatrix = Matrix.CreateRotationZ(totalRotation);

        foreach (var pt in HitPolygon)
        {
            var scaled = new Vector2(pt.X * uniformScale, pt.Y * uniformScale);
            var rotated = Vector2.Transform(scaled, rotationMatrix);
            transformed.Add(origin + rotated);
        }
        return transformed;
    }

    public Vector2 GetWorldOrigin(Vector2 ownerCenter)
    {
        // Return the actual position where the weapon is drawn
        return ownerCenter + Offset + ManualOffset;
    }

    // Debug method to draw the opaque bounds (useful for debugging hitbox generation)
    public void DrawOpaqueRegionBounds(SpriteBatch spriteBatch, Texture2D pixel, Vector2 ownerCenter, Color color)
    {
        if (_opaqueRegionBounds.IsEmpty) return;
        
        var worldOrigin = GetWorldOrigin(ownerCenter);
        var scaledBounds = new Rectangle(
            (int)(worldOrigin.X + _opaqueRegionBounds.X * Scale - _opaqueRegionBounds.Width * Scale / 2),
            (int)(worldOrigin.Y - _opaqueRegionBounds.Y * Scale - _opaqueRegionBounds.Height * Scale),
            (int)(_opaqueRegionBounds.Width * Scale),
            (int)(_opaqueRegionBounds.Height * Scale)
        );
        
        WeaponHitboxGenerator.DrawRectangleOutline(spriteBatch, pixel, scaledBounds, color);
    }

    private void DrawLine(SpriteBatch spriteBatch, Texture2D pixel, Vector2 a, Vector2 b, Color color)
    {
        var distance = Vector2.Distance(a, b);
        var angle = (float)Math.Atan2(b.Y - a.Y, b.X - a.X);
        spriteBatch.Draw(pixel, a, null, color, angle, Vector2.Zero, new Vector2(distance, 1), SpriteEffects.None, 0);
    }

    private void ApplySwingProfile(WeaponSwingProfile profile)
    {
        profile ??= WeaponSwingProfile.Default;
        _currentSwingProfile = profile;
        _currentWindUpDuration = profile.WindUpDuration;
        _currentSlashDuration = profile.ActiveDuration;
        _currentRecoveryDuration = profile.RecoveryDuration;
        _currentWindUpAngle = profile.WindUpAngleRadians;
        _currentSlashArc = profile.SlashArcRadians;
        _currentRecoveryAngle = profile.RecoveryAngleRadians;
        Scale = _baseScale * profile.WeaponLengthScale;
    }

    private void InitializeSwingRotations()
    {
        _windUpTargetRotation = _baseRotation - _currentWindUpAngle * _swingDirectionSign;
        _activeEndRotation = _windUpTargetRotation + _currentSlashArc * _swingDirectionSign;
        _recoveryStartRotation = _currentRecoveryDuration > float.Epsilon
            ? _activeEndRotation + _currentRecoveryAngle * _swingDirectionSign
            : _baseRotation;
    }

    private SwingState DeterminePostWindUpState()
    {
        if (_currentSlashDuration > float.Epsilon)
        {
            return SwingState.Slashing;
        }

        if (_currentRecoveryDuration > float.Epsilon)
        {
            return SwingState.Recovering;
        }

        return SwingState.Idle;
    }

    private SwingState DeterminePostSlashState()
    {
        if (_currentRecoveryDuration > float.Epsilon)
        {
            return SwingState.Recovering;
        }

        return SwingState.Idle;
    }

    private void SetSwingState(SwingState newState)
    {
        if (_currentSwingState == newState && newState != SwingState.Idle)
        {
            _swingTimer = 0f;
            return;
        }

        _currentSwingState = newState;
        _swingTimer = 0f;

        switch (newState)
        {
            case SwingState.Idle:
                Rotation = _baseRotation;
                SetAnimation("Idle");
                _swingSpeedMultiplier = 1f;
                Scale = _baseScale;
                break;
            case SwingState.WindingUp:
                Rotation = _baseRotation;
                if (_currentWindUpDuration <= float.Epsilon)
                {
                    SetSwingState(DeterminePostWindUpState());
                }
                break;
            case SwingState.Slashing:
                Rotation = _windUpTargetRotation;
                if (_currentSlashDuration <= float.Epsilon)
                {
                    SetSwingState(DeterminePostSlashState());
                }
                break;
            case SwingState.Recovering:
                Rotation = _recoveryStartRotation;
                if (_currentRecoveryDuration <= float.Epsilon)
                {
                    SetSwingState(SwingState.Idle);
                }
                break;
        }
    }

    public void StartSwing(bool clockwise)
    {
        if (_currentSwingState != SwingState.Idle || Sprite == null)
            return;

        var profileToApply = _pendingSwingProfile ?? _currentSwingProfile ?? WeaponSwingProfile.Default;
        ApplySwingProfile(profileToApply);
        _pendingSwingProfile = null;

        _swingClockwise = clockwise;
        _swingDirectionSign = _swingClockwise ? 1f : -1f;
        _baseRotation = Rotation;

        if (_pendingSwingSpeedMultiplier.HasValue)
        {
            _swingSpeedMultiplier = MathF.Max(0.01f, _pendingSwingSpeedMultiplier.Value);
            _pendingSwingSpeedMultiplier = null;
        }
        else
        {
            _swingSpeedMultiplier = 1f;
        }

        SetAnimation("Swing");
        InitializeSwingRotations();
        SetSwingState(SwingState.WindingUp);
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

    public void SetNextSwingSpeedMultiplier(float multiplier)
    {
        if (multiplier <= 0f)
        {
            multiplier = 0.01f;
        }

        _pendingSwingSpeedMultiplier = multiplier;
    }

    public void SetNextSwingProfile(WeaponSwingProfile profile)
    {
        _pendingSwingProfile = profile ?? WeaponSwingProfile.Default;
    }
}