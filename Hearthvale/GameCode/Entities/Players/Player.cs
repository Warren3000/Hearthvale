using Hearthvale.GameCode.Entities;
using Hearthvale.GameCode.Entities.NPCs;
using Hearthvale.GameCode.Managers;
using Hearthvale.GameCode.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended.Animations;
using MonoGameLibrary.Graphics;
using System.Collections.Generic;
using System.Linq;

namespace Hearthvale.GameCode.Entities.Players
{
    public class Player : Character
    {
        private readonly TextureAtlas _atlas;
        private PlayerCombatController _combatController;
        private PlayerMovementController _movementController;
        private CombatEffectsManager _effectsManager;
        private readonly PlayerAnimationController _animationController;

        public bool IsAttacking { get; set; }

        private float _weaponOrbitRadius = 3f;
        public float WeaponOrbitRadius => _weaponOrbitRadius;

        public PlayerCombatController CombatController => _combatController;

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

            System.Diagnostics.Debug.WriteLine($"Player created at position: {this.Position}");

            _movementController = new PlayerMovementController(this);
            _combatController = new PlayerCombatController(this, hitSound, defeatSound, playerAttackSound);

            var animations = new Dictionary<string, Animation>
            {
                { "Mage_Idle", atlas.GetAnimation("Mage_Idle") },
                { "Mage_Walk", atlas.GetAnimation("Mage_Walk") }
            };
            _animationController = new PlayerAnimationController(this, this.AnimationComponent.Sprite, animations);

            // Set sprite position immediately
            this.SetPosition(position);
        }

        public override bool TakeDamage(int amount, Vector2? knockback = null)
        {
            if (IsDefeated) return false;
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
        }

        public void Update(GameTime gameTime, IEnumerable<NPC> npcs)
        {
            // Add position validation at the start
            if (float.IsNaN(this.Position.X) || float.IsNaN(this.Position.Y))
            {
                System.Diagnostics.Debug.WriteLine($"❌ CRITICAL: Player position is NaN at start of Update! Resetting to spawn position.");
                this.SetPosition(new Vector2(896, 80)); // Use the known good spawn position
            }

            UpdateKnockback(gameTime); // Handles knockback and wall bounce

            // Check after knockback update
            if (float.IsNaN(Position.X) || float.IsNaN(Position.Y))
            {
                System.Diagnostics.Debug.WriteLine($"❌ CRITICAL: Player position became NaN after UpdateKnockback!");
                this.SetPosition(new Vector2(896, 80)); // Reset again
                _collisionComponent.SetKnockback(Vector2.Zero);
            }

            _animationController.UpdateFlash((float)gameTime.ElapsedGameTime.TotalSeconds);
            _combatController.Update(gameTime, npcs);
            _animationController.UpdateAnimation(_movementController.IsMoving());
            AnimationComponent.Sprite.Position = this.Position;
            AnimationComponent.Sprite.Effects = FacingRight ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

            // Final validation
            if (float.IsNaN(this.Position.X) || float.IsNaN(this.Position.Y))
            {
                System.Diagnostics.Debug.WriteLine($"❌ CRITICAL: Player position is NaN at end of Update!");
                this.SetPosition(new Vector2(896, 80)); // Final reset
            }
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

    // Convert continuous movement vector to cardinal direction and back
    if (movement != Vector2.Zero)
    {
        // Convert to cardinal direction
        CardinalDirection direction = movement.ToCardinalDirection();
        
        // Update the MovementComponent with the cardinal direction
        MovementComponent.FacingDirection = direction;
        MovementComponent.LastMovementVector = direction.ToVector();
    }

    // Create movement vector using cardinal direction's unit vector
    Vector2 directedMovement = movement.Length() * MovementComponent.LastMovementVector;
    Vector2 newPosition = Position + directedMovement;

    // Add NaN check and prevention
    if (float.IsNaN(newPosition.X) || float.IsNaN(newPosition.Y))
    {
        System.Diagnostics.Debug.WriteLine($"❌ CRITICAL: newPosition is NaN! Position={Position}, movement={movement}");
        return;
    }

    float clampedX = MathHelper.Clamp(newPosition.X, roomBounds.Left, roomBounds.Right - spriteWidth);
    float clampedY = MathHelper.Clamp(newPosition.Y, roomBounds.Top, roomBounds.Bottom - spriteHeight);
    Vector2 candidate = new Vector2(clampedX, clampedY);

    // Add another NaN check after clamping
    if (float.IsNaN(candidate.X) || float.IsNaN(candidate.Y))
    {
        System.Diagnostics.Debug.WriteLine($"❌ CRITICAL: candidate position is NaN after clamping!");
        return;
    }

    // Defensive: ensure obstacleRects is never null
    var allObstacles = (obstacleRects ?? Enumerable.Empty<Rectangle>()).ToList();
    foreach (var npc in npcs)
    {
        if (!npc.IsDefeated)
            allObstacles.Add(npc.Bounds);
    }

    // Use wall sliding for movement
    TrySetPositionWithWallSliding(candidate, allObstacles);
}

        // Add this method to your Player class
        public bool IsNearTile(int column, int row, float tileWidth, float tileHeight)
        {
            // Calculate the center of the target tile in world coordinates.
            Vector2 tileCenter = new Vector2(
                column * tileWidth + tileWidth / 2,
                row * tileHeight + tileHeight / 2
            );

            // Use the player's bounds for a more accurate center position.
            Vector2 playerCenter = this.Bounds.Center.ToVector2();

            // Define the maximum distance for interaction. Let's use the tile's width as a radius.
            float interactionRadius = tileWidth;

            // Check if the distance between the player and the tile is within the interaction radius.
            return Vector2.Distance(playerCenter, tileCenter) <= interactionRadius;
        }

        public void SetFacingRight(bool facingRight)
        {
            MovementComponent.FacingRight = facingRight;
        }

        protected override Vector2 GetAttackDirection()
        {
            // Use the MovementComponent's FacingDirection property
            return MovementComponent.FacingDirection.ToVector();
        }

        

        // Add these fields to store obstacle information
        private IEnumerable<Rectangle> _currentObstacles;
        private IEnumerable<NPC> _currentNpcs;

        /// <summary>
        /// Updates the obstacle references for knockback collision detection.
        /// Call this from the game scene when obstacles change.
        /// </summary>
        public void UpdateObstacles(IEnumerable<Rectangle> obstacleRects, IEnumerable<NPC> npcs)
        {
            _currentObstacles = obstacleRects;
            _currentNpcs = npcs;
        }

        public override IEnumerable<Rectangle> GetObstacleRectangles()
        {
            var obstacles = new List<Rectangle>();

            // Add static obstacles
            if (_currentObstacles != null)
            {
                obstacles.AddRange(_currentObstacles);
            }

            // Add NPC bounds (except defeated ones)
            if (_currentNpcs != null)
            {
                foreach (var npc in _currentNpcs)
                {
                    if (!npc.IsDefeated)
                    {
                        obstacles.Add(npc.Bounds);
                    }
                }
            }

            return obstacles;
        }

        /// <summary>
        /// Enhanced position setting with wall sliding support
        /// </summary>
        private bool TrySetPositionWithWallSliding(Vector2 candidate, IEnumerable<Rectangle> obstacles)
        {
            // Add NaN check at the start
            if (float.IsNaN(candidate.X) || float.IsNaN(candidate.Y))
            {
                System.Diagnostics.Debug.WriteLine($"❌ CRITICAL: TrySetPositionWithWallSliding called with NaN candidate: {candidate}");
                return false;
            }

            Vector2 currentPos = Position;
            Vector2 movement = candidate - currentPos;

            // If no movement, return true (no collision)
            if (movement.LengthSquared() < 0.001f)
                return true;

            // Try full movement first
            if (!IsPositionBlocked(candidate, obstacles))
            {
                this.SetPosition(candidate);
                return true;
            }

            // If full movement is blocked, try sliding along walls

            // Try horizontal movement only (slide along vertical walls)
            Vector2 horizontalTarget = new Vector2(candidate.X, currentPos.Y);
            if (!IsPositionBlocked(horizontalTarget, obstacles))
            {
                this.SetPosition(horizontalTarget);
                return true;
            }

            // Try vertical movement only (slide along horizontal walls)
            Vector2 verticalTarget = new Vector2(currentPos.X, candidate.Y);
            if (!IsPositionBlocked(verticalTarget, obstacles))
            {
                this.SetPosition(verticalTarget);
                return true;
            }

            // If both individual axes are blocked, stay at current position
            return false;
        }

        /// <summary>
        /// Check if a position would cause collision with walls or obstacles
        /// </summary>
        private bool IsPositionBlocked(Vector2 position, IEnumerable<Rectangle> obstacles)
        {
            // Check if candidate position would collide with any obstacle
            Rectangle candidateBounds = new Rectangle(
                (int)position.X + 8,
                (int)position.Y + 16,
                (int)Sprite.Width / 2,
                (int)Sprite.Height / 2
            );

            foreach (var obstacle in obstacles)
            {
                if (candidateBounds.Intersects(obstacle))
                    return true;
            }

            // Check against tilemap walls
            if (_collisionComponent.Tilemap != null && TilesetManager.Instance.WallTileset != null)
            {
                var tilemap = _collisionComponent.Tilemap;
                int leftTile = candidateBounds.Left / (int)tilemap.TileWidth;
                int rightTile = (candidateBounds.Right - 1) / (int)tilemap.TileWidth;
                int topTile = candidateBounds.Top / (int)tilemap.TileHeight;
                int bottomTile = (candidateBounds.Bottom - 1) / (int)tilemap.TileHeight;

                for (int col = leftTile; col <= rightTile; col++)
                {
                    for (int row = topTile; row <= bottomTile; row++)
                    {
                        if (col >= 0 && col < tilemap.Columns && row >= 0 && row < tilemap.Rows)
                        {
                            if (tilemap.GetTileset(col, row) == TilesetManager.Instance.WallTileset &&
                                AutotileMapper.IsWallTile(tilemap.GetTileId(col, row)))
                            {
                                return true; // Collision with a wall
                            }
                        }
                    }
                }
            }

            return false;
        }
    }
}