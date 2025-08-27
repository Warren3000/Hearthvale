//using Hearthvale.GameCode.Managers;
//using Hearthvale.GameCode.Utils;
//using Microsoft.Xna.Framework;
//using MonoGame.Extended.Tiled;
//using MonoGameLibrary.Graphics;
//using System;

//namespace Hearthvale.GameCode.Entities.Components;
//public class NpcMovementComponent
//{
//    private Vector2 _velocity;
//    private float _speed;
//    private float _directionChangeTimer;
//    private float _idleTimer;
//    private bool _isIdle;
//    private readonly Random _random = new();
//    public Vector2 Position { get; set; }
//    public bool IsIdle => _isIdle;

//    // AI chase support
//    private Vector2? _chaseTarget = null;
//    public Vector2? ChaseTarget => _chaseTarget; // Expose for debug
//    private float _chaseSpeed = 40f;

//    public NpcMovementComponent(Vector2 startPosition, float speed)
//    {
//        Position = startPosition;
//        _speed = speed;
//    }

//    /// <summary>
//    /// Set a target position to chase (e.g., the player's position).
//    /// If null, NPC will wander randomly.
//    /// </summary>
//    public void SetChaseTarget(Vector2? target, float chaseSpeed = 40f)
//    {
//        _chaseTarget = target;
//        _chaseSpeed = chaseSpeed;
//    }

//    public void SetRandomDirection()
//    {
//        float angle = (float)(_random.NextDouble() * Math.PI * 2);
//        _velocity = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * _speed;
//        _directionChangeTimer = 2f + (float)_random.NextDouble() * 3f;
//        _isIdle = false;
//    }

//    public void SetIdle()
//    {
//        _velocity = Vector2.Zero;
//        _isIdle = true;
//        _idleTimer = 1f + (float)_random.NextDouble() * 2f;
//    }

    
//    public void SetPosition(Vector2 pos) => Position = pos;
//    public Vector2 GetVelocity() => _velocity;
//    public void SetVelocity(Vector2 v)
//    {
//        _velocity = v;
//        _isIdle = v == Vector2.Zero;
//    }
//}