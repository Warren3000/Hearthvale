using Microsoft.Xna.Framework;
using System;

namespace Hearthvale.GameCode.Entities.NPCs;
public class NpcMovementController
{
    private Vector2 _velocity;
    private float _speed;
    private float _directionChangeTimer;
    private float _idleTimer;
    private bool _isIdle;
    private readonly Random _random = new();
    public Rectangle Bounds;
    public Vector2 Position { get; set; }
    public bool IsIdle => _isIdle;

    // Knockback support
    private float _knockbackTimer = 0f;
    private const float KnockbackDuration = 0.2f; // seconds

    public NpcMovementController(Vector2 startPosition, float speed, Rectangle bounds)
    {
        Position = startPosition;
        _speed = speed;
        Bounds = bounds;
    }

    public void SetRandomDirection()
    {
        float angle = (float)(_random.NextDouble() * Math.PI * 2);
        _velocity = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * _speed;
        _directionChangeTimer = 2f + (float)_random.NextDouble() * 3f;
        _isIdle = false;
    }

    public void SetIdle()
    {
        // Only set idle if not in knockback
        if (_knockbackTimer <= 0f)
        {
            _velocity = Vector2.Zero;
            _isIdle = true;
            _idleTimer = 1f + (float)_random.NextDouble() * 2f;
        }
    }

    public void Update(float elapsed, Func<Vector2, bool> collisionCheck)
    {
        // Handle knockback first, as it's a forced movement
        if (_knockbackTimer > 0f)
        {
            _knockbackTimer -= elapsed;
            Vector2 nextPosition = Position + _velocity * elapsed;
            
            // During knockback, only stop if colliding. Don't zero out velocity yet.
            if (!collisionCheck(nextPosition))
            {
                Position = Vector2.Clamp(
                    nextPosition,
                    new Vector2(Bounds.Left, Bounds.Top),
                    new Vector2(Bounds.Right, Bounds.Bottom)
                );
            }

            if (_knockbackTimer <= 0f)
            {
                _velocity = Vector2.Zero; // Reset velocity only when timer expires
                SetIdle(); // Transition to idle state after knockback
            }
            return; // IMPORTANT: Skip normal AI movement during knockback
        }

        // Normal AI movement logic
        if (_isIdle)
        {
            _idleTimer -= elapsed;
            if (_idleTimer <= 0)
                SetRandomDirection();
        }
        else
        {
            Vector2 nextPosition = Position + _velocity * elapsed;
            if (collisionCheck(nextPosition))
            {
                SetIdle();
                return;
            }
            Position = Vector2.Clamp(
                nextPosition,
                new Vector2(Bounds.Left, Bounds.Top),
                new Vector2(Bounds.Right, Bounds.Bottom)
            );
            _directionChangeTimer -= elapsed;
            if (_directionChangeTimer <= 0)
                SetIdle();
        }
    }

    public void SetPosition(Vector2 pos) => Position = pos;
    public Vector2 GetVelocity() => _velocity;
    public void SetVelocity(Vector2 v)
    {
        _velocity = v;
        _knockbackTimer = KnockbackDuration;
        _isIdle = false;
    }
}