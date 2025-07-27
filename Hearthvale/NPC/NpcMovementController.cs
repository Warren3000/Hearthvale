using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace Hearthvale;
public class NpcMovementController
{
    private Vector2 _velocity;
    private float _speed;
    private Rectangle _bounds;
    private float _directionChangeTimer;
    private float _idleTimer;
    private bool _isIdle;
    private readonly Random _random = new();

    public Vector2 Position { get; private set; }
    public bool IsIdle => _isIdle;

    public NpcMovementController(Vector2 startPosition, float speed, Rectangle bounds)
    {
        Position = startPosition;
        _speed = speed;
        _bounds = bounds;
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
        _velocity = Vector2.Zero;
        _isIdle = true;
        _idleTimer = 1f + (float)_random.NextDouble() * 2f;
    }

    public void Update(float elapsed, Func<Vector2, bool> collisionCheck)
    {
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
                new Vector2(_bounds.Left, _bounds.Top),
                new Vector2(_bounds.Right, _bounds.Bottom)
            );
            _directionChangeTimer -= elapsed;
            if (_directionChangeTimer <= 0)
                SetIdle();
        }
    }

    public void SetPosition(Vector2 pos) => Position = pos;
    public Vector2 GetVelocity() => _velocity;
    public void SetVelocity(Vector2 v) => _velocity = v;
}