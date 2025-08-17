using Hearthvale.GameCode.Entities.Interfaces;
using Hearthvale.GameCode.Entities.NPCs.Components;
using Hearthvale.GameCode.Entities.Animations;
using Hearthvale.GameCode.Utils;
using Hearthvale.GameCode.Managers; // added
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

    // Tag to identify NPC combat class (separate from AI)
    public enum NpcClass
    {
        Merchant,
        Knight,
        HeavyKnight
    }

    public class NPC : Character, ICombatNpc, IDialog
    {
        private readonly NpcAnimationController _animationController;
        private readonly NpcMovementComponent _npcMovement;
        private readonly NpcHealthController _healthController;
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

        public override Rectangle Bounds => this.GetTightSpriteBounds();

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

        public NPC(string name, Dictionary<string, Animation> animations, Vector2 position, Rectangle bounds, SoundEffect defeatSound, int maxHealth)
        {
            Name = name;
            var sprite = new AnimatedSprite(animations["Idle"]);

            // Initialize base class components first
            InitializeComponents();

            // Create specialized NPC components
            _animationController = new NpcAnimationController(sprite, animations);
            _npcMovement = new NpcMovementComponent(this, position, 60.0f, bounds, (int)sprite.Width, (int)sprite.Height);
            _healthController = new NpcHealthController(maxHealth, defeatSound);
            _combatComponent = new NpcCombatComponent(this, AttackPower); // synced by property setter later too

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

        public override bool TakeDamage(int amount, Vector2? knockback = null)
        {
            if (_healthController.CanTakeDamage)
            {
                bool justDefeated = _healthController.TakeDamage(amount);

                if (knockback.HasValue)
                    _npcMovement.SetVelocity(knockback.Value);

                Flash();
                return justDefeated;
            }
            else
            {
                Log.Info(LogArea.NPC, $"----[NPC.{Name}] CanTakeDamage is FALSE. Damage blocked.");            }
            return false;
        }

        public override void Heal(int amount)
        {
            _healthController.Heal(amount);
        }

        public void Update(GameTime gameTime, IEnumerable<NPC> allNpcs, Character player, IEnumerable<Rectangle> rectangles)
        {
            _frameCounter++; // Increment frame counter
            float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;

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

            if (_healthController.IsStunned)
            {
                UpdateKnockback(gameTime);
                SetPosition(Position);
                UpdateHealthAndAnimation(gameTime);
                return;
            }

            if (IsAttacking)
            {
                _attackAnimTimer -= elapsed;
                if (_attackAnimTimer <= 0)
                {
                    IsAttacking = false;
                }
            }

            // --- AI Behavior ---
            switch (_aiType)
            {
                case NpcAiType.Wander:
                    _npcMovement.SetChaseTarget(null);
                    break;
                // In the Update method, replace the chase target calculation block with:
                case NpcAiType.ChasePlayer:
                {
                    // Use a smaller standoff distance to get closer
                    float weaponLength = EquippedWeapon?.Length ?? 32f;
                    float desiredStandOff = MathF.Max(weaponLength * 0.5f, 8f); // Reduced from previous calculation

                    // Update chase target less frequently for performance
                    if (_frameCounter % 5 == 0)
                    {
                        Vector2 chasePoint = ComputeEngagementPoint(player.GetOrientationAwareBounds(),
                            this.GetOrientationAwareBounds(), desiredStandOff);
                        _npcMovement.SetChaseTarget(chasePoint);
                    }
                    break;
                }
            }

            // Use the current bounds to compute per-frame hitbox offset and size
            Rectangle currentBounds = Bounds; // This will now use the analyzed sprite bounds

            // Calculate dynamic offsets based on analyzed content bounds
            int hitboxOffsetX = currentBounds.Left - (int)Position.X;
            int hitboxOffsetY = currentBounds.Top - (int)Position.Y;
            int hitboxWidth = currentBounds.Width;
            int hitboxHeight = currentBounds.Height;

            // Replace this block in the Update method:
            _npcMovement.Update(elapsed, candidatePos =>
            {
                var allObstacles = rectangles.ToList();

                if (!player.IsDefeated)
                    allObstacles.Add(player.GetOrientationAwareBounds());

                foreach (var npc in allNpcs)
                {
                    if (npc != this && !npc.IsDefeated)
                        allObstacles.Add(npc.GetOrientationAwareBounds());
                }

                // Create collision rectangle at candidate position using orientation-aware bounds
                Rectangle currentBounds = this.GetOrientationAwareBounds();
                int offsetX = currentBounds.Left - (int)Position.X;
                int offsetY = currentBounds.Top - (int)Position.Y;

                Rectangle candidateRect = new Rectangle(
                    (int)candidatePos.X + offsetX,
                    (int)candidatePos.Y + offsetY,
                    currentBounds.Width,
                    currentBounds.Height
                );

                return allObstacles.Any(r => candidateRect.Intersects(r));
            });

            UpdateWeapon(gameTime, player);

            // Attack range check (body-to-body distance) using analyzed bounds
            Vector2 npcCenterForAttack = GetPixelHitboxCenter(this.Bounds);
            Vector2 playerCenterForAttack = GetPixelHitboxCenter(player.Bounds);
            float attackRange = MathF.Max(EquippedWeapon?.Length ?? 32f, 32f);

            // Calculate true center-to-center distance based on analyzed content
            float centerDist = Vector2.Distance(npcCenterForAttack, playerCenterForAttack);

            if (CanAttack && !IsAttacking && centerDist <= attackRange)
            {
                StartAttack();
            }

            // Sync base position from movement
            MovementComponent.SetPosition(_npcMovement.Position);

            // Update facing from current velocity (so direction matches motion)
            var vel = _npcMovement.GetVelocity();
            if (vel.LengthSquared() > 0.0001f)
            {
                _facingDir = AngleToCardinal(MathF.Atan2(vel.Y, vel.X));
                FacingRight = vel.X >= 0f;
                MovementComponent.FacingDirection = _facingDir;
                
                // REMOVED: Debug logging for performance
            }

            // --- Hard resolve any overlap with player or other NPCs ---
            Rectangle myBounds = this.GetOrientationAwareBounds(); // Use orientation-aware bounds

            if (!player.IsDefeated)
            {
                Rectangle playerBounds = player.GetOrientationAwareBounds(); // Use orientation-aware bounds
                if (myBounds.Intersects(playerBounds))
                {
                    Vector2 separation = ComputeSeparationVector(myBounds, playerBounds);
                    if (separation != Vector2.Zero)
                    {
                        // Test if separation would put us in a wall
                        Vector2 candidatePosition = Position + separation;
                        
                        // Use tilemap bounds check to avoid pushing through walls
                        bool wouldHitWall = IsWallAtPoint(candidatePosition);
                        
                        if (!wouldHitWall)
                        {
                            // Only apply separation if it won't push through walls
                            _npcMovement.SetPosition(candidatePosition);
                            MovementComponent.SetPosition(candidatePosition);
                        }
                    }
                }
            }

            // Same for NPC-to-NPC collisions
            foreach (var npc in allNpcs)
            {
                if (npc == this || npc.IsDefeated)
                    continue;
                
                Rectangle otherBounds = npc.GetOrientationAwareBounds(); // Use orientation-aware bounds
                if (myBounds.Intersects(otherBounds))
                {
                    Vector2 separation = ComputeSeparationVector(myBounds, otherBounds);
                    if (separation != Vector2.Zero)
                    {
                        // Test if separation would put us in a wall
                        Vector2 candidatePosition = Position + separation;
                        
                        // Use tilemap bounds check to avoid pushing through walls
                        bool wouldHitWall = IsWallAtPoint(candidatePosition);
                        
                        if (!wouldHitWall)
                        {
                            // Only apply separation if it won't push through walls
                            _npcMovement.SetPosition(candidatePosition);
                            MovementComponent.SetPosition(candidatePosition);
                        }
                    }
                }
            }

            // Compute movement delta for animation: true only if position actually changed
            _movingForAnim = Vector2.DistanceSquared(_npcMovement.Position, _lastAnimPosition) > 0.01f;
            _lastAnimPosition = _npcMovement.Position;

            // Defer animation selection until movement/facing are up to date.
            UpdateHealthAndAnimation(gameTime);
        }
        public override void Flash()
        {
            _animationController?.Flash();
        }
        public void StartAttack()
        {
            IsAttacking = true;
            _attackAnimTimer = AttackAnimDuration;

            bool swingClockwise = _facingDir switch
            {
                CardinalDirection.North => true,
                CardinalDirection.East => true,
                CardinalDirection.South => false,
                CardinalDirection.West => false,
                _ => true
            };

            SetAnimationDirectionalSafe("Attack", "Walk");

            EquippedWeapon?.StartSwing(swingClockwise);
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
            if (_animationController != null)
            {
                // Try to set the primary animation first
                string currentAnim = _animationController.Sprite?.Animation?.ToString() ?? "";
                _animationController.SetAnimation(primaryAnimation);

                // Check if the animation actually changed (meaning it exists)
                string newAnim = _animationController.Sprite?.Animation?.ToString() ?? "";

                // If animation didn't change and we're not already on the primary, try fallback
                if (currentAnim == newAnim && currentAnim != primaryAnimation)
                {
                    _animationController.SetAnimation(fallbackAnimation);
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

        private void UpdateWeapon(GameTime gameTime, Character player)
        {
            if (EquippedWeapon == null) return;

            // Use dynamic content bounds for both NPC and player
            Vector2 npcCenter = GetPixelHitboxCenter(this.Bounds);
            Vector2 playerCenter = GetPixelHitboxCenter(player.Bounds);

            Vector2 toPlayer = playerCenter - npcCenter;
            if (toPlayer.LengthSquared() > 0.0001f)
            {
                float angle = MathF.Atan2(toPlayer.Y, toPlayer.X);

                _facingDir = AngleToCardinal(angle);

                if (!IsAttacking)
                    EquippedWeapon.Rotation = _facingDir.ToRotation();

                FacingRight = toPlayer.X >= 0f;

                // Position weapon based on actual visual center
                EquippedWeapon.Position = npcCenter + EquippedWeapon.Offset + EquippedWeapon.ManualOffset;

                EquippedWeapon.Update(gameTime);
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

        // Helper to get the true visual center of a hitbox based on analyzed content
        private static Vector2 GetPixelHitboxCenter(Rectangle bounds)
        {
            // Calculate center point using the analyzed content bounds
            return new Vector2(
                bounds.Left + bounds.Width / 2f, 
                bounds.Top + bounds.Height / 2f
            );
        }

        // Replace the ComputeEngagementPoint method with this improved version
        private static Vector2 ComputeEngagementPoint(Rectangle playerBounds, Rectangle npcBounds, float desiredStandOff)
        {
            // Use orientation-aware centers
            Vector2 playerCenter = new Vector2(
                playerBounds.Left + playerBounds.Width / 2f,
                playerBounds.Top + playerBounds.Height / 2f
            );
            
            Vector2 npcCenter = new Vector2(
                npcBounds.Left + npcBounds.Width / 2f,
                npcBounds.Top + npcBounds.Height / 2f
            );

            // Calculate direction from player to NPC
            Vector2 direction = npcCenter - playerCenter;
            float currentDistance = direction.Length();
            
            // Allow NPCs to get closer - reduce the standoff distance
            float actualStandOff = MathF.Max(desiredStandOff * 0.6f, 8f);
            
            // If close enough, just target the player's position directly
            if (currentDistance <= actualStandOff)
                return playerCenter;
                
            // Normalize direction
            if (currentDistance > 0.001f)
                direction /= currentDistance;
            else
                direction = new Vector2(1, 0); // Default direction
        
            // Calculate position at adjusted standoff distance
            return playerCenter + direction * actualStandOff;
        }

        // Lightweight wall test at a point (tile-based). Keeps chase targets out of walls.
        private static bool IsWallAtPoint(Vector2 worldPos)
        {
            var tilemap = TilesetManager.Instance.Tilemap;
            if (tilemap == null) return false;

            // Convert world position to tile coordinates
            int col = (int)(worldPos.X / tilemap.TileWidth);
            int row = (int)(worldPos.Y / tilemap.TileHeight);

            // Check bounds
            if (col < 0 || row < 0 || col >= tilemap.Columns || row >= tilemap.Rows)
                return true; // treat out-of-bounds as wall

            var ts = tilemap.GetTileset(col, row);
            var id = tilemap.GetTileId(col, row);
            return ts == TilesetManager.Instance.WallTileset && AutotileMapper.IsWallTile(id);
        }

        private static Vector2 ComputeSeparationVector(Rectangle a, Rectangle b)
        {
            if (!a.Intersects(b))
                return Vector2.Zero;

            int overlapX = Math.Min(a.Right, b.Right) - Math.Max(a.Left, b.Left);
            int overlapY = Math.Min(a.Bottom, b.Bottom) - Math.Max(a.Top, b.Top);

            // Prefer horizontal separation when overlaps are equal
            if (overlapX <= overlapY)
            {
                // Move left or right
                if (a.Center.X < b.Center.X)
                    return new Vector2(-overlapX, 0);
                else
                    return new Vector2(overlapX, 0);
            }
            else
            {
                // Move up or down
                if (a.Center.Y < b.Center.Y)
                    return new Vector2(0, -overlapY);
                else
                    return new Vector2(0, overlapY);
            }
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
            _animationController.UpdateFlash(elapsed);

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
            string targetAnimation;

            if (IsAttacking)
            {
                targetAnimation = "Attack";
            }
            else if (isStunned)
            {
                targetAnimation = "Hit";
            }
            else if (_movingForAnim)
            {
                targetAnimation = "Walk";
            }
            else
            {
                targetAnimation = "Idle";
            }

            // Apply animation with fallback safety
            SetAnimationDirectionalSafe(targetAnimation, "Idle");

            // Update the animated sprite
            if (_animationController.Sprite != null)
            {
                _animationController.Sprite.Update(gameTime);
                
                // Use the dynamic content position instead of hard-coded offsets
                Vector2 adjustedPosition = _animationController.Sprite.GetContentPosition(Position);
                _animationController.Sprite.Position = adjustedPosition;
                
                // Apply sprite effects for facing direction
                _animationController.Sprite.Effects = FacingRight
                    ? SpriteEffects.None
                    : SpriteEffects.FlipHorizontally;
            }

            // Update movement animation driver for smooth transitions
            _animDriver.Tick(gameTime, _movingForAnim, isMoving =>
            {
                if (!IsAttacking && !isStunned)
                {
                    SetAnimationDirectionalSafe(isMoving ? "Walk" : "Idle", "Idle");
                }
            }, _animationController.Sprite);
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

            // Add player bounds if alive, using orientation-aware bounds
            if (player != null && !player.IsDefeated)
                obstacles.Add(player.GetOrientationAwareBounds());

            // Add other NPC bounds (excluding self), using orientation-aware bounds
            if (allNpcs != null)
            {
                foreach (var otherNpc in allNpcs)
                {
                    if (otherNpc != this && !otherNpc.IsDefeated)
                        obstacles.Add(otherNpc.GetOrientationAwareBounds());
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

        public void DrawMovementDebug(SpriteBatch spriteBatch, Texture2D pixel)
        {
            if (!DebugManager.Instance.ShowCollisionBoxes || spriteBatch == null || pixel == null) 
                return;
            
            // Nothing to draw if no sprite
            if (Sprite == null) return;
            
            // Get the sprite's actual rendered position
            Vector2 spriteRenderPosition = Position;
            if (Sprite is AnimatedSprite animSprite)
            {
                // This position accounts for content offsets
                spriteRenderPosition = animSprite.GetContentPosition(Position);
            }
            
            // Get collision bounds
            Rectangle tightBounds = this.GetTightSpriteBounds();
            Rectangle orientedBounds = this.GetOrientationAwareBounds();
            
            // Calculate the offset between logical position and render position
            Vector2 renderOffset = spriteRenderPosition - Position;
            
            // Draw full sprite bounds for reference (cyan)
            Rectangle spriteBounds = new Rectangle(
                (int)spriteRenderPosition.X, 
                (int)spriteRenderPosition.Y, 
                (int)Sprite.Width, 
                (int)Sprite.Height
            );
            DrawDebugRect(spriteBatch, pixel, spriteBounds, Color.Cyan * 0.3f);
            
            // Special handling for flipped sprites (facing left)
            if (!FacingRight && Sprite != null)
            {
                // When flipped horizontally, we need to mirror the bounds around the sprite's center X
                int spriteCenter = (int)spriteRenderPosition.X + (int)Sprite.Width / 2;
                
                // Copy bounds before modifying
                Rectangle adjustedTight = tightBounds;
                Rectangle adjustedOriented = orientedBounds;
                
                // Apply render offset first
                adjustedTight.Offset((int)renderOffset.X, (int)renderOffset.Y);
                adjustedOriented.Offset((int)renderOffset.X, (int)renderOffset.Y);
                
                // Calculate distance from left edge to center and from right edge to center
                int tightLeftDist = spriteCenter - adjustedTight.Left;
                int tightRightDist = adjustedTight.Right - spriteCenter;
                int orientedLeftDist = spriteCenter - adjustedOriented.Left;
                int orientedRightDist = adjustedOriented.Right - spriteCenter;
                
                // Swap left/right distances to mirror around center
                adjustedTight.X = spriteCenter - tightRightDist;
                adjustedTight.Width = tightLeftDist + tightRightDist;
                
                adjustedOriented.X = spriteCenter - orientedRightDist;
                adjustedOriented.Width = orientedLeftDist + orientedRightDist;
                
                // Draw the mirrored bounds
                DrawDebugRect(spriteBatch, pixel, adjustedTight, Color.Red * 0.5f);
                DrawDebugRect(spriteBatch, pixel, adjustedOriented, Color.Orange * 0.6f);
            }
            else
            {
                // Standard rendering for right-facing sprites
                tightBounds.Offset((int)renderOffset.X, (int)renderOffset.Y);
                orientedBounds.Offset((int)renderOffset.X, (int)renderOffset.Y);
                
                DrawDebugRect(spriteBatch, pixel, tightBounds, Color.Red * 0.5f);
                DrawDebugRect(spriteBatch, pixel, orientedBounds, Color.Orange * 0.6f);
            }
            
            // Draw center markers using the adjusted sprite center
            Vector2 spriteCenter = new Vector2(
                spriteRenderPosition.X + Sprite.Width/2f, 
                spriteRenderPosition.Y + Sprite.Height/2f
            );
            DrawDebugCross(spriteBatch, pixel, spriteCenter, Color.White, 4);
            
            // Draw movement indicators
            if (_npcMovement != null)
            {
                var velocity = _npcMovement.GetVelocity();
                if (velocity != Vector2.Zero)
                {
                    Vector2 normalizedVel = Vector2.Normalize(velocity) * 20f;
                    DrawDebugLine(spriteBatch, pixel, spriteCenter, spriteCenter + normalizedVel, Color.Yellow);
                }
                
                if (_npcMovement.IsStuck)
                {
                    DrawDebugCircle(spriteBatch, pixel, spriteCenter, 10, Color.Red * 0.5f);
                }
            }
        }

        private void DrawDebugLine(SpriteBatch spriteBatch, Texture2D pixel, Vector2 start, Vector2 end, Color color)
        {
            Vector2 edge = end - start;
            float angle = (float)Math.Atan2(edge.Y, edge.X);
            spriteBatch.Draw(
                pixel,
                start,
                null,
                color,
                angle,
                Vector2.Zero,
                new Vector2(edge.Length(), 1),
                SpriteEffects.None,
                0);
        }

        private void DrawDebugCross(SpriteBatch spriteBatch, Texture2D pixel, Vector2 center, Color color, int size)
        {
            spriteBatch.Draw(pixel, new Rectangle((int)center.X - size, (int)center.Y, size * 2, 1), color);
            spriteBatch.Draw(pixel, new Rectangle((int)center.X, (int)center.Y - size, 1, size * 2), color);
        }

        private void DrawDebugRect(SpriteBatch spriteBatch, Texture2D pixel, Rectangle rect, Color color)
        {
            // Draw outline
            spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, 1), color);
            spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Bottom - 1, rect.Width, 1), color);
            spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, 1, rect.Height), color);
            spriteBatch.Draw(pixel, new Rectangle(rect.Right - 1, rect.Y, 1, rect.Height), color);
        }

        private void DrawDebugCircle(SpriteBatch spriteBatch, Texture2D pixel, Vector2 position, float radius, Color color)
        {
            spriteBatch.Draw(
                pixel,
                new Rectangle((int)(position.X - radius), (int)(position.Y - radius), (int)(radius * 2), (int)(radius * 2)),
                color);
        }
    }
}