using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;

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
    public int Level { get; private set; }
    public int XP { get; private set; }
    public int Damage { get; private set; }
    public float Length { get; private set; }
    public float Scale
    {
        get => Sprite.Scale.X; // Assuming uniform scaling
        set
        {
            Sprite.Scale = new Vector2(value, value);
            // Update the weapon's effective length when its scale changes
            Length = Sprite.Region.Height * value;
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

    public Weapon(string name, int baseDamage, TextureAtlas atlas)
    {
        Name = name;
        _atlas = atlas;
        Damage = baseDamage;
        // For single-frame weapons, create an animation with one frame
        var region = atlas.GetRegion(name);
        var animation = new Animation(
            new List<TextureRegion> { region },
            TimeSpan.FromSeconds(0.2)
        );
        Sprite = new AnimatedSprite(animation);
        Sprite.Origin = new Vector2(0, Sprite.Region.Height);
        // Set initial length. This will be updated if scale is changed.
        Length = Sprite.Region.Height;
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

    public void Draw(SpriteBatch spriteBatch, Vector2 playerPosition)
    {
        // Center of the player sprite
        Vector2 playerCenter = playerPosition + new Vector2(Sprite.Width / 2f, Sprite.Height / 1.4f);

        // Final weapon position
        Vector2 finalPosition = playerCenter + Offset + ManualOffset;

        Sprite.Position = finalPosition;
        Sprite.Rotation = Rotation;
        Sprite.Draw(spriteBatch, Sprite.Position);
    }
    public Projectile Fire(Vector2 direction, Vector2 spawnPosition)
    {
        var projectileTexture = _atlas.GetRegion("Dagger");
        var velocity = Vector2.Normalize(direction) * 500f; // 500f is projectile speed
        // Use the provided spawn position, which will be the player's center
        return new Projectile(projectileTexture, spawnPosition, velocity, Damage);
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
        if (_atlas.HasAnimation(animationName) && _currentAnimation != animationName)
        {
            Sprite.Animation = _atlas.GetAnimation(animationName);
            _currentAnimation = animationName;
        }
    }

    public void GainXP(int amount)
    {
        XP += amount;
        if (XP >= XPToNextLevel())
        {
            LevelUp();
        }
    }

    private void LevelUp()
    {
        Level++;
        XP = 0;
        Damage += 2; // Example increment
    }

    private int XPToNextLevel() => Level * 10;
}