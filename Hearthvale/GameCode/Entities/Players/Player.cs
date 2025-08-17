using Hearthvale.GameCode.Entities;
using Hearthvale.GameCode.Entities.NPCs;
using Hearthvale.GameCode.Entities.Players.Components;
using Hearthvale.GameCode.Managers;
using Hearthvale.GameCode.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Animations;
using MonoGameLibrary.Graphics;
using System.Collections.Generic;
using System.Linq;
using System;

namespace Hearthvale.GameCode.Entities.Players
{
    public class Player : Character
    {
        private readonly TextureAtlas _atlas;
        private PlayerCombatComponent _combatController;
        private PlayerMovementComponent _movementController;
        private PlayerAnimationComponent _animationController;
        private PlayerCollisionComponent _collisionController;
        private PlayerInteractionComponent _interactionController;

        private float _weaponOrbitRadius = 3f;
        public float WeaponOrbitRadius => _weaponOrbitRadius;

        public bool IsAttacking { get; set; }
        public PlayerCombatComponent CombatController => _combatController;

        // Track whether movement input occurred this frame
        private bool _movingThisFrame;
        // Track last applied movement state to avoid resetting animation each frame
        private bool _wasMoving;

        // Cache npcs for obstacle filtering during knockback (no weapon shapes will be used)
        private IEnumerable<NPC> _currentNpcsForObstacles;

        public Player(TextureAtlas atlas, Vector2 position, SoundEffect hitSound, SoundEffect defeatSound, SoundEffect playerAttackSound, float movementSpeed)
        {
            // Initialize components
            InitializeComponents();

            _atlas = atlas;
            this.AnimationComponent.SetSprite(new AnimatedSprite(atlas.GetAnimation("Mage_Idle")));
            this.MovementComponent.SetPosition(position);
            this.MovementComponent.SetMovementSpeed(movementSpeed);
            this.MovementComponent.FacingRight = true;

            // Initialize health with max health of 100
            InitializeHealth(100);

            Log.Info(LogArea.Player, $"[Player] Created at position: {this.Position}");

            // Create specialized player components
            _movementController = new PlayerMovementComponent(this);
            _combatController = new PlayerCombatComponent(this, hitSound, defeatSound, playerAttackSound);
            _collisionController = new PlayerCollisionComponent(this);
            _interactionController = new PlayerInteractionComponent(this);

            var animations = new Dictionary<string, Animation>
            {
                { "Mage_Idle", atlas.GetAnimation("Mage_Idle") },
                { "Mage_Walk", atlas.GetAnimation("Mage_Walk") }
            };
            _animationController = new PlayerAnimationComponent(this, this.AnimationComponent.Sprite, animations);

            // Set sprite position immediately
            this.SetPosition(position);
        }

        public override bool TakeDamage(int amount, Vector2? knockback = null)
        {
            if (IsDefeated) return false;

            Log.Info(LogArea.Player, $"[Player] Taking damage: {amount}, knockback={(knockback?.ToString() ?? "null")}");
            bool justDefeated = base.TakeDamage(amount, knockback);
            if (knockback.HasValue)
                _movementController.SetVelocity(knockback.Value);
            return justDefeated;
        }

        public override void Flash()
        {
            _animationController.Flash();
        }

        public void StartAttack()
        {
            IsAttacking = true;
            Log.Verbose(LogArea.Player, "[Player] StartAttack()");
        }

        public void Update(GameTime gameTime, IEnumerable<NPC> npcs)
        {
            // Add position validation at the start
            if (float.IsNaN(this.Position.X) || float.IsNaN(this.Position.Y))
            {
                Log.Error(LogArea.Player, "❌ CRITICAL: Player position is NaN at start of Update! Resetting to spawn position.");
                this.SetPosition(new Vector2(896, 80));
            }

            UpdateKnockback(gameTime); // Handles knockback and wall bounce

            // Check after knockback update
            if (float.IsNaN(Position.X) || float.IsNaN(Position.Y))
            {
                Log.Error(LogArea.Player, "❌ CRITICAL: Player position became NaN after UpdateKnockback! Resetting.");
                this.SetPosition(new Vector2(896, 80));
                _collisionComponent.SetKnockback(Vector2.Zero);
            }

            _animationController.UpdateFlash((float)gameTime.ElapsedGameTime.TotalSeconds);
            _combatController.Update(gameTime, npcs);

            // Apply animation only when movement state changes (prevents constant resets)
            if (_movingThisFrame != _wasMoving)
            {
                _animationController.UpdateAnimation(_movingThisFrame);
                _wasMoving = _movingThisFrame;
            }

            // Always advance animation frames
            AnimationComponent.Sprite.Update(gameTime);

            AnimationComponent.Sprite.Position = this.Position;
            AnimationComponent.Sprite.Effects = FacingRight ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

            Log.VerboseThrottled(LogArea.Player,
                $"[Player] Pos={Position} FacingRight={MovementComponent.FacingRight}",
                TimeSpan.FromMilliseconds(250));

            if (float.IsNaN(this.Position.X) || float.IsNaN(this.Position.Y))
            {
                Log.Error(LogArea.Player, "❌ CRITICAL: Player position is NaN at end of Update! Resetting.");
                this.SetPosition(new Vector2(896, 80));
            }

            // Clear the movement flag at the end of the frame
            _movingThisFrame = false;
        }

