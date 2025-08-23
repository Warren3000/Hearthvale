using Hearthvale.GameCode.Entities.Animations;
using Hearthvale.GameCode.Entities.Components;
using Hearthvale.GameCode.Entities.Interfaces;
using Hearthvale.GameCode.Managers; // added
using Hearthvale.GameCode.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Graphics;
using SharpDX.MediaFoundation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Hearthvale.GameCode.Entities.NPCs
{
    public enum NpcAiType
    {
        Wander,
        ChasePlayer
    }

    // Tag to identify NPC combat class (separate from AI)
    public enum NpcClass
    {
        Merchant,
        Knight,
        HeavyKnight
    }

    public class NPC : Character, ICombatNpc, IDialog, INpcAnimationProvider
    {
        private readonly NpcHealthComponent _healthController;
        private readonly NpcCombatComponent _combatComponent;
        private int _frameCounter;

        // Centralized animation driver for walk/idle frames
        private readonly MovementAnimationDriver _animDriver = new();

        // Keep NPC and combat component attack power in sync
        private int _attackPower = 1;
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

        private float _attackCooldown = 1.5f;
        // Expose cooldown for tuning per-NPC (prevents spamming)
        public float AttackCooldown
        {
            get => _attackCooldown;
            set => _attackCooldown = MathF.Max(0f, value);
        }

        private Vector2 _lastMovementDirection = Vector2.Zero;

        public bool CanAttack => _combatComponent.CanAttack;
        public void ResetAttackTimer() => _combatComponent.StartAttackCooldown(_attackCooldown);
        public bool IsReadyToRemove => _healthController.IsReadyToRemove;

        public override Rectangle Bounds
        {
            get
            {
                if (Sprite != null)
                {
                    return new Rectangle(
                        (int)Position.X,
                        (int)Position.Y,
                        (int)Sprite.Width,
                        (int)Sprite.Height
                    );
                }
                return this.GetTightSpriteBounds();
            }
        }

        public bool IsAttacking { get; private set; }
        private float _attackAnimTimer = 0f;
        private const float AttackAnimDuration = 0.25f; // WindUp (0.15) + Slash (0.1)

        public string Name { get; private set; }
        private NpcAiType _aiType;

        // Type tag for per-NPC-type buffs
        public NpcClass Class { get; private set; }

        // Optional: timed flat buffs to AttackPower
        private readonly struct AttackBuff
        {
            public readonly int Delta;
            public readonly float TimeLeft;
            public AttackBuff(int delta, float timeLeft) { Delta = delta; TimeLeft = timeLeft; }
            public AttackBuff Tick(float dt) => new AttackBuff(Delta, TimeLeft - dt);
            public bool Expired => TimeLeft <= 0f;
        }
        private readonly List<AttackBuff> _attackBuffs = new();

        // Example conditional flag (e.g., HeavyKnight enrage once at low HP)
        private bool _enrageApplied;

        // Visual indicator state for active attack buffs
        private float _buffPulseTimer = 0f;
        private bool HasActiveTimedAttackBuff => _attackBuffs.Count > 0;

        // Player-style swing: cache current facing cardinal
        private CardinalDirection _facingDir = CardinalDirection.East;

        // Animation movement tracking
        private Vector2 _lastAnimPosition;
        private bool _movingForAnim;

        // Stuck detection
        private int _stuckFrameCount = 0;
        private const int STUCK_THRESHOLD = 15;
        public bool IsStuck => _stuckFrameCount >= STUCK_THRESHOLD;

        // Add field to store current player reference
        private Character _currentPlayer;

        public NPC(string name, Dictionary<string, Animation> animations, Vector2 position, Rectangle bounds, SoundEffect defeatSound, int maxHealth)
        {
            Name = name;
            var sprite = new AnimatedSprite(animations["Idle"]);

            // Initialize base class components first
            InitializeComponents();

            _healthController = new NpcHealthComponent(maxHealth, defeatSound);
            _combatComponent = new NpcCombatComponent(this, AttackPower);

            // FIXED: Use centralized speed configuration
            var speedProfile = NpcSpeedConfiguration.GetSpeedProfile(Class);
            speedProfile.Validate(); // Ensure all speeds are within reasonable limits

            // Initialize health component
            HealthComponent.SetMaxHealth(maxHealth);

            // Initialize animation and movement components
            AnimationComponent.SetSprite(sprite);
            MovementComponent.SetPosition(position);

            // Set AI type based on name
            _aiType = name.ToLower() switch
            {
                "merchant" => NpcAiType.Wander,
                "knight" => NpcAiType.ChasePlayer,
                "heavyknight" => NpcAiType.ChasePlayer,
                _ => NpcAiType.Wander
            };

            // Set NPC class for buff routing
            Class = name.ToLower() switch
            {
                "merchant" => NpcClass.Merchant,
                "knight" => NpcClass.Knight,
                "heavyknight" => NpcClass.HeavyKnight,
                _ => NpcClass.Merchant
            };

            switch (Class)
            {
                case NpcClass.Knight:
                    MovementComponent.SetMovementSpeed(2f);
                    break;

                case NpcClass.HeavyKnight:
                    MovementComponent.SetMovementSpeed(1.5f);
                    break;

                case NpcClass.Merchant:
                    MovementComponent.SetMovementSpeed(1.8f);
                    break;
            }
            MovementComponent.SetMovementSpeed(speedProfile.MovementSpeed);
            // Set custom AI speeds
            MovementComponent.SetCustomSpeeds(
                wanderSpeed: Math.Min(speedProfile.WanderSpeed * 8f, 45f),    
                chaseSpeed: Math.Min(speedProfile.ChaseSpeed * 10f, 70f),   
                fleeSpeed: Math.Min(speedProfile.FleeSpeed * 3f, 90f)
            );
            MovementComponent.ValidateSpeeds();
            var currentSpeeds = MovementComponent.GetCurrentSpeeds();


            System.Diagnostics.Debug.WriteLine($"NPC {Name} ({Class}) speeds set - " +
                $"Wander: {currentSpeeds.wander}, Chase: {currentSpeeds.chase}, Flee: {currentSpeeds.flee}");
            // Apply per-type attack buffs at spawn
            ConfigureTypeAttackBuffs();

            // Init animation movement tracking
            _lastAnimPosition = position;
            _movingForAnim = false;
        }

        private void ConfigureTypeAttackBuffs()
        {
            switch (Class)
            {
                case NpcClass.Merchant:
                    // No combat buffs
                    break;

                case NpcClass.Knight:
                    // Flat permanent bonus
                    AttackPower += 2;
                    Sprite?.Flash(Color.Goldenrod, 0.25);
                    break;

                case NpcClass.HeavyKnight:
                    AttackPower += 3;
                    AttackCooldown = MathF.Max(AttackCooldown, 2.0f);
                    Sprite?.Flash(Color.Goldenrod, 0.35);
                    break;
            }
        }
        protected override Vector2 GetAttackDirection()
        {
            return MovementComponent.FacingDirection.ToVector();
        }
        public override bool TakeDamage(int amount, Vector2? knockback = null)
        {
            if (_healthController.CanTakeDamage)
            {
                // Use the base class TakeDamage to handle health and knockback centrally
                bool justDefeated = base.TakeDamage(amount, knockback);
                if (justDefeated)
                {
                    // Handle NPC-specific defeat logic if any
                }
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
            _healthController.Heal(amount);
        }

        public void Update(GameTime gameTime, IEnumerable<NPC> allNpcs, Character player, IEnumerable<Rectangle> rectangles)
        {
            _currentPlayer = player; // Store for weapon update
            _frameCounter++; // Increment frame counter

            Vector2 playerCenter = new Vector2(player.Bounds.Center.X, player.Bounds.Center.Y);
            Vector2 npcCenter = new Vector2(this.Bounds.Center.X, this.Bounds.Center.Y);
            float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;
            float weaponLength = EquippedWeapon?.Length ?? 32f;
            float attackRange = weaponLength * 0.8f; // Closer to attack range
            // Update knockback first, as it overrides other movement.
            UpdateKnockback(gameTime); 
            _combatComponent.Update(elapsed);

            // Type-specific conditional buffs (example: HeavyKnight enrages under 50% HP)
            if (Class == NpcClass.HeavyKnight && !_enrageApplied && Health <= (MaxHealth / 2))
            {
                _enrageApplied = true;
                ApplyAttackBuff(+2, 10f);
            }

            // Tick down timed attack buffs and remove expired ones
            if (_attackBuffs.Count > 0)
            {
                for (int i = _attackBuffs.Count - 1; i >= 0; i--)
                {
                    var next = _attackBuffs[i].Tick(elapsed);
                    if (next.Expired)
                    {
                        AttackPower -= _attackBuffs[i].Delta;
                        _attackBuffs.RemoveAt(i);
                    }
                    else
                    {
                        _attackBuffs[i] = next;
                    }
                }
            }

            // Visual indicator: pulse while any timed attack buff is active
            if (HasActiveTimedAttackBuff)
            {
                _buffPulseTimer -= elapsed;
                if (_buffPulseTimer <= 0f)
                {
                    Sprite?.Flash(Color.Gold, 0.12);
                    _buffPulseTimer = 0.8f;
                }
            }
            else
            {
                _buffPulseTimer = 0f;
            }

            _combatComponent.CheckPlayerHit(player);

            if (IsDefeated)
            {
                UpdateHealthAndAnimation(gameTime);
                return;
            }

            // --- Enhanced AI Behavior ---
            if (!IsKnockedBack && !IsAttacking)
            {
                switch (_aiType)
                {
                    case NpcAiType.Wander:
                        // Clear any chase target for wandering NPCs
                        MovementComponent.SetChaseTarget(null);
                        break;
                        
                    case NpcAiType.ChasePlayer:
                        {
                            float distanceToPlayer = Vector2.Distance(npcCenter, playerCenter);
                            
                            // FIXED: More aggressive chase behavior
                            if (distanceToPlayer <= MovementComponent.ChaseRange)
                            {
                                
                                
                                
                                // FIXED: Always update chase target for responsive movement
                                // Only skip update if very close to player
                                if (distanceToPlayer > attackRange)
                                {
                                    Vector2 chasePoint = ComputeEngagementPoint(player.Bounds, this.Bounds, attackRange);
                                    
                                    // FIXED: Validate chase point before using it
                                    if (float.IsNaN(chasePoint.X) || float.IsNaN(chasePoint.Y))
                                    {
                                        System.Diagnostics.Debug.WriteLine($"⚠️ NPC {Name}: Invalid chase point detected, skipping chase target update");
                                        MovementComponent.SetChaseTarget(null); // Clear invalid target
                                    }
                                    else
                                    {
                                        MovementComponent.SetChaseTarget(chasePoint, 70f); // Use configured chase speed
                                    }
                                }
                                else
                                {
                                    // Close enough - stop chasing, prepare to attack
                                    MovementComponent.SetChaseTarget(null);
                                }
                            }
                            else if (distanceToPlayer > MovementComponent.LoseTargetRange)
                            {
                                // Lost sight of player
                                MovementComponent.SetChaseTarget(null);
                            }
                            break;
                        }
                }
                
                // Update AI movement
                MovementComponent.UpdateAIMovement(elapsed);
            }
            else
            {
                // If knocked back or attacking, force idle
                MovementComponent.ForceIdle();
            }

            // --- Movement and Collision Resolution ---
            // Use Bounds instead of GetTightSpriteBounds
            if (!IsKnockedBack)
            {
                Vector2 velocity = MovementComponent.GetVelocity();
                if (velocity.LengthSquared() > 0)
                {
                    Vector2 nextPosition = Position + velocity * elapsed;
                    CollisionComponent.TryMove(nextPosition, allNpcs.Where(n => n != this).Cast<Character>().Append(player));
                }
            }

            // Sync the AI's position with the character's final position after collision resolution.
            MovementComponent.SetPosition(this.Position);

            if (IsAttacking)
            {
                _attackAnimTimer -= elapsed;
                if (_attackAnimTimer <= 0)
                {
                    IsAttacking = false;
                }
            }

            // Calculate true center-to-center distance
            float centerDist = Vector2.Distance(npcCenter, playerCenter);

            if (CanAttack && !IsAttacking && centerDist <= attackRange)
            {
                StartAttack();
            }

            // Sync base position from movement
            MovementComponent.SetPosition(MovementComponent.Position);

            // Update facing from current velocity (so direction matches motion)
            var vel = MovementComponent.GetVelocity();
            if (vel.LengthSquared() > 0.0001f)
            {
                _facingDir = AngleToCardinal(MathF.Atan2(vel.Y, vel.X));
                FacingRight = vel.X >= 0f;
                MovementComponent.FacingDirection = _facingDir;
            }

            // Compute movement delta for animation: true only if position actually changed
            _movingForAnim = Vector2.DistanceSquared(MovementComponent.Position, _lastAnimPosition) > 0.01f;

            // Stuck detection logic
            bool isTryingToMove = MovementComponent.GetVelocity().LengthSquared() > 0.01f;
            if (isTryingToMove && !_movingForAnim)
            {
                _stuckFrameCount++;
            }
            else
            {
                _stuckFrameCount = 0; // Reset if moving successfully or intentionally idle
            }

            _lastAnimPosition = MovementComponent.Position;
            UpdateWeapon(gameTime);
            // Defer animation selection until movement/facing are up to date.
            UpdateHealthAndAnimation(gameTime);

            if (_frameCounter % 180 == 0) // Every 3 seconds at 60 FPS
            {
                var velocity = MovementComponent.GetVelocity();
                var speeds = MovementComponent.GetCurrentSpeeds();

                System.Diagnostics.Debug.WriteLine($"NPC {Name}: " +
                    $"Current velocity magnitude = {velocity.Length():F1}, " +
                    $"Configured speeds - W:{speeds.wander}, C:{speeds.chase}, F:{speeds.flee}");

                // Alert if velocity is unexpectedly high
                if (velocity.Length() > NpcSpeedConfiguration.MAX_SPEED)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ WARNING: {Name} velocity {velocity.Length():F1} exceeds max speed {NpcSpeedConfiguration.MAX_SPEED}!");
                }
            }
        }
        public override void Flash()
        {
            AnimationComponent.Flash();
        }
        // In the Update method, store the player reference
        public void StartAttack()
        {
            IsAttacking = true;
            _attackAnimTimer = AttackAnimDuration;

            SetAnimationDirectionalSafe("Attack", "Walk");

            // Use the weapon component to start the swing
            WeaponComponent?.StartSwing(_facingDir);
            
            ResetAttackTimer();
        }
        /// <summary>
        /// Safely sets animation with directional fallback. Tries the primary animation first,
        /// then falls back to the fallback animation if the primary doesn't exist.
        /// </summary>
        /// <param name="primaryAnimation">The preferred animation name to try first</param>
        /// <param name="fallbackAnimation">The fallback animation to use if primary doesn't exist</param>
        private void SetAnimationDirectionalSafe(string primaryAnimation, string fallbackAnimation)
        {
            if (AnimationComponent != null)
            {
                // Try to set the primary animation first
                string currentAnim = AnimationComponent.Sprite?.Animation?.ToString() ?? "";
                AnimationComponent.SetAnimation(primaryAnimation);

                // Check if the animation actually changed (meaning it exists)
                string newAnim = AnimationComponent.Sprite?.Animation?.ToString() ?? "";

                // If animation didn't change and we're not already on the primary, try fallback
                if (currentAnim == newAnim && currentAnim != primaryAnimation)
                {
                    AnimationComponent.SetAnimation(fallbackAnimation);
                }
            }
        }
        public void ApplyAttackBuff(int flatDelta, float durationSeconds)
        {
            if (flatDelta == 0 || durationSeconds <= 0f) return;
            _attackBuffs.Add(new AttackBuff(flatDelta, durationSeconds));
            AttackPower += flatDelta;

            Sprite?.Flash(Color.Gold, 0.35);
            _buffPulseTimer = 0.3f;
        }

        // Override the UpdateWeapon method
        protected override void UpdateWeapon(GameTime gameTime)
        {
            if (_currentPlayer != null && EquippedWeapon != null)
            {
                // Calculate player center for targeting
                //Rectangle playerTightBounds = this.GetTightSpriteBounds();
                //Vector2 playerCenter = new Vector2(
                //    playerTightBounds.Left + playerTightBounds.Width / 2f,
                //    playerTightBounds.Top + playerTightBounds.Height / 2f
                //);
                Vector2 playerCenter = new Vector2(
                    _currentPlayer.Bounds.Center.X,
                    _currentPlayer.Bounds.Center.Y
                );

                // Use the weapon component's update with target position
                _weaponComponent?.Update(gameTime, playerCenter);
            }
            else
            {
                // No target, just update normally
                base.UpdateWeapon(gameTime);
            }
        }

        /// <summary>
        /// Handles projectile hit damage and effects. Returns true if the NPC was defeated.
        /// </summary>
        public bool HandleProjectileHit(int damage, Vector2 knockback)
        {
            return TakeDamage(damage, knockback);
        }

        /// <summary>
        /// Applies a status effect to the NPC (placeholder implementation)
        /// </summary>
        public void ApplyStatusEffect(string effectName)
        {
            // Placeholder implementation - extend as needed
            switch (effectName?.ToLower())
            {
                case "burn":
                    // TODO: Implement burn damage over time
                    Sprite?.Flash(Color.Orange, 0.5f);
                    break;
                case "magic":
                    // TODO: Implement magic effects (slow, confusion, etc.)
                    Sprite?.Flash(Color.Purple, 0.5f);
                    break;
                default:
                    // Unknown effect
                    break;
            }
        }

        //// Replace the ComputeEngagementPoint method with this corrected version
        //private static Vector2 ComputeEngagementPoint(Rectangle playerBounds, Rectangle npcBounds, float desiredStandOff)
        //{
        //    // FIXED: Use exact center calculations to avoid offset issues
        //    Vector2 playerCenter = new Vector2(
        //        playerBounds.X + playerBounds.Width * 0.5f,
        //        playerBounds.Y + playerBounds.Height * 0.5f
        //    );

        //    Vector2 npcCenter = new Vector2(
        //        npcBounds.X + npcBounds.Width * 0.5f,
        //        npcBounds.Y + npcBounds.Height * 0.5f
        //    );

        //    // Calculate direction from NPC to player
        //    Vector2 direction = playerCenter - npcCenter;
        //    float currentDistance = direction.Length();

        //    // If already within attack range, stay put
        //    if (currentDistance <= desiredStandOff)
        //        return npcCenter;

        //    // Normalize direction
        //    if (currentDistance > 0.001f)
        //        direction.Normalize();
        //    else
        //        direction = new Vector2(1, 0);

        //    // FIXED: Calculate the exact position at attack range from player center
        //    // This should position the NPC center at the correct distance from player center
        //    Vector2 targetPosition = playerCenter - direction * desiredStandOff;

        //    return targetPosition;
        //}
        // Add this debugging version temporarily to diagnose the issue
        private static Vector2 ComputeEngagementPoint(Rectangle playerBounds, Rectangle npcBounds, float desiredStandOff)
        {
            Vector2 playerCenter = new Vector2(
                playerBounds.X + playerBounds.Width * 0.5f,
                playerBounds.Y + playerBounds.Height * 0.5f
            );

            Vector2 npcCenter = new Vector2(
                npcBounds.X + npcBounds.Width * 0.5f,
                npcBounds.Y + npcBounds.Height * 0.5f
            );

            // FIXED: Safety checks for invalid positions
            if (float.IsNaN(playerCenter.X) || float.IsNaN(playerCenter.Y) ||
                float.IsNaN(npcCenter.X) || float.IsNaN(npcCenter.Y))
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ WARNING: NaN position detected in ComputeEngagementPoint!");
                return npcCenter; // Return NPC center as fallback
            }

            Vector2 direction = npcCenter - playerCenter; // FIXED: Direction FROM player TO npc (where npc came from)
            float currentDistance = direction.Length();

            // FIXED: Handle zero-distance case (player and NPC at same position)
            if (float.IsNaN(currentDistance) || currentDistance < 0.1f)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ WARNING: Zero distance or NaN in ComputeEngagementPoint! Distance: {currentDistance}");
                // Create a small offset based on random direction to avoid clustering
                float randomAngle = (float)(new Random().NextDouble() * Math.PI * 2);
                direction = new Vector2((float)Math.Cos(randomAngle), (float)Math.Sin(randomAngle));
                currentDistance = 1.0f;
            }

            // If already at ideal attack range, stay put
            if (currentDistance >= desiredStandOff - 2f && currentDistance <= desiredStandOff + 2f)
                return npcCenter;

            // Normalize direction safely
            if (currentDistance > 0.001f)
            {
                direction = direction / currentDistance; // Manual normalization to avoid NaN
            }
            else
            {
                direction = new Vector2(1, 0); // Default direction
            }

            // FIXED: Validate direction vector after normalization
            if (float.IsNaN(direction.X) || float.IsNaN(direction.Y))
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ WARNING: NaN direction after normalization!");
                direction = new Vector2(1, 0); // Default to moving right
            }

            // FIXED: Position the NPC at attack range FROM the player, in the direction the NPC came from
            Vector2 targetPosition = playerCenter + direction * desiredStandOff;

            // FIXED: Final validation of target position
            if (float.IsNaN(targetPosition.X) || float.IsNaN(targetPosition.Y))
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ WARNING: NaN target position detected!");
                return npcCenter; // Return NPC center as safe fallback
            }

            return targetPosition;
        }
        // Helper method to convert angle to cardinal direction
        private static CardinalDirection AngleToCardinal(float angleRadians)
        {
            // Normalize angle to [0, 2π)
            float normalizedAngle = angleRadians;
            while (normalizedAngle < 0) normalizedAngle += MathF.Tau;
            while (normalizedAngle >= MathF.Tau) normalizedAngle -= MathF.Tau;

            // Convert to degrees for easier reasoning
            float degrees = normalizedAngle * (180f / MathF.PI);

            // Map angle ranges to cardinal directions
            // East: -45 to 45 degrees (or 315 to 360 + 0 to 45)
            // South: 45 to 135 degrees  
            // West: 135 to 225 degrees
            // North: 225 to 315 degrees
            return degrees switch
            {
                >= 315f or < 45f => CardinalDirection.East,
                >= 45f and < 135f => CardinalDirection.South,
                >= 135f and < 225f => CardinalDirection.West,
                >= 225f and < 315f => CardinalDirection.North,
                _ => CardinalDirection.East // fallback
            };
        }
        private void UpdateHealthAndAnimation(GameTime gameTime)
        {
            float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Update health controller and get stun state
            bool isStunned = _healthController.Update(elapsed);

            // Update flash effects
            AnimationComponent.Update(elapsed);

            // Skip animation updates if defeated
            if (IsDefeated)
            {
                // Set defeated animation if available, otherwise keep current
                SetAnimationDirectionalSafe("Defeated", "Idle");

                // Apply visual effects for defeated state
                if (Sprite != null)
                {
                    Sprite.Color = Color.White * 0.5f; // Semi-transparent
                }
                return;
            }

            // Determine animation based on current state
            if (IsAttacking)
            {
                SetAnimationDirectionalSafe("Attack", "Idle");
            }
            else if (isStunned)
            {
                SetAnimationDirectionalSafe("Hit", "Idle");
            }
            else
            {
                // Let the MovementAnimationDriver handle idle/walk transitions
                _animDriver.Tick(gameTime, _movingForAnim, isMoving =>
                {
                    SetAnimationDirectionalSafe(isMoving ? "Walk" : "Idle", "Idle");
                }, AnimationComponent.Sprite);
            }

            // Update the animated sprite
            if (AnimationComponent.Sprite != null)
            {
                AnimationComponent.Sprite.Update(gameTime);

                // FIXED: Use the character's actual position directly without offset adjustment
                AnimationComponent.Sprite.Position = Position; // Direct position, no GetContentPosition

                // Apply sprite effects for facing direction
                AnimationComponent.Sprite.Effects = FacingRight
                    ? SpriteEffects.None
                    : SpriteEffects.FlipHorizontally;
            }
        }

        /// <summary>
        /// Updates the obstacles that this NPC should consider for collision detection.
        /// This method caches the obstacles to avoid recalculating them every frame.
        /// </summary>
        /// <param name="obstacleRects">Static obstacle rectangles (walls, etc.)</param>
        /// <param name="allNpcs">All NPCs in the scene for NPC-to-NPC collision avoidance</param>
        /// <param name="player">The player character for collision avoidance</param>
        public void UpdateObstacles(IEnumerable<Rectangle> obstacleRects, IEnumerable<NPC> allNpcs, Character player)
        {
            // Cache obstacles for the movement component to use
            var obstacles = new List<Rectangle>();

            // Add static obstacles (walls, etc.)
            if (obstacleRects != null)
                obstacles.AddRange(obstacleRects);

            // Add player bounds if alive, using Bounds property
            if (player != null && !player.IsDefeated)
                obstacles.Add(player.Bounds);

            // Add other NPC bounds (excluding self), using Bounds property
            if (allNpcs != null)
            {
                foreach (var otherNpc in allNpcs)
                {
                    if (otherNpc != this && !otherNpc.IsDefeated)
                        obstacles.Add(otherNpc.Bounds);
                }
            }

            // Store the obstacles for use by the movement component
            _cachedObstacles = obstacles;
}
        // Add a field to cache the obstacles
        private List<Rectangle> _cachedObstacles = new List<Rectangle>();

        /// <summary>
        /// Gets the cached obstacle rectangles for this NPC.
        /// </summary>
        public IEnumerable<Rectangle> GetCachedObstacles()
        {
            return _cachedObstacles ?? Enumerable.Empty<Rectangle>();
        }

        public AnimatedSprite GetAnimationSprite()
        {
            return AnimationComponent?.Sprite;
        }
    }
}