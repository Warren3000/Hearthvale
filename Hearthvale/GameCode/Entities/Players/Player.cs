using Hearthvale.GameCode.Entities.Characters;
using Hearthvale.GameCode.Entities.NPCs;
using Hearthvale.GameCode.Managers;
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
        public int CurrentHealth => _currentHealth;

        public override AnimatedSprite Sprite => _sprite;
        public override Vector2 Position => _position;
        public override Rectangle Bounds => new Rectangle(
            (int)Position.X + 8,
            (int)Position.Y + 16,
            (int)Sprite.Width / 2,
            (int)Sprite.Height / 2
        );

        public Player(TextureAtlas atlas, Vector2 position, SoundEffect hitSound, SoundEffect defeatSound, SoundEffect playerAttackSound, float movementSpeed)
        {
            _atlas = atlas;
            _sprite = new AnimatedSprite(atlas.GetAnimation("Mage_Idle"));
            _position = position;
            _movementSpeed = movementSpeed;
            _facingRight = true;
            _lastMovementDirection = Vector2.UnitX;

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
            UpdateKnockback(gameTime); // Handles knockback and wall bounce
            _animationController.UpdateFlash((float)gameTime.ElapsedGameTime.TotalSeconds);
            _combatController.Update(gameTime, npcs);
            _animationController.UpdateAnimation(_movementController.IsMoving());

            _sprite.Position = _position;
            _sprite.Effects = _facingRight ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
        }

        public void Move(
            Vector2 movement,
            Rectangle roomBounds,
            float spriteWidth,
            float spriteHeight,
            IEnumerable<NPC> npcs,
            Tilemap tilemap,
            int wallTileId,
            IEnumerable<Rectangle> obstacleRects)
        {
            if (_movementController.IsKnockedBack) return;

            if (movement != Vector2.Zero)
            {
                _lastMovementDirection = Vector2.Normalize(movement);
                _facingRight = _lastMovementDirection.X >= 0;
            }

            Vector2 newPosition = Position + movement;
            float clampedX = MathHelper.Clamp(newPosition.X, roomBounds.Left, roomBounds.Right - spriteWidth);
            float clampedY = MathHelper.Clamp(newPosition.Y, roomBounds.Top, roomBounds.Bottom - spriteHeight);
            Vector2 candidate = new Vector2(clampedX, clampedY);

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
    }
    }