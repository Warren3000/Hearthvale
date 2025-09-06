using Hearthvale.GameCode.Utils;
using Microsoft.Xna.Framework;
using MonoGameLibrary.Graphics;
using System;

namespace Hearthvale.GameCode.Entities.Components
{
    public enum AIState
    {
        Idle,
        Wandering, 
        Chasing,
        Attacking,
        Fleeing,
        Investigating
    }

    /// <summary>
    /// Manages character position, movement, and bounds calculations
    /// </summary>
    public class CharacterMovementComponent
    {
        private readonly Character _character;
        private Vector2 _position;
        private float _movementSpeed = 2f;
        private bool _facingRight = true;
        private float _directionChangeTimer;
        private Vector2 _lastMovementVector = Vector2.Zero;
        private CardinalDirection _facingDirection = CardinalDirection.South;

        // Enhanced AI system
        private AIState _currentState = AIState.Idle;
        private AIState _previousState = AIState.Idle;
        private float _stateTimer = 0f;
        private Vector2? _chaseTarget = null;
        private Vector2? _investigationTarget = null;
        private float _chaseSpeed = 60f; // Increased for more responsiveness
        private float _wanderSpeed = 30f;
        private float _fleeSpeed = 80f;
        
        // Reaction system
        private float _reactionTime = 0.1f; // How quickly AI responds to changes
        private float _reactionTimer = 0f;
        private float _pathRecalculationTimer = 0f;
        private const float PATH_RECALC_INTERVAL = 0.2f; // Recalculate path 5 times per second

        // Advanced movement
        private Vector2 _velocity;
        private Vector2 _desiredVelocity;
        private float _acceleration = 300f; // Pixels per second squared
        private float _deceleration = 400f;
        private float _maxSpeed = 100f;

        // Behavior parameters
        private float _idleTimer;
        private float _investigationRadius = 80f;
        private float _chaseRange = 120f;
        private float _loseTargetRange = 180f;
        private float _fleeRange = 60f;

        private readonly Random _random = new();

        public Vector2 Position => _position;
        //public float MovementSpeed => _movementSpeed;
        //public float ReactionTime
        //{
        //    get => _reactionTime;
        //    set => _reactionTime = Math.Max(0.05f, value); // Minimum reaction time
        //}
        public float ChaseRange
        {
            get => _chaseRange;
            set => _chaseRange = Math.Max(20f, value);
        }
        public Vector2? ChaseTarget => _chaseTarget;
        public float LoseTargetRange
        {
            get => _loseTargetRange;
            set => _loseTargetRange = Math.Max(_chaseRange + 20f, value);
        }
        //public AIState CurrentAIState => _currentState;
        //public bool IsMoving => _velocity.LengthSquared() > 0.1f;

        public bool FacingRight
        {
            get => _facingRight;
            set => _facingRight = value;
        }

        public CardinalDirection FacingDirection
        {
            get => _facingDirection;
            set => _facingDirection = value;
        }
        
        public Vector2 LastMovementVector
        {
            get => _lastMovementVector;
            set
            {
                _lastMovementVector = value;
                if (value != Vector2.Zero)
                {
                    _facingDirection = value.ToCardinalDirection();
                    _facingRight = (_facingDirection == CardinalDirection.East);
                }
            }
        }

        public CharacterMovementComponent(Character character, Vector2 initialPosition, float movementSpeed = 2f)
        {
            _character = character;
            _position = initialPosition;
            _movementSpeed = movementSpeed;
            _wanderSpeed = 8f;   // Very low for wandering
            _chaseSpeed = 15f;   // Reasonable chase speed
        }

        /// <summary>
        /// Enhanced AI update with responsive state machine
        /// </summary>
        public void UpdateAIMovement(float elapsed)
        {
            _stateTimer += elapsed;
            _reactionTimer -= elapsed;
            _pathRecalculationTimer -= elapsed;

            // Update AI state based on conditions
            UpdateAIState(elapsed);

            // Execute current state behavior
            ExecuteAIState(elapsed);

            // Apply physics-based movement
            UpdatePhysicsMovement(elapsed);

            // Update facing direction
            UpdateFacingDirection();
            if (_velocity.Length() > 50f) // Max reasonable velocity
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ CLAMPING excessive velocity: {_velocity.Length()} -> 50");
                _velocity = Vector2.Normalize(_velocity) * 50f;
            }
        }

        private void UpdateAIState(float elapsed)
        {
            AIState newState = _currentState;

            // State transition logic based on conditions
            switch (_currentState)
            {
                case AIState.Idle:
                    if (_chaseTarget.HasValue)
                        newState = AIState.Chasing;
                    else if (_stateTimer > GetRandomIdleTime())
                        newState = AIState.Wandering;
                    break;

                case AIState.Wandering:
                    if (_chaseTarget.HasValue)
                        newState = AIState.Chasing;
                    else if (_stateTimer > GetRandomWanderTime() || _random.NextDouble() < 0.1f * elapsed)
                        newState = AIState.Idle;
                    break;

                case AIState.Chasing:
                    if (!_chaseTarget.HasValue)
                        newState = AIState.Investigating;
                    else if (Vector2.Distance(_position, _chaseTarget.Value) > _loseTargetRange)
                        newState = AIState.Investigating;
                    break;

                case AIState.Investigating:
                    if (_chaseTarget.HasValue && Vector2.Distance(_position, _chaseTarget.Value) < _chaseRange)
                        newState = AIState.Chasing;
                    else if (_stateTimer > 3f) // Investigate for 3 seconds
                        newState = AIState.Wandering;
                    break;

                case AIState.Fleeing:
                    if (!_chaseTarget.HasValue || Vector2.Distance(_position, _chaseTarget.Value) > _fleeRange * 2f)
                        newState = AIState.Wandering;
                    break;
            }

            // Change state if needed
            if (newState != _currentState)
            {
                ChangeState(newState);
            }
        }

        private void ChangeState(AIState newState)
        {
            _previousState = _currentState;
            _currentState = newState;
            _stateTimer = 0f;
            _reactionTimer = _reactionTime;

            // State entry logic
            switch (newState)
            {
                case AIState.Investigating:
                    _investigationTarget = _chaseTarget; // Remember last known position
                    break;
                case AIState.Wandering:
                    SetRandomWanderDirection();
                    break;
                case AIState.Idle:
                    _desiredVelocity = Vector2.Zero;
                    break;
            }
        }

        private void ExecuteAIState(float elapsed)
        {
            // Only make decisions after reaction time
            if (_reactionTimer > 0f) return;

            Vector2 targetVelocity = Vector2.Zero;
            float targetSpeed = 0f;

            switch (_currentState)
            {
                case AIState.Idle:
                    targetVelocity = Vector2.Zero;
                    break;

                case AIState.Wandering:
                    if (_pathRecalculationTimer <= 0f)
                    {
                        SetRandomWanderDirection();
                        _pathRecalculationTimer = PATH_RECALC_INTERVAL * 2f; // Less frequent for wandering
                    }
                    targetVelocity = _desiredVelocity;
                    targetSpeed = _wanderSpeed;
                    break;

                case AIState.Chasing:
                    if (_chaseTarget.HasValue && _pathRecalculationTimer <= 0f)
                    {
                        // Get direction from our center to target center
                        Vector2 myCenter = _position;
                        if (_character.Sprite != null)
                        {
                            myCenter = new Vector2(
                                _position.X + _character.Sprite.Width * 0.5f,
                                _position.Y + _character.Sprite.Height * 0.5f
                            );
                        }
                        
                        Vector2 direction = _chaseTarget.Value - myCenter;
                        float distance = direction.Length();
                        
                        // Only move if we're not already at the target
                        if (distance > 2f) // Small threshold to prevent jitter
                        {
                            direction.Normalize();
                            _desiredVelocity = direction;
                        }
                        else
                        {
                            // We've reached our target, stop
                            _desiredVelocity = Vector2.Zero;
                        }
                        
                        _pathRecalculationTimer = PATH_RECALC_INTERVAL;
                    }
                    targetVelocity = _desiredVelocity;
                    targetSpeed = _chaseSpeed;
                    break;

                case AIState.Investigating:
                    if (_investigationTarget.HasValue)
                    {
                        Vector2 direction = _investigationTarget.Value - _position;
                        if (direction.LengthSquared() > 4f) // Close enough threshold
                        {
                            direction.Normalize();
                            targetVelocity = direction;
                            targetSpeed = _wanderSpeed;
                        }
                        else
                        {
                            // Reached investigation point, look around
                            if (_stateTimer > 1f)
                            {
                                SetRandomWanderDirection();
                                targetVelocity = _desiredVelocity * 0.5f; // Slower investigation
                                targetSpeed = _wanderSpeed * 0.5f;
                            }
                        }
                    }
                    break;

                case AIState.Fleeing:
                    if (_chaseTarget.HasValue)
                    {
                        Vector2 fleeDirection = _position - _chaseTarget.Value;
                        if (fleeDirection.LengthSquared() > 0.1f)
                        {
                            fleeDirection.Normalize();
                            targetVelocity = fleeDirection;
                            targetSpeed = _fleeSpeed;
                        }
                    }
                    break;
            }

            // Apply target velocity with speed
            if (targetVelocity.LengthSquared() > 0f)
            {
                _desiredVelocity = targetVelocity * targetSpeed;
            }
        }

        private void UpdatePhysicsMovement(float elapsed)
        {
            // Smooth acceleration/deceleration
            Vector2 velocityDiff = _desiredVelocity - _velocity;
            float distance = velocityDiff.Length();

            if (distance > 0.1f)
            {
                Vector2 acceleration = velocityDiff;
                
                // FIXED: Check for NaN before normalizing
                if (float.IsNaN(acceleration.X) || float.IsNaN(acceleration.Y) || distance == 0f)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ WARNING: Invalid acceleration detected! Resetting velocity.");
                    _velocity = Vector2.Zero;
                    _desiredVelocity = Vector2.Zero;
                    return;
                }
                
                acceleration.Normalize();

                // Use acceleration or deceleration based on whether we're speeding up or slowing down
                float accel = Vector2.Dot(_velocity, _desiredVelocity) >= 0 ? _acceleration : _deceleration;
                
                float maxChange = accel * elapsed;
                if (distance < maxChange)
                {
                    _velocity = _desiredVelocity;
                }
                else
                {
                    _velocity += acceleration * maxChange;
                }
            }

            // FIXED: Additional NaN safety checks
            if (float.IsNaN(_velocity.X) || float.IsNaN(_velocity.Y))
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ CRITICAL: NaN velocity detected! Resetting to zero.");
                _velocity = Vector2.Zero;
            }

            if (_velocity.LengthSquared() > 0)
            {
                float currentSpeed = _velocity.Length();
                
                // FIXED: Check for NaN in speed calculations
                if (float.IsNaN(currentSpeed))
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ CRITICAL: NaN speed detected! Resetting velocity.");
                    _velocity = Vector2.Zero;
                    return;
                }
                
                float maxAllowedSpeed = GetCurrentMaxSpeed(); // Based on current AI state

                if (currentSpeed > maxAllowedSpeed)
                {
                    _velocity = Vector2.Normalize(_velocity) * maxAllowedSpeed;
                }
            }

            // Update last movement vector for animation
            if (_velocity.LengthSquared() > 0.01f)
            {
                _lastMovementVector = Vector2.Normalize(_velocity);
            }
            else
            {
                _lastMovementVector = Vector2.Zero;
            }
        }
        private float GetCurrentMaxSpeed()
        {
            return _currentState switch
            {
                AIState.Wandering => Math.Min(_wanderSpeed, 15f),
                AIState.Chasing => Math.Min(_chaseSpeed, 25f),
                AIState.Fleeing => Math.Min(_fleeSpeed, 30f),
                _ => 10f
            };
        }
        private void UpdateFacingDirection()
        {
            if (_velocity.LengthSquared() > 0.01f)
            {
                _facingDirection = VelocityToCardinal(_velocity);
                _facingRight = (_facingDirection == CardinalDirection.East);
            }
        }

        private void SetRandomWanderDirection()
        {
            float angle = (float)(_random.NextDouble() * Math.PI * 2);
            _desiredVelocity = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
        }

        private float GetRandomIdleTime() => 1f + (float)_random.NextDouble() * 2f;
        private float GetRandomWanderTime() => 2f + (float)_random.NextDouble() * 4f;

        // Public API methods
        public void SetChaseTarget(Vector2? target, float chaseSpeed = 60f)
        {
            _chaseTarget = target;
            _chaseSpeed = chaseSpeed;
            
            // Immediately react to new chase target
            if (target.HasValue && _currentState != AIState.Chasing)
            {
                ChangeState(AIState.Chasing);
            }
            else if (!target.HasValue && _currentState == AIState.Chasing)
            {
                ChangeState(AIState.Investigating);
            }
        }

        public void SetFleeTarget(Vector2 fleeFrom)
        {
            _chaseTarget = fleeFrom; // Reuse chase target for flee logic
            ChangeState(AIState.Fleeing);
        }

        public void ForceIdle()
        {
            ChangeState(AIState.Idle);
        }

        // Existing methods with updates
        public void MoveInDirection(CardinalDirection direction, float deltaTime)
        {
            _facingDirection = direction;
            _facingRight = (direction == CardinalDirection.East);

            Vector2 movement = direction.ToVector() * _movementSpeed * deltaTime;
            _position += movement;
            _lastMovementVector = direction.ToVector();

            UpdateSpritePosition();
        }

        public void SetPosition(Vector2 position)
        {
            _position = position;
            UpdateSpritePosition();
        }
        
        public void SetMovementSpeed(float speed)
        {
            _movementSpeed = speed;
            // FIXED: Don't override custom speeds if they were already set
            // Only set defaults if they haven't been customized
            if (_wanderSpeed <= _movementSpeed * 15f) // Only update if using default scaling
                _wanderSpeed = speed * 10f;
            if (_chaseSpeed <= _movementSpeed * 30f) // Only update if using default scaling  
                _chaseSpeed = speed * 20f;
        }
        
        public void SetVelocity(Vector2 velocity)
        {
            _velocity = velocity;
            _desiredVelocity = velocity;
        }
        
        public Vector2 GetVelocity() => _velocity;

        public void SetCustomSpeeds(float wanderSpeed, float chaseSpeed, float fleeSpeed = 0f)
        {
            // Clamp speeds to reasonable values
            _wanderSpeed = MathHelper.Clamp(wanderSpeed, 1f, 25f);
            _chaseSpeed = MathHelper.Clamp(chaseSpeed, 1f, 30f);
            if (fleeSpeed > 0f)
                _fleeSpeed = MathHelper.Clamp(fleeSpeed, 1f, 35f);
            Log.Info(LogArea.NPC, $"CustomSpeeds CLAMPED - Wander: {_wanderSpeed}, Chase: {_chaseSpeed}, Flee: {_fleeSpeed}");
        }

        public (float wander, float chase, float flee) GetCurrentSpeeds()
        {
            return (_wanderSpeed, _chaseSpeed, _fleeSpeed);
        }

        public void ValidateSpeeds()
        {
            const float MAX_REASONABLE_SPEED = 100f;
            
            if (_wanderSpeed > MAX_REASONABLE_SPEED)
            {
                Log.Info(LogArea.NPC, $"⚠️ WARNING: Wander speed {_wanderSpeed} exceeds reasonable limit {MAX_REASONABLE_SPEED}");
                _wanderSpeed = Math.Min(_wanderSpeed, MAX_REASONABLE_SPEED);
            }
            
            if (_chaseSpeed > MAX_REASONABLE_SPEED)
            {
                Log.Info(LogArea.NPC, $"⚠️ WARNING: Chase speed {_chaseSpeed} exceeds reasonable limit {MAX_REASONABLE_SPEED}");
                _chaseSpeed = Math.Min(_chaseSpeed, MAX_REASONABLE_SPEED);
            }
            
            if (_fleeSpeed > MAX_REASONABLE_SPEED)
            {
                Log.Info(LogArea.NPC, $"⚠️ WARNING: Flee speed {_fleeSpeed} exceeds reasonable limit {MAX_REASONABLE_SPEED}");
                _fleeSpeed = Math.Min(_fleeSpeed, MAX_REASONABLE_SPEED);
            }
        }

        private static CardinalDirection VelocityToCardinal(Vector2 velocity)
        {
            if (velocity.LengthSquared() < 0.01f)
                return CardinalDirection.South;

            float angle = MathF.Atan2(velocity.Y, velocity.X);
            
            while (angle < 0) angle += MathF.Tau;
            while (angle >= MathF.Tau) angle -= MathF.Tau;

            float degrees = angle * (180f / MathF.PI);

            return degrees switch
            {
                >= 315f or < 45f => CardinalDirection.East,
                >= 45f and < 135f => CardinalDirection.South,
                >= 135f and < 225f => CardinalDirection.West,
                >= 225f and < 315f => CardinalDirection.North,
                _ => CardinalDirection.East
            };
        }

        public void ClampToBounds(Rectangle bounds)
        {
            if (_character.Sprite == null) return;

            float clampedX = MathHelper.Clamp(Position.X, bounds.Left, bounds.Right - _character.Sprite.Width);
            float clampedY = MathHelper.Clamp(Position.Y, bounds.Top, bounds.Bottom - _character.Sprite.Height);
            SetPosition(new Vector2(clampedX, clampedY));
        }
        private void UpdateSpritePosition()
        {
            if (_character.Sprite != null)
            {
                _character.Sprite.Position = _position;
            }
        }
    }
}