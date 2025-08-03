using Microsoft.Xna.Framework;
using MonoGameLibrary.Graphics;
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

    private Tilemap _tilemap;
    private int _wallTileId;
    private int _spriteWidth;
    private int _spriteHeight;

    public NpcMovementController(Vector2 startPosition, float speed, Rectangle bounds, Tilemap tilemap, int wallTileId, int spriteWidth, int spriteHeight)
    {
        Position = startPosition;
        _speed = speed;
        Bounds = bounds;
        _tilemap = tilemap;
        _wallTileId = wallTileId;
        _spriteWidth = spriteWidth;
        _spriteHeight = spriteHeight;
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
            if (collisionCheck(nextPosition) || IsWall(nextPosition))
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

    // Helper to check for wall collision
    private bool IsWall(Vector2 pos)
    {
        Rectangle candidateBounds = new Rectangle(
            (int)pos.X + 8,
            (int)pos.Y + 16,
            _spriteWidth / 2,
            _spriteHeight / 2
        );

        int leftTile = candidateBounds.Left / (int)_tilemap.TileWidth;
        int rightTile = (candidateBounds.Right - 1) / (int)_tilemap.TileWidth;
        int topTile = candidateBounds.Top / (int)_tilemap.TileHeight;
        int bottomTile = (candidateBounds.Bottom - 1) / (int)_tilemap.TileHeight;

        for (int col = leftTile; col <= rightTile; col++)
        {
            for (int row = topTile; row <= bottomTile; row++)
            {
                int tileId = _tilemap.GetTileId(col, row);
                if (tileId == _wallTileId)
                    return true; // Block movement if any part overlaps a wall
            }
        }
        return false;
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