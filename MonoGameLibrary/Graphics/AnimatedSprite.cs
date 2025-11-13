using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace MonoGameLibrary.Graphics;

public class AnimatedSprite : Sprite
{
    private int _currentFrame;
    private TimeSpan _elapsed;
    private Animation _animation;
    public Vector2 Position { get; set; }
    private Color? _originalColor = null;
    private double _flashTimer = 0;
    public bool IsLooping { get; set; } = true;
    public bool IsAnimationComplete { get; private set; }

    /// <summary>
    /// Gets or Sets the animation for this animated sprite.
    /// </summary>
    public Animation Animation
    {
        get => _animation;
        set
        {
            _animation = value ?? throw new ArgumentNullException(nameof(value));
            _currentFrame = 0;
            _elapsed = TimeSpan.Zero;
            IsAnimationComplete = false;

            if (_animation.Frames.Count > 0)
            {
                Region = _animation.Frames[0];
            }
        }
    }

    /// <summary>
    /// Creates a new animated sprite.
    /// </summary>
    public AnimatedSprite() { }

    /// <summary>
    /// Creates a new animated sprite with the specified frames and delay.
    /// </summary>
    /// <param name="animation">The animation for this animated sprite.</param>
    public AnimatedSprite(Animation animation)
    {
        Animation = animation;
    }

    /// <summary>
    /// Updates this animated sprite.
    /// </summary>
    /// <param name="gameTime">A snapshot of the game timing values provided by the framework.</param>
    public void Update(GameTime gameTime)
    {
        if (_animation == null || _animation.Frames.Count == 0)
        {
            return;
        }

        if (!IsLooping && IsAnimationComplete)
        {
            return;
        }

        _elapsed += gameTime.ElapsedGameTime;

        while (_elapsed >= _animation.Delay)
        {
            _elapsed -= _animation.Delay;
            _currentFrame++;

            if (_currentFrame >= _animation.Frames.Count)
            {
                if (IsLooping)
                {
                    _currentFrame = 0;
                }
                else
                {
                    _currentFrame = _animation.Frames.Count - 1;
                    IsAnimationComplete = true;
                    break;
                }
            }
        }

        Region = _animation.Frames[_currentFrame];

        // Handle flash timer
        if (_flashTimer > 0)
        {
            _flashTimer -= gameTime.ElapsedGameTime.TotalSeconds;
            if (_flashTimer <= 0 && _originalColor != null)
            {
                this.Color = _originalColor.Value;
                _originalColor = null;
            }
        }
    }
    public void Flash(Color? flashColor = null, double duration = 0.2)
    {
        if (_originalColor == null)
            _originalColor = this.Color;
        this.Color = flashColor ?? Color.Yellow;
        _flashTimer = duration;
    }

    /// <summary>
    /// Submit this animated sprite for drawing using its stored Position.
    /// </summary>
    /// <param name="spriteBatch">The SpriteBatch instance used for batching draw calls.</param>
    public void Draw(SpriteBatch spriteBatch)
    {
        base.Draw(spriteBatch, Position);
    }

    // The inherited Draw(SpriteBatch, Vector2) method is still available for manual positioning
}