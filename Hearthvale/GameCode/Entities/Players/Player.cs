using Hearthvale.GameCode.Collision;
using Hearthvale.GameCode.Entities;
using Hearthvale.GameCode.Entities.NPCs;
using Hearthvale.GameCode.Entities.Players.Components;
using Hearthvale.GameCode.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Graphics;
using System.Collections.Generic;
using System.Linq;
using System;
using Hearthvale.GameCode.Entities.Components;
using MonoGame.Extended;

namespace Hearthvale.GameCode.Entities.Players
{
    public class Player : Character
    {
        private readonly TextureAtlas _atlas;
        private PlayerCombatComponent _combatController;
        private PlayerInteractionComponent _interactionComponent;

        private float _weaponOrbitRadius = 3f;
        public float WeaponOrbitRadius => _weaponOrbitRadius;

        public bool IsAttacking { get; set; }
        public PlayerCombatComponent CombatController => _combatController;

        // Track whether movement input occurred this frame
        private bool _movingThisFrame;
        // Track last applied movement state to avoid resetting animation each frame
        private bool _wasMoving;

        public Player(TextureAtlas atlas, Vector2 position, SoundEffect hitSound, SoundEffect defeatSound, SoundEffect playerAttackSound, float movementSpeed)
        {
            // Initialize components
            InitializeComponents();

            _atlas = atlas;

            // Add all 4-directional idle and run animations
            var idleDown = AnimationUtils.WithDelayFactor(atlas.GetAnimation("Idle_Down"), 2.0f);
            var idleUp = AnimationUtils.WithDelayFactor(atlas.GetAnimation("Idle_Up"), 2.0f);
            var idleSide = AnimationUtils.WithDelayFactor(atlas.GetAnimation("Idle_Side"), 2.0f);
            var runDown = AnimationUtils.WithDelayFactor(atlas.GetAnimation("Run_Down"), 2.0f);
            var runUp = AnimationUtils.WithDelayFactor(atlas.GetAnimation("Run_Up"), 2.0f);
            var runSide = AnimationUtils.WithDelayFactor(atlas.GetAnimation("Run_Side"), 2.0f);

            this.AnimationComponent.SetSprite(new AnimatedSprite(idleDown));
            this.AnimationComponent.AddAnimation("Idle_Down", idleDown);
            this.AnimationComponent.AddAnimation("Idle_Up", idleUp);
            this.AnimationComponent.AddAnimation("Idle_Side", idleSide);
            this.AnimationComponent.AddAnimation("Run_Down", runDown);
            this.AnimationComponent.AddAnimation("Run_Up", runUp);
            this.AnimationComponent.AddAnimation("Run_Side", runSide);

            this.MovementComponent.SetPosition(position);
            this.MovementComponent.SetMovementSpeed(movementSpeed);
            this.MovementComponent.FacingRight = true;

            // Initialize health with max health of 100
            InitializeHealth(100);

            Log.Info(LogArea.Player, $"[Player] Created at position: {this.Position}");

            // Create specialized player components
            _combatController = new PlayerCombatComponent(this, hitSound, defeatSound, playerAttackSound);
            _interactionComponent = new PlayerInteractionComponent(this);

            // Set sprite position immediately
            this.MovementComponent.SetPosition(position);
        }

        public override bool TakeDamage(int amount, Vector2? knockback = null)
        {
            if (IsDefeated) return false;

            Log.Info(LogArea.Player, $"[Player] Taking damage: {amount}, knockback={(knockback?.ToString() ?? "null")}");
            bool justDefeated = base.TakeDamage(amount, knockback);
            if (knockback.HasValue)
                this.MovementComponent.SetVelocity(knockback.Value);
            return justDefeated;
        }

        public override void Flash()
        {
            _animationComponent.Flash();
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

            this.AnimationComponent.Update((float)gameTime.ElapsedGameTime.TotalSeconds);
            _combatController.Update(gameTime, npcs);

            // Always update animation - the CharacterAnimationComponent handles optimization internally
            AnimationComponent.UpdateAnimation(_movingThisFrame);
            _wasMoving = _movingThisFrame;

            // Always advance animation frames
            AnimationComponent.Sprite.Update(gameTime);

            // New atlas: sprites top-left aligned; draw sprite directly at logical position
            AnimationComponent.Sprite.Position = this.Position;
            AnimationComponent.Sprite.Effects = FacingRight ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

            Log.VerboseThrottled(LogArea.Player,
                $"[Player] Pos={Position} SpritePos={AnimationComponent.Sprite.Position} FacingRight={MovementComponent.FacingRight}",
                TimeSpan.FromMilliseconds(250));

            if (float.IsNaN(this.Position.X) || float.IsNaN(this.Position.Y))
            {
                Log.Error(LogArea.Player, "❌ CRITICAL: Player position is NaN at end of Update! Resetting.");
                this.SetPosition(new Vector2(896, 80));
            }

            // Update weapon position and state - this calls Character.UpdateWeapon
            UpdateWeapon(gameTime);

            // Clear the movement flag at the end of the frame
            _movingThisFrame = false;
        }

        public void Move(
            Vector2 movement,
            Rectangle roomBounds,
            float boundsWidth,   // Changed parameter name
            float boundsHeight,  // Changed parameter name  
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
            }

            Vector2 newPosition = Position + movement;
            if (float.IsNaN(newPosition.X) || float.IsNaN(newPosition.Y))
            {
                Log.Error(LogArea.Player, $"❌ CRITICAL: newPosition is NaN! Position={Position}, movement={movement}");
                return;
            }

            float clampedX = MathHelper.Clamp(newPosition.X, roomBounds.Left, roomBounds.Right - boundsWidth);
            float clampedY = MathHelper.Clamp(newPosition.Y, roomBounds.Top, roomBounds.Bottom - boundsHeight);
            Vector2 candidate = new Vector2(clampedX, clampedY);

            // Build collision obstacles - ONLY character bodies, never weapons
            var allObstacles = (obstacleRects ?? Enumerable.Empty<Rectangle>()).ToList();
            //foreach (var npc in npcs)
            //{
            //    if (!npc.IsDefeated)
            //    {
            //        // Use only the NPC's body bounds, not weapon bounds
            //        allObstacles.Add(npc.GetTightSpriteBounds());
            //    }
            //}

            // Use physics collision (walls, chests, characters) without injecting legacy obstacle list
            bool moved = CollisionComponent.TryMove(candidate, null);
            if (!moved)
            {
                Log.Verbose(LogArea.Player, "[Player.Move] Movement blocked by collision (wall slide).");
            }
        }

        public bool IsNearTile(int column, int row, float tileWidth, float tileHeight)
        {
            return _interactionComponent.IsNearTile(column, row, tileWidth, tileHeight);
        }

        public void SetFacingRight(bool facingRight)
        {
            MovementComponent.FacingRight = facingRight;
        }

        protected override Vector2 GetAttackDirection()
        {
            return MovementComponent.FacingDirection.ToVector();
        }

        public override Rectangle Bounds 
        { 
            get 
            {
                return this.GetTightSpriteBounds();
            }
        }
    }
}