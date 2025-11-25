using Hearthvale.GameCode.Data.Atlases.Models;
using Hearthvale.GameCode.Data.Models;
using Hearthvale.GameCode.Entities.Animations;
using Hearthvale.GameCode.Entities.Components;
using Hearthvale.GameCode.Entities.Interfaces;
using Hearthvale.GameCode.Entities.NPCs.Components;
using Hearthvale.GameCode.Entities;
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
        Skeleton,
        Goblin,
        Warrior
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
        private static readonly Random _random = new Random();

        // Properties
        public string Name { get; private set; }
        public NpcClass Class { get; private set; }
        public string CurrentAttackProfileId { get; set; }
        
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
            if (animations == null || animations.Count == 0)
            {
                throw new ArgumentException("NPC requires at least one animation", nameof(animations));
            }

            if (!animations.TryGetValue("Idle_Down", out var idleDownAnimation))
            {
                if (animations.TryGetValue("Idle", out var fallbackIdle))
                {
                    idleDownAnimation = fallbackIdle;
                    animations["Idle_Down"] = idleDownAnimation;
                }
                else if (animations.Count > 0)
                {
                    idleDownAnimation = animations.Values.First();
                    animations["Idle_Down"] = idleDownAnimation;
                }
                else
                {
                    throw new ArgumentException("NPC animations must include an 'Idle_Down' entry", nameof(animations));
                }
            }

            var sprite = new AnimatedSprite(idleDownAnimation);

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
                "skeleton" => NpcClass.Skeleton,
                "goblin" => NpcClass.Goblin,
                "warrior" => NpcClass.Warrior,
                _ => NpcClass.Skeleton
            };

            var aiType = Class switch
            {
                NpcClass.Skeleton => NpcAiType.ChasePlayer,
                NpcClass.Goblin => NpcAiType.ChasePlayer,
                _ => NpcAiType.Wander
            };
            _aiComponent.AiType = aiType;

            // Configure speed based on class
            var speedProfile = NpcSpeedConfiguration.GetSpeedProfile(Class);
            speedProfile.Validate();

            // Initialize components
            HealthComponent.SetMaxHealth(maxHealth);
            AnimationComponent.SetSprite(sprite);
            foreach (var (animationName, animation) in animations)
            {
                AnimationComponent.AddAnimation(animationName, animation);
            }
            AnimationComponent.SetAnimation("Idle_Down");
            MovementComponent.SetPosition(position);
            MovementComponent.SetMovementSpeed(speedProfile.MovementSpeed);
            MovementComponent.SetCustomSpeeds(
                wanderSpeed: Math.Min(speedProfile.WanderSpeed * 8f, 45f),
                chaseSpeed: Math.Min(speedProfile.ChaseSpeed * 10f, 70f),
                fleeSpeed: Math.Min(speedProfile.FleeSpeed * 3f, 90f)
            );
            MovementComponent.ValidateSpeeds();
            _lastAnimPosition = position;

            // Apply per-type attack buffs at spawn
            _buffComponent.ConfigureTypeAttackBuffs();
        }

        public void SetCollisionProfile(NpcCollisionProfile profile)
        {
            if (profile.Hitbox != Rectangle.Empty)
            {
                SetCollisionBox(profile.Hitbox);
            }
        }

        public void Update(GameTime gameTime, IEnumerable<NPC> allNpcs, Character player, IEnumerable<Rectangle> rectangles)
        {
            _currentPlayer = player;
            _frameCounter++;

            float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;
            float attackRange = GetEffectiveAttackRange();

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
            bool wasAttacking = IsAttacking;

            // Check for player hit
            _combatComponent.CheckPlayerHit(player);

            // Update attack animation timer
            if (IsAttacking)
            {
                _attackAnimTimer -= elapsed;
                if (_attackAnimTimer <= 0)
                {
                    IsAttacking = false;
                    CurrentAttackProfileId = null;
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

                // SelectAttackProfile();
                StartAttack();
            }

            if (wasAttacking && !IsAttacking)
            {
                CollisionComponent?.ClearProfileCollider();
            }
        }

        private void SelectAttackProfile()
        {
            // Reset to default
            CurrentAttackProfileId = null;

            // 30% chance to use special attack
            if (_random.NextDouble() < 0.3)
            {
                switch (Class)
                {
                    case NpcClass.Warrior:
                        CurrentAttackProfileId = "warrior_spin";
                        break;
                    case NpcClass.Goblin:
                        CurrentAttackProfileId = "goblin_leap";
                        break;
                    case NpcClass.Skeleton:
                        CurrentAttackProfileId = "skeleton_lunge";
                        break;
                }
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
            Log.Info(LogArea.NPC, $"----[NPC.{Name}] Velocity: {velocity}, Speeds: W:{speeds.wander}, C:{speeds.chase}, F:{speeds.flee}");
            //System.Diagnostics.Debug.WriteLine($"NPC {Name}: " +
            //    $"Current velocity magnitude = {velocity.Length():F1}, " +
            //    $"Configured speeds - W:{speeds.wander}, C:{speeds.chase}, F:{speeds.flee}");

            if (velocity.Length() > NpcSpeedConfiguration.MAX_SPEED)
            {
                Log.Warn(LogArea.NPC, $"NPC {Name} velocity {velocity.Length():F1} exceeds max speed {NpcSpeedConfiguration.MAX_SPEED}!");
                //System.Diagnostics.Debug.WriteLine($"⚠️ WARNING: {Name} velocity {velocity.Length():F1} exceeds max speed {NpcSpeedConfiguration.MAX_SPEED}!");
            }
        }

        // Attack-related fields
        private float _attackAnimTimer = 0f;
        private const float DefaultAttackAnimDuration = 0.25f;
        private const float DefaultAttackRangeBuffer = 6f;
        private const float DefaultMinAttackRange = 24f;
        private const float DefaultWeaponLengthScale = 0.85f;
        private static readonly string[] AttackAnimationPrefixes = { "Attack", "Attack_01" };
        private CardinalDirection _facingDir = CardinalDirection.East;
        private Vector2 _lastAnimPosition = Vector2.Zero;

        public void StartAttack()
        {
            IsAttacking = true;
            _attackAnimTimer = GetAttackAnimationDurationSeconds(_facingDir);
            var swingProfile = ResolveSwingProfile(_facingDir);
            WeaponComponent?.StartSwing(_facingDir, swingProfile);
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
            return CardinalDirectionExtensions.FromAngle(angleRadians);
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

        public float GetEffectiveAttackRange()
        {
            AttackTimingProfile timingProfile = TryGetAttackTimingProfile();

            float rangeBuffer = timingProfile?.RangeBuffer ?? DefaultAttackRangeBuffer;
            float minRange = timingProfile?.MinRange ?? DefaultMinAttackRange;
            float weaponLengthScale = timingProfile?.WeaponLengthScale ?? DefaultWeaponLengthScale;

            if (timingProfile?.RangeOverride is float overrideRange && overrideRange > 0f)
            {
                return overrideRange;
            }

            var weapon = EquippedWeapon;
            if (weapon == null)
            {
                return minRange;
            }

            Rectangle tightBounds = GetTightSpriteBounds();
            Vector2 center = new Vector2(
                tightBounds.Left + tightBounds.Width / 2f,
                tightBounds.Top + tightBounds.Height / 2f
            );

            var polygon = weapon.GetTransformedHitPolygon(center);
            float maxDistance = 0f;

            if (polygon != null && polygon.Count > 0)
            {
                for (int i = 0; i < polygon.Count; i++)
                {
                    float distance = Vector2.Distance(polygon[i], center);
                    if (distance > maxDistance)
                    {
                        maxDistance = distance;
                    }
                }
            }

            if (maxDistance <= 0f)
            {
                maxDistance = weapon.Length > 0f ? weapon.Length * weaponLengthScale : minRange;
            }

            return MathF.Max(maxDistance + rangeBuffer, minRange);
        }

        private float GetAttackAnimationDurationSeconds(CardinalDirection direction)
        {
            if (AnimationComponent == null)
            {
                return DefaultAttackAnimDuration;
            }

            string suffix = GetDirectionSuffix(direction);

            foreach (var prefix in AttackAnimationPrefixes)
            {
                string directionalName = $"{prefix}_{suffix}";
                TimeSpan duration = AnimationComponent.GetAnimationDuration(directionalName);
                if (duration > TimeSpan.Zero)
                {
                    return (float)duration.TotalSeconds;
                }
            }

            foreach (var prefix in AttackAnimationPrefixes)
            {
                TimeSpan duration = AnimationComponent.GetAnimationDuration(prefix);
                if (duration > TimeSpan.Zero)
                {
                    return (float)duration.TotalSeconds;
                }
            }

            return DefaultAttackAnimDuration;
        }

        private WeaponSwingProfile ResolveSwingProfile(CardinalDirection direction)
        {
            AttackTimingProfile timingProfile = TryGetAttackTimingProfile();
            if (timingProfile == null || AnimationComponent == null)
            {
                return WeaponSwingProfile.Default;
            }

            string animationName = ResolveAttackAnimationName(direction);
            if (string.IsNullOrWhiteSpace(animationName))
            {
                return WeaponSwingProfile.Default;
            }

            if (!AnimationComponent.TryGetAnimation(animationName, out var animation))
            {
                return WeaponSwingProfile.Default;
            }

            return WeaponSwingProfileFactory.FromAttackTiming(timingProfile, animation);
        }

        private string ResolveAttackAnimationName(CardinalDirection direction)
        {
            if (AnimationComponent == null)
            {
                return null;
            }

            string suffix = GetDirectionSuffix(direction);

            foreach (var prefix in AttackAnimationPrefixes)
            {
                string directional = $"{prefix}_{suffix}";
                if (AnimationComponent.HasAnimation(directional))
                {
                    return directional;
                }
            }

            foreach (var prefix in AttackAnimationPrefixes)
            {
                if (AnimationComponent.HasAnimation(prefix))
                {
                    return prefix;
                }
            }

            return null;
        }

        private static string GetDirectionSuffix(CardinalDirection direction)
        {
            CardinalDirection primary = direction.ToFourWay();
            return primary switch
            {
                CardinalDirection.North => "Up",
                CardinalDirection.South => "Down",
                CardinalDirection.East => "Right",
                CardinalDirection.West => "Left",
                _ => "Down"
            };
        }

        private AttackTimingProfile TryGetAttackTimingProfile()
        {
            try
            {
                var manager = DataManager.Instance;
                if (!string.IsNullOrEmpty(CurrentAttackProfileId))
                {
                    var profile = manager.GetEnemyAttackProfile(CurrentAttackProfileId);
                    if (profile != null) return profile;
                }
                return manager.GetEnemyAttackProfile(Name) ?? manager.GetEnemyAttackProfile(Class.ToString().ToLowerInvariant());
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }

        private static Vector2 ComputeSeparationVector(Rectangle rectA, Rectangle rectB)
        {
            if (!rectA.Intersects(rectB))
            {
                return Vector2.Zero;
            }

            float overlapLeft = rectA.Right - rectB.Left;
            float overlapRight = rectB.Right - rectA.Left;
            float overlapTop = rectA.Bottom - rectB.Top;
            float overlapBottom = rectB.Bottom - rectA.Top;

            float minXOverlap = MathF.Min(overlapLeft, overlapRight);
            float minYOverlap = MathF.Min(overlapTop, overlapBottom);

            bool preferHorizontal = minXOverlap <= minYOverlap + 0.001f;

            if (preferHorizontal)
            {
                float direction = rectA.Center.X < rectB.Center.X ? -1f : 1f;
                return new Vector2(minXOverlap * direction, 0f);
            }

            float verticalDirection = rectA.Center.Y < rectB.Center.Y ? -1f : 1f;
            return new Vector2(0f, minYOverlap * verticalDirection);
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