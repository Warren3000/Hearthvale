using Hearthvale.GameCode.Entities.Animations;
using Hearthvale.GameCode.Entities.Components;
using Hearthvale.GameCode.Entities.Interfaces;
using Hearthvale.GameCode.Entities.NPCs.Components;
using Hearthvale.GameCode.Managers;
using Hearthvale.GameCode.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hearthvale.GameCode.Entities.NPCs
{
    public enum NpcAiType
    {
        Wander,
        ChasePlayer
    }

    public enum NpcClass
    {
        Merchant,
        Knight,
        HeavyKnight
    }

    public class NPC : Character, ICombatNpc, IDialog, INpcAnimationProvider, IDisposable
    {
        // Components
        private readonly NpcCombatComponent _combatComponent;
        private readonly NpcBuffComponent _buffComponent;
        private readonly NpcAnimationComponent _npcAnimationComponent;
        private readonly CharacterAIComponent _aiComponent;

        private int _frameCounter;
        private int _attackPower = 1;
        private float _attackCooldown = 1.5f;
        private int _stuckFrameCount = 0;
        private const int STUCK_THRESHOLD = 15;
        private Character _currentPlayer;
        private List<Rectangle> _cachedObstacles = new List<Rectangle>();
        private bool _disposed = false;

        // Properties
        public string Name { get; private set; }
        public NpcClass Class { get; private set; }
        
        public bool CanAttack => _combatComponent.CanAttack;
        public bool IsReadyToRemove => IsDefeated && _npcAnimationComponent.HasCompletedFadeOut;
        public bool IsStuck => _stuckFrameCount >= STUCK_THRESHOLD;

        public int AttackPower
        {
            get => _attackPower;
            set
            {
                _attackPower = Math.Max(0, value);
                if (_combatComponent != null)
                    _combatComponent.AttackPower = _attackPower;
            }
        }

        public float AttackCooldown
        {
            get => _attackCooldown;
            set => _attackCooldown = MathF.Max(0f, value);
        }

        protected override float GetRenderOpacity() => _npcAnimationComponent?.FadeOpacity ?? 1f;

        public NPC(string name, Dictionary<string, Animation> animations, Vector2 position, Rectangle bounds, SoundEffect defeatSound, int maxHealth)
        {
            Name = name;
            AnimationUtils.ApplyDelayFactorToAll(animations, 2.0f);
            var sprite = new AnimatedSprite(animations["Idle"]);

            // Initialize base class components first
            InitializeComponents();
            _healthComponent = HealthComponent;

            // Initialize NPC-specific components
            _combatComponent = new NpcCombatComponent(this, AttackPower);
            _buffComponent = new NpcBuffComponent(this);
            _npcAnimationComponent = new NpcAnimationComponent(this, AnimationComponent);
            _aiComponent = new CharacterAIComponent(this, MovementComponent);

            // Set NPC class and AI type based on name
            Class = name.ToLower() switch
            {
                "merchant" => NpcClass.Merchant,
                "knight" => NpcClass.Knight,
                "heavyknight" => NpcClass.HeavyKnight,
                _ => NpcClass.Merchant
            };

            var aiType = name.ToLower() switch
            {
                "merchant" => NpcAiType.Wander,
                "knight" => NpcAiType.ChasePlayer,
                "heavyknight" => NpcAiType.ChasePlayer,
                _ => NpcAiType.Wander
            };
            _aiComponent.AiType = aiType;

            // Configure speed based on class
            var speedProfile = NpcSpeedConfiguration.GetSpeedProfile(Class);
            speedProfile.Validate();

            // Initialize components
            HealthComponent.SetMaxHealth(maxHealth);
            AnimationComponent.SetSprite(sprite);
            MovementComponent.SetPosition(position);
            MovementComponent.SetMovementSpeed(speedProfile.MovementSpeed);
            MovementComponent.SetCustomSpeeds(
                wanderSpeed: Math.Min(speedProfile.WanderSpeed * 8f, 45f),
                chaseSpeed: Math.Min(speedProfile.ChaseSpeed * 10f, 70f),
                fleeSpeed: Math.Min(speedProfile.FleeSpeed * 3f, 90f)
            );
            MovementComponent.ValidateSpeeds();

            // Apply per-type attack buffs at spawn
            _buffComponent.ConfigureTypeAttackBuffs();
        }

        public void Update(GameTime gameTime, IEnumerable<NPC> allNpcs, Character player, IEnumerable<Rectangle> rectangles)
        {
            _currentPlayer = player;
            _frameCounter++;

            float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;
            float weaponLength = EquippedWeapon?.Length ?? 32f;
            float attackRange = weaponLength * 1.5f;

            // Update knockback first
            UpdateKnockback(gameTime);

            // Update components
            _combatComponent.Update(elapsed);
            _buffComponent.Update(elapsed);

            if (!IsDefeated)
            {
                // Update AI behavior
                _aiComponent.Update(elapsed, player, attackRange);

                // Handle movement and collision
                if (!IsKnockedBack)
                {
                    Vector2 velocity = MovementComponent.GetVelocity();
                    if (velocity.LengthSquared() > 0)
                    {
                        Vector2 nextPosition = Position + velocity * elapsed;
                        CollisionComponent.TryMove(nextPosition, allNpcs.Where(n => n != this).Cast<Character>().Append(player));
                    }
                }

                // Update attack state
                UpdateAttackState(gameTime, player, attackRange);

                // Update stuck detection
                UpdateStuckDetection();
            }

            // Update weapon
            UpdateWeapon(gameTime);

            // Update animation
            _npcAnimationComponent.Update(gameTime);

            // Debug logging
            if (_frameCounter % 180 == 0)
            {
                LogDebugInfo();
            }
        }

        private void UpdateAttackState(GameTime gameTime, Character player, float attackRange)
        {
            float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Check for player hit
            _combatComponent.CheckPlayerHit(player);

            // Update attack animation timer
            if (IsAttacking)
            {
                _attackAnimTimer -= elapsed;
                if (_attackAnimTimer <= 0)
                {
                    IsAttacking = false;
                }
            }

            // Check if we can start a new attack
            Vector2 playerCenter = new Vector2(player.Bounds.Center.X, player.Bounds.Center.Y);
            Vector2 npcCenter = new Vector2(this.Bounds.Center.X, this.Bounds.Center.Y);
            float centerDist = Vector2.Distance(npcCenter, playerCenter);

            if (CanAttack && !IsAttacking && !IsKnockedBack && centerDist <= attackRange)
            {
                // Face the player before attacking
                Vector2 toPlayer = playerCenter - npcCenter;
                if (toPlayer.LengthSquared() > 0.01f)
                {
                    _facingDir = AngleToCardinal(MathF.Atan2(toPlayer.Y, toPlayer.X));
                    FacingRight = toPlayer.X >= 0f;
                    MovementComponent.FacingDirection = _facingDir;
                }

                StartAttack();
            }
        }

        private void UpdateStuckDetection()
        {
            bool isTryingToMove = MovementComponent.GetVelocity().LengthSquared() > 0.01f;
            bool actuallyMoved = Vector2.DistanceSquared(MovementComponent.Position, _lastAnimPosition) > 0.01f;

            if (isTryingToMove && !actuallyMoved)
            {
                _stuckFrameCount++;
            }
            else
            {
                _stuckFrameCount = 0;
            }
        }

        private void LogDebugInfo()
        {
            var velocity = MovementComponent.GetVelocity();
            var speeds = MovementComponent.GetCurrentSpeeds();

            System.Diagnostics.Debug.WriteLine($"NPC {Name}: " +
                $"Current velocity magnitude = {velocity.Length():F1}, " +
                $"Configured speeds - W:{speeds.wander}, C:{speeds.chase}, F:{speeds.flee}");

            if (velocity.Length() > NpcSpeedConfiguration.MAX_SPEED)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ WARNING: {Name} velocity {velocity.Length():F1} exceeds max speed {NpcSpeedConfiguration.MAX_SPEED}!");
            }
        }

        // Attack-related fields
        private float _attackAnimTimer = 0f;
        private const float AttackAnimDuration = 0.25f;
        private CardinalDirection _facingDir = CardinalDirection.East;
        private Vector2 _lastAnimPosition;

        public void StartAttack()
        {
            IsAttacking = true;
            _attackAnimTimer = AttackAnimDuration;
            WeaponComponent?.StartSwing(_facingDir);
            ResetAttackTimer();
        }

        public void ResetAttackTimer() => _combatComponent.StartAttackCooldown(_attackCooldown);

        protected override Vector2 GetAttackDirection()
        {
            return MovementComponent.FacingDirection.ToVector();
        }

        public override bool TakeDamage(int amount, Vector2? knockback = null)
        {
            if (_healthComponent.CanTakeDamage)
            {
                bool justDefeated = base.TakeDamage(amount, knockback);
                Flash();
                return justDefeated;
            }
            else
            {
                Log.Info(LogArea.NPC, $"----[NPC.{Name}] CanTakeDamage is FALSE. Damage blocked.");
            }
            return false;
        }

        public override void Heal(int amount)
        {
            _healthComponent.Heal(amount);
        }

        public override void Flash()
        {
            AnimationComponent.Flash();
        }

        protected override void UpdateWeapon(GameTime gameTime)
        {
            if (_currentPlayer != null && EquippedWeapon != null)
            {
                Vector2 playerCenter = new Vector2(
                    _currentPlayer.Bounds.Center.X,
                    _currentPlayer.Bounds.Center.Y
                );
                _weaponComponent?.Update(gameTime, playerCenter);
            }
            else
            {
                base.UpdateWeapon(gameTime);
            }
        }

        public bool HandleProjectileHit(int damage, Vector2 knockback)
        {
            return TakeDamage(damage, knockback);
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

        public void UpdateObstacles(IEnumerable<Rectangle> obstacleRects, IEnumerable<NPC> allNpcs, Character player)
        {
            var obstacles = new List<Rectangle>();

            if (obstacleRects != null)
                obstacles.AddRange(obstacleRects);

            if (player != null && !player.IsDefeated)
                obstacles.Add(player.Bounds);

            if (allNpcs != null)
            {
                foreach (var otherNpc in allNpcs)
                {
                    if (otherNpc != this && !otherNpc.IsDefeated)
                        obstacles.Add(otherNpc.Bounds);
                }
            }

            _cachedObstacles = obstacles;
        }

        public AnimatedSprite GetAnimationSprite()
        {
            return AnimationComponent?.Sprite;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Clean up managed resources
                    _cachedObstacles?.Clear();
                    _cachedObstacles = null;
                    
                    // Unequip weapon to ensure it's properly disposed
                    if (WeaponComponent?.EquippedWeapon != null)
                    {
                        WeaponComponent.UnequipWeapon();
                    }
                }

                _disposed = true;
            }
        }
    }
}