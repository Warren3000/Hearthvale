using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary;
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
    private float _defeatTimer = 0f;
    private float _stunTimer = 0f;
    public bool IsReadyToRemove { get; private set; } = false;

    public string DialogText { get; set; } = "Hello, adventurer!";

    private Random _random = new Random();

    // --- Flash effect fields ---
    private float _flashTimer = 0f;
    private static readonly Color HitFlashColor = Color.Red;
    private static readonly float FlashDuration = 0.15f;
    private Color _originalColor = Color.White;

    public Rectangle Bounds => new Rectangle(
        (int)Position.X+8,
        (int)Position.Y+16,
        (int)Sprite.Width/2,
        (int)Sprite.Height/2
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
        _originalColor = Sprite.Color;

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

        // Handle flash effect
        if (_flashTimer > 0)
        {
            _flashTimer -= elapsed;
            if (_flashTimer <= 0)
            {
                Sprite.Color = _originalColor;
            }
        }

        if (_isDefeated)
        {
            if (_defeatTimer > 0)
            {
                _defeatTimer -= elapsed;
                if (_defeatTimer <= 0)
                {
                    IsReadyToRemove = true; // Mark for removal after defeat animation
                }
            }
            Sprite.Update(gameTime);
            return;
        }

        if (_stunTimer > 0)
        {
            _stunTimer -= elapsed;
            Position += _velocity * elapsed * 30f; // move with knockback
            _velocity *= 0.8f; // dampen knockback
            Sprite.Update(gameTime);
            // Early return while stunned
            return;
        }
        else if (_currentAnimationName == "Hit")
        {
            // After stun, return to idle or walk
            if (_isIdle)
            {
                if (_animations.ContainsKey("Idle") && _animations["Idle"] != null)
                {
                    Sprite.Animation = _animations["Idle"];
                    _currentAnimationName = "Idle";
                }
            }
            else
            {
                if (_animations.ContainsKey("Walk") && _animations["Walk"] != null)
                {
                    Sprite.Animation = _animations["Walk"];
                    _currentAnimationName = "Walk";
                }
            }
        }

        if (_isIdle)
        {
            _idleTimer -= elapsed;
            if (_idleTimer <= 0)
            {
                _isIdle = false;
                SetRandomDirection();
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

    // Call this when hit for a flash effect
    public void Flash()
    {
        Sprite.Color = HitFlashColor;
        _flashTimer = FlashDuration;
    }

    public void TakeDamage(int amount, Vector2? knockback = null)
    {
        if (IsDefeated) return;
        _currentHealth -= amount;
        if (knockback.HasValue)
            _velocity = knockback.Value;
        _stunTimer = 0.3f; // 0.3 seconds stun

        Flash(); // Trigger visual feedback

        // Set hit animation if available
        if (_animations.ContainsKey("Hit") && _animations["Hit"] != null)
        {
            Sprite.Animation = _animations["Hit"];
            _currentAnimationName = "Hit";
        }

        if (_currentHealth <= 0)
        {
            _currentHealth = 0;
            _isDefeated = true;
            OnDefeated();
        }
    }

    private void OnDefeated()
    {
        _isDefeated = true;

        // Check for "Defeated" animation
        if (_animations.ContainsKey("Defeated") && _animations["Defeated"] != null)
        {
            Sprite.Animation = _animations["Defeated"];
            _currentAnimationName = "Defeated";
            _defeatTimer = 1.0f; // seconds to show defeat animation
        }
        else
        {
            // No animation: remove immediately
            IsReadyToRemove = true;
        }

        Core.Audio.PlaySoundEffect(Core.Content.Load<SoundEffect>("audio/npc_defeat"));
    }
}