        public void Move(
            Vector2 movement,
            Rectangle roomBounds,
            float spriteWidth,
            float spriteHeight,
            IEnumerable<NPC> npcs,
            IEnumerable<Rectangle> obstacleRects)
        {
            if (_collisionComponent.IsKnockedBack) return;

            if (movement != Vector2.Zero)
            {
                _movingThisFrame = true;

                MovementComponent.LastMovementVector = movement.LengthSquared() > 0
                    ? Vector2.Normalize(movement)
                    : MovementComponent.LastMovementVector;

                MovementComponent.FacingDirection = movement.ToCardinalDirection();

                if (movement.X != 0)
                    MovementComponent.FacingRight = movement.X > 0;

                Log.VerboseThrottled(LogArea.Player,
                    $"[Player.Move] input={movement} facingDir={MovementComponent.FacingDirection} facingRight={MovementComponent.FacingRight}",
                    TimeSpan.FromMilliseconds(200));
            }

            Vector2 newPosition = Position + movement;

            if (float.IsNaN(newPosition.X) || float.IsNaN(newPosition.Y))
            {
                Log.Error(LogArea.Player, $"❌ CRITICAL: newPosition is NaN! Position={Position}, movement={movement}");
                return;
            }

            float clampedX = MathHelper.Clamp(newPosition.X, roomBounds.Left, roomBounds.Right - spriteWidth);
            float clampedY = MathHelper.Clamp(newPosition.Y, roomBounds.Top, roomBounds.Bottom - spriteHeight);
            Vector2 candidate = new Vector2(clampedX, clampedY);

            if (float.IsNaN(candidate.X) || float.IsNaN(candidate.Y))
            {
                Log.Error(LogArea.Player, "❌ CRITICAL: candidate position is NaN after clamping!");
                return;
            }

            // In the Move method, update the obstacle collection:
            var allObstacles = (obstacleRects ?? Enumerable.Empty<Rectangle>()).ToList();
            foreach (var npc in npcs)
            {
                if (!npc.IsDefeated)
                    allObstacles.Add(npc.GetOrientationAwareBounds());
            }

            // Note: weapon rectangles are intentionally ignored for movement
            bool moved = _collisionController.TrySetPositionWithWallSliding(candidate, allObstacles);
            if (!moved)
            {
                Log.Verbose(LogArea.Player, "[Player.Move] Movement blocked by collision (wall slide).");
            }
        }

        public bool IsNearTile(int column, int row, float tileWidth, float tileHeight)
        {
            return _interactionController.IsNearTile(column, row, tileWidth, tileHeight);
        }

        public void SetFacingRight(bool facingRight)
        {
            MovementComponent.FacingRight = facingRight;
        }

        protected override Vector2 GetAttackDirection()
        {
            return MovementComponent.FacingDirection.ToVector();
        }

        public void UpdateObstacles(IEnumerable<Rectangle> obstacleRects, IEnumerable<NPC> npcs)
        {
            _collisionController.UpdateObstacles(obstacleRects, npcs);
            _currentNpcsForObstacles = npcs;

            if (obstacleRects != null || (npcs != null && npcs.Any()))
            {
                int staticCount = obstacleRects?.Count() ?? 0;
                int npcCount = npcs?.Count(n => !n.IsDefeated) ?? 0;
                Log.VerboseThrottled(LogArea.Player, $"[Player] Obstacles updated: static={staticCount}, npcs={npcCount}", TimeSpan.FromMilliseconds(500));
            }
        }

        public override IEnumerable<Rectangle> GetObstacleRectangles()
        {
            // Only bodies and static obstacles block knockback; no weapon AABBs involved
            var baseObstacles = _collisionController.GetObstacleRectangles() ?? Enumerable.Empty<Rectangle>();
            return baseObstacles;
        }

        public override Rectangle Bounds => this.GetTightSpriteBounds();
    }
}