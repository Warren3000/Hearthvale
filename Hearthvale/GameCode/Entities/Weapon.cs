using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Hearthvale.GameCode.Data;
using Microsoft.Xna.Framework.Audio;
using Hearthvale.GameCode.Managers;

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
    private readonly TextureAtlas _projectileAtlas; // Add this line
    public int Level { get; private set; } = 1;
    public int XP { get; private set; }
    public int XpToNextLevel => 10 * (Level + 1);
    public int Damage { get; private set; }
    public float Length { get; private set; }
    public float Scale
    {
        get => Sprite != null ? Sprite.Scale.X : 1.0f; // Default to 1.0f if Sprite is null
        set
        {
            if (Sprite != null)
            {
                Sprite.Scale = new Vector2(value, value);
                Length = Sprite.Region.Height * value;
            }
        }
    }

    // Swing animation properties
    private enum SwingState { Idle, WindingUp, Slashing }
    private SwingState _currentSwingState = SwingState.Idle;
    private float _swingTimer = 0f;
    private const float WindUpDuration = 0.15f;
    private const float SlashDuration = 0.1f;
    private float _baseRotation = 0f;
    private bool _swingClockwise = true;
    private string _currentAnimation = "Idle";
    
    public bool IsSlashing => _currentSwingState == SwingState.Slashing;

    
    public Weapon(string name, WeaponStats stats, TextureAtlas atlas, TextureAtlas projectileAtlas)
    {
        Name = name;
        _atlas = atlas;
        _projectileAtlas = projectileAtlas;
        Damage = stats.BaseDamage;

        try
        {
            var region = atlas.GetRegion(name);
            var animation = new Animation(new List<TextureRegion> { region }, TimeSpan.FromSeconds(0.2));

            Sprite = new AnimatedSprite(animation)
            {
                Origin = new Vector2(0, region.Height)
            };
            Scale = stats.Scale;
            Length = region.Height * Scale; // Apply scale to length

            SetAnimation("Idle");
        }
        catch (Exception ex)
        {
            // If the region isn't found, Sprite will be null. We should handle this gracefully.
        }
    }

    /// <summary>
    /// Triggers a visual flash and plays a sound effect when the weapon levels up.
    /// </summary>
    public void PlayLevelUpEffect(SoundEffect? soundEffect = null)
    {
        Sprite?.Flash(Color.Yellow, 0.5); // Flash yellow for 0.2 seconds

        // Play sound effect if provided
        soundEffect?.Play();
    }

    public void GainXP(int amount, SoundEffect? levelUpSound = null)
    {
        XP += amount;
        while (XP >= XpToNextLevel)
        {
            XP -= XpToNextLevel;
            Level++;
            Damage++; // Increase damage by 1 on level up

            // Play level-up effect (flash + sound)
            PlayLevelUpEffect(levelUpSound);
        }
    }
    public void Update(GameTime gameTime)
    {
        Sprite.Update(gameTime);

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
                // Pulls the weapon back
                float windUpAngle = MathHelper.PiOver2; // 90 degrees back
                Rotation = MathHelper.Lerp(_baseRotation, _baseRotation - (windUpAngle * direction), _swingTimer / WindUpDuration);
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
                // Swings the weapon forward in an arc
                float startAngle = _baseRotation - (MathHelper.PiOver2 * direction);
                float endAngle = _baseRotation + (MathHelper.PiOver2 * direction);
                Rotation = MathHelper.Lerp(startAngle, endAngle, _swingTimer / SlashDuration);
            }
        }
    }

    public void Draw(SpriteBatch spriteBatch, Vector2 ownerCenterPosition)
    {
        if (Sprite == null) return;

        // The weapon's position is now calculated relative to its owner's center.
        Position = ownerCenterPosition + Offset + ManualOffset;
        Sprite.Position = Position;
        Sprite.Rotation = Rotation;
        Sprite.Draw(spriteBatch, Sprite.Position);
    }
    public Projectile Fire(Vector2 direction, Vector2 spawnPosition)
    {
        if (_projectileAtlas == null)
        {
            return null;
        }

        // Use the animation from the projectile atlas
        var projectileAnimation = _projectileAtlas.GetAnimation("Arrow-Wooden-Attack");
        
        if (projectileAnimation == null)
        {
            return null;
        }

        var velocity = Vector2.Normalize(direction) * 500f; // 500f is projectile speed
        // Use the provided spawn position, which will be the player's center
        return new Projectile(projectileAnimation, spawnPosition, velocity, Damage);
    }

    public void StartSwing(bool clockwise)
    {
        if (_currentSwingState == SwingState.Idle)
        {
            _currentSwingState = SwingState.WindingUp;
            _swingTimer = 0f;
            _swingClockwise = clockwise;
            _baseRotation = Rotation;
            SetAnimation("Swing");
        }
    }

    public void SetAnimation(string animationName)
    {
        // Ensure sprite exists before trying to set an animation.
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
                var fallbackRegion = _atlas.GetRegion(Name);
                newAnimation = new Animation(new List<TextureRegion> { fallbackRegion }, TimeSpan.FromSeconds(0.2));
            }
            catch (Exception ex)
            {
                return; // Exit if we can't find any texture
            }
        }

        if (Sprite.Animation != newAnimation)
        {
            Sprite.Animation = newAnimation;
        }
        _currentAnimation = animationName; // Keep track of the logical state, e.g., "Idle", "Swing"
    }
}