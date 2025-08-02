using Microsoft.Xna.Framework;
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

    /// <summary>
    /// Gets or Sets the animation for this animated sprite.
    /// </summary>
    public Animation Animation
    {
        get => _animation;
        set
        {
            _animation = value;
            Region = _animation.Frames[0];
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
        _elapsed += gameTime.ElapsedGameTime;

        if (_elapsed >= _animation.Delay)
        {
            _elapsed -= _animation.Delay;
            _currentFrame++;

            if (_currentFrame >= _animation.Frames.Count)
            {
                _currentFrame = 0;
            }

            Region = _animation.Frames[_currentFrame];
        }

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
}