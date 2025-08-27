using Hearthvale.GameCode.Entities.NPCs;
using Hearthvale.GameCode.Utils;
using Microsoft.Xna.Framework;
using System;

namespace Hearthvale.GameCode.Entities.Components
{
    /// <summary>
    /// AI component that can be used by any character (NPCs or Player in AI mode)
    /// </summary>
    public class CharacterAIComponent
    {
        private readonly Character _character;
        private readonly CharacterMovementComponent _movementComponent;
        private NpcAiType _aiType;
        private bool _isEnabled;
        private Vector2? _lastEngagementPoint;
        private const float ENGAGEMENT_STICKY_RADIUS = 16f;

        public NpcAiType AiType
        {
            get => _aiType;
            set => _aiType = value;
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set => _isEnabled = value;
        }

        public CharacterAIComponent(Character character, CharacterMovementComponent movementComponent)
        {
            _character = character;
            _movementComponent = movementComponent;
            _aiType = NpcAiType.Wander;
            _isEnabled = true;
        }

        public void Update(float deltaTime, Character target, float attackRange)
        {
            if (!_isEnabled || _character.IsKnockedBack || _character.IsAttacking || _character.IsDefeated)
            {
                _movementComponent.ForceIdle();
                return;
            }

            if (target == null || target.IsDefeated)
            {
                HandleWanderBehavior();
                return;
            }

            Vector2 targetCenter = new Vector2(target.Bounds.Center.X, target.Bounds.Center.Y);
            Vector2 characterCenter = new Vector2(_character.Bounds.Center.X, _character.Bounds.Center.Y);

            switch (_aiType)
            {
                case NpcAiType.Wander:
                    HandleWanderBehavior();
                    break;

                case NpcAiType.ChasePlayer:
                    HandleChaseBehavior(targetCenter, characterCenter, attackRange);
                    break;
            }

            // Update AI movement if not in attack range
            if (_aiType != NpcAiType.ChasePlayer ||
                Vector2.Distance(characterCenter, targetCenter) > attackRange * 1.2f)
            {
                _movementComponent.UpdateAIMovement(deltaTime);
            }
        }

        private void HandleWanderBehavior()
        {
            _movementComponent.SetChaseTarget(null);
        }

        private void HandleChaseBehavior(Vector2 targetCenter, Vector2 characterCenter, float attackRange)
        {
            float distanceToTarget = Vector2.Distance(characterCenter, targetCenter);

            if (distanceToTarget <= _movementComponent.ChaseRange)
            {
                if (distanceToTarget <= attackRange * 1.1f)
                {
                    // Clear chase target - we're close enough to attack
                    _movementComponent.SetChaseTarget(null);
                    _movementComponent.ForceIdle();

                    // Face the target for attack
                    UpdateFacingDirection(targetCenter - characterCenter);
                }
                else
                {
                    // We need to get closer to attack
                    Vector2 direction = targetCenter - characterCenter;
                    if (direction.LengthSquared() > 0.01f)
                    {
                        direction.Normalize();
                        Vector2 targetPos = targetCenter - direction * (attackRange * 0.95f);
                        _movementComponent.SetChaseTarget(targetPos, 70f);
                    }
                }
            }
            else if (distanceToTarget > _movementComponent.LoseTargetRange)
            {
                // Lost sight of target
                _movementComponent.SetChaseTarget(null);
                _lastEngagementPoint = null;
            }
        }

        private void UpdateFacingDirection(Vector2 direction)
        {
            if (direction.LengthSquared() > 0.01f)
            {
                _character.FacingRight = direction.X >= 0f;
                _movementComponent.FacingDirection = AngleToCardinal(MathF.Atan2(direction.Y, direction.X));
            }
        }

        private static CardinalDirection AngleToCardinal(float angleRadians)
        {
            float normalizedAngle = angleRadians;
            while (normalizedAngle < 0) normalizedAngle += MathF.Tau;
            while (normalizedAngle >= MathF.Tau) normalizedAngle -= MathF.Tau;

            float degrees = normalizedAngle * (180f / MathF.PI);

            return degrees switch
            {
                >= 315f or < 45f => CardinalDirection.East,
                >= 45f and < 135f => CardinalDirection.South,
                >= 135f and < 225f => CardinalDirection.West,
                >= 225f and < 315f => CardinalDirection.North,
                _ => CardinalDirection.East
            };
        }
    }
}