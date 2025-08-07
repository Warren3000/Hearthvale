using Hearthvale.GameCode.Entities.Characters;
using Hearthvale.GameCode.Entities.NPCs;
using Hearthvale.GameCode.Managers;
using Hearthvale.GameCode.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
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

        public float MovementSpeed { get; }
        public bool IsAttacking { get; set; }
        private float _attackTimer = 0f;
        private const float AttackDuration = 0.3f;  
        private float _weaponOrbitRadius = 3f;
        public float WeaponOrbitRadius => _weaponOrbitRadius;

        private Vector2 _lastMovementDirection = Vector2.UnitX;
        private readonly float _movementSpeed;
        public Vector2 LastMovementDirection => _lastMovementDirection;
        public PlayerCombatController CombatController => _combatController;
        public override int Health => _currentHealth;
        public int CurrentHealth => _currentHealth;

        public override AnimatedSprite Sprite => _sprite;
        public override Vector2 Position => _position;
        public override Rectangle Bounds => new Rectangle(
            (int)Position.X + 8,
            (int)Position.Y + 16,
            (int)Sprite.Width / 2,
            (int)Sprite.Height / 2
        );
        private Tileset _wallTileset;
        private Tileset _floorTileset;

        public Player(TextureAtlas atlas, Vector2 position, SoundEffect hitSound, SoundEffect defeatSound, SoundEffect playerAttackSound, float movementSpeed)
        {
            _atlas = atlas;
            _sprite = new AnimatedSprite(atlas.GetAnimation("Mage_Idle"));
            _position = position; // Make sure this is set
            _movementSpeed = movementSpeed;
            _facingRight = true;
            _lastMovementDirection = Vector2.UnitX;

            // Add debug output to confirm position is set
            System.Diagnostics.Debug.WriteLine($"Player created at position: {_position}");

            _movementController = new PlayerMovementController(this);
            _combatController = new PlayerCombatController(this, hitSound, defeatSound, playerAttackSound);

            var animations = new Dictionary<string, Animation>
            {
                { "Mage_Idle", atlas.GetAnimation("Mage_Idle") },
                { "Mage_Walk", atlas.GetAnimation("Mage_Walk") }
            };
            _animationController = new PlayerAnimationController(this, _sprite, animations);

            // Initialize health
            _maxHealth = 100;
            _currentHealth = _maxHealth;

            // Set sprite position immediately
            _sprite.Position = _position;
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
            if (float.IsNaN(_position.X) || float.IsNaN(_position.Y))
            {
                System.Diagnostics.Debug.WriteLine($"❌ CRITICAL: Player position is NaN at start of Update! Resetting to spawn position.");
                _position = new Vector2(896, 80); // Use the known good spawn position
            }

            UpdateKnockback(gameTime); // Handles knockback and wall bounce
            
            // Check after knockback update
            if (float.IsNaN(_position.X) || float.IsNaN(_position.Y))
            {
                System.Diagnostics.Debug.WriteLine($"❌ CRITICAL: Player position became NaN after UpdateKnockback!");
                _position = new Vector2(896, 80); // Reset again
                _knockbackVelocity = Vector2.Zero;
                _knockbackTimer = 0;
            }

            _animationController.UpdateFlash((float)gameTime.ElapsedGameTime.TotalSeconds);
            _combatController.Update(gameTime, npcs);
            _animationController.UpdateAnimation(_movementController.IsMoving());

            _sprite.Position = _position;
            _sprite.Effects = _facingRight ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            
            // Final validation
            if (float.IsNaN(_position.X) || float.IsNaN(_position.Y))
            {
                System.Diagnostics.Debug.WriteLine($"❌ CRITICAL: Player position is NaN at end of Update!");
                _position = new Vector2(896, 80); // Final reset
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
            if (_movementController.IsKnockedBack) return;

            if (movement != Vector2.Zero)
            {
                _lastMovementDirection = Vector2.Normalize(movement);
                _facingRight = _lastMovementDirection.X >= 0;
            }

            Vector2 newPosition = Position + movement;

            // Add NaN check and prevention
            if (float.IsNaN(newPosition.X) || float.IsNaN(newPosition.Y))
            {
                System.Diagnostics.Debug.WriteLine($"❌ CRITICAL: newPosition is NaN! Position={Position}, movement={movement}");
                return; // Don't move if calculation results in NaN
            }

            float clampedX = MathHelper.Clamp(newPosition.X, roomBounds.Left, roomBounds.Right - spriteWidth);
            float clampedY = MathHelper.Clamp(newPosition.Y, roomBounds.Top, roomBounds.Bottom - spriteHeight);
            Vector2 candidate = new Vector2(clampedX, clampedY);

            // Add another NaN check after clamping
            if (float.IsNaN(candidate.X) || float.IsNaN(candidate.Y))
            {
                System.Diagnostics.Debug.WriteLine($"❌ CRITICAL: candidate position is NaN after clamping! roomBounds={roomBounds}, spriteWidth={spriteWidth}, spriteHeight={spriteHeight}");
                return; // Don't move if clamping results in NaN
            }

            // Defensive: ensure obstacleRects is never null
            var allObstacles = (obstacleRects ?? Enumerable.Empty<Rectangle>()).ToList();
            foreach (var npc in npcs)
            {
                if (!npc.IsDefeated)
                    allObstacles.Add(npc.Bounds);
            }

            // Prevent movement into any obstacle
            if (!TrySetPosition(candidate, allObstacles))
                return;
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
        public void SetLastMovementDirection(Vector2 dir)
        {
            _lastMovementDirection = dir;
        }

        public void SetFacingRight(bool facingRight)
        {
            _facingRight = facingRight;
        }


        protected override Vector2 GetAttackDirection()
        {
            return LastMovementDirection;
        }

        protected override bool ShouldDrawWeaponBehind()
        {
            return LastMovementDirection.Y < 0;
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

        protected override IEnumerable<Rectangle> GetObstacleRectangles()
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
        private bool TrySetPosition(Vector2 candidate, IEnumerable<Rectangle> obstacles)
        {
            // Add NaN check at the start
            if (float.IsNaN(candidate.X) || float.IsNaN(candidate.Y))
            {
                System.Diagnostics.Debug.WriteLine($"❌ CRITICAL: TrySetPosition called with NaN candidate: {candidate}");
                return false;
            }

            // Check if candidate position would collide with any obstacle
            Rectangle candidateBounds = new Rectangle(
                (int)candidate.X + 8,
                (int)candidate.Y + 16,
                (int)Sprite.Width / 2,
                (int)Sprite.Height / 2
            );

            foreach (var obstacle in obstacles)
            {
                if (candidateBounds.Intersects(obstacle))
                    return false;
            }

            // Check against tilemap walls
            if (this.Tilemap != null && TilesetManager.Instance.WallTileset != null)
            {
                int leftTile = candidateBounds.Left / (int)this.Tilemap.TileWidth;
                int rightTile = (candidateBounds.Right - 1) / (int)this.Tilemap.TileWidth;
                int topTile = candidateBounds.Top / (int)this.Tilemap.TileHeight;
                int bottomTile = (candidateBounds.Bottom - 1) / (int)this.Tilemap.TileHeight;

                for (int col = leftTile; col <= rightTile; col++)
                {
                    for (int row = topTile; row <= bottomTile; row++)
                    {
                        if (this.Tilemap.GetTileset(col, row) == TilesetManager.Instance.WallTileset && AutotileMapper.IsWallTile(Tilemap.GetTileId(col, row)))
                        {
                            return false; // Collision with a wall
                        }
                    }
                }
            }

            // If no collision, set the position
            _position = candidate;
            
            // Add debug check after setting position
            if (float.IsNaN(_position.X) || float.IsNaN(_position.Y))
            {
                System.Diagnostics.Debug.WriteLine($"❌ CRITICAL: _position became NaN after setting to {candidate}!");
            }
            
            return true;
        }
    }
    }