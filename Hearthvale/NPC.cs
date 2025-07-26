using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;

public class NPC
{
    public AnimatedSprite Sprite;
    public Vector2 Position;
    private Dictionary<string, Animation> _animations;
    private Rectangle _bounds;
    private string _currentAnimationName;

    private Vector2 _velocity;
    private float _speed = 1.0f;

    private float _directionChangeTimer;
    private float _idleTimer;
    private bool _isIdle;

    private int _maxHealth = 10;
    private int _currentHealth;
    public int Health => _currentHealth;
    private bool _isDefeated = false;
    public bool IsDefeated => _isDefeated;
    private SoundEffect _defeatSound;


    private Random _random = new Random();

    public Rectangle Bounds => new Rectangle(
        (int)Position.X,
        (int)Position.Y,
        (int)Sprite.Width,
        (int)Sprite.Height
    );

    public bool FacingRight
    {
        get => Sprite.Effects != SpriteEffects.FlipHorizontally;
        set => Sprite.Effects = value ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
    }

    public NPC(Dictionary<string, Animation> animations, Vector2 position, Rectangle bounds, SoundEffect soundEffect)
    {
        _animations = animations;
        Position = position;
        _bounds = bounds;
        _currentHealth = _maxHealth;
        _defeatSound = soundEffect;

        // Initialize Sprite with the Idle animation
        Sprite = new AnimatedSprite(_animations["Idle"]);

        SetIdle();
    }

    private void SetRandomDirection()
    {
        if (_animations["Walk"] == null)
            throw new Exception("Walk animation is missing!");

        float angle = (float)(_random.NextDouble() * Math.PI * 2);
        _velocity = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * _speed;

        if (_velocity.X > 0.1f)
            FacingRight = true;
        else if (_velocity.X < -0.1f)
            FacingRight = false;

        _directionChangeTimer = 2f + (float)_random.NextDouble() * 3f;

        Sprite.Animation = _animations["Walk"];
        _currentAnimationName = "Walk";

    }

    private void SetIdle()
    {
        if (_animations["Idle"] == null)
        {
            Debug.WriteLine("ERROR: Idle animation is null!");
            return;
        }
        _velocity = Vector2.Zero;
        _isIdle = true;
        _idleTimer = 1f + (float)_random.NextDouble() * 2f;

        Sprite.Animation = _animations["Idle"];
        _currentAnimationName = "Idle";
    }

    public void Update(GameTime gameTime)
    {
        float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (_isIdle)
        {
            _idleTimer -= elapsed;
            if (_idleTimer <= 0)
            {
                _isIdle = false;
                SetRandomDirection();
                // Optionally switch sprite animation to "walk"
            }
        }
        else
        {
            Position += _velocity * elapsed * 60f;  // Move scaled by frame time

            Vector2 clampedPosition = Vector2.Clamp(
                Position,
                new Vector2(_bounds.Left, _bounds.Top),
                new Vector2(_bounds.Right - Sprite.Width, _bounds.Bottom - Sprite.Height)
            );

            Position = clampedPosition;

            _directionChangeTimer -= elapsed;
            if (_directionChangeTimer <= 0)
            {
                SetIdle();
            }
        }

        Sprite.Update(gameTime);
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        Sprite.Draw(spriteBatch, Position);
    }

    public void TakeDamage(int amount)
    {
        if (IsDefeated) return;
        _currentHealth -= amount;
        if (_currentHealth <= 0)
        {
            _currentHealth = 0;
            OnDefeated();
        }
    }
    private void OnDefeated()
    {
        _isDefeated = true;

        // Example: Play defeat animation if you have one
        if (_animations.ContainsKey("Defeated") && _animations["Defeated"] != null)
        {
            Sprite.Animation = _animations["Defeated"];
            _currentAnimationName = "Defeated";
        }

        // Example: Play a sound effect (pseudo-code, depends on your audio system)
        // Core.Audio.PlaySoundEffect(Core.Content.Load<SoundEffect>("audio/npc_defeat"));

        // Optionally: spawn particles or effects here
    }
}