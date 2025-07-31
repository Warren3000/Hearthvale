using Hearthvale.GameCode.Entities.Characters;
using Hearthvale.GameCode.Entities.NPCs;
using Hearthvale.GameCode.Managers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary.Graphics;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Audio;

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
        public Weapon EquippedWeapon { get; private set; }
        private Vector2 _lastMovementDirection = Vector2.UnitX;
        // Add this field to the Player class to fix CS0103
        private readonly float _movementSpeed;
        public Vector2 LastMovementDirection => _lastMovementDirection;
        public int CurrentHealth => _currentHealth;
        public int MaxHealth => _maxHealth;

        public override AnimatedSprite Sprite => _sprite;
        public override Vector2 Position => _position;
        public override Rectangle Bounds => new Rectangle(
            (int)Position.X + 8,
            (int)Position.Y + 16,
            (int)Sprite.Width / 2,
            (int)Sprite.Height / 2
        );

        public Player(TextureAtlas atlas, Vector2 position, CombatManager combatManager, CombatEffectsManager combatEffectsManager, ScoreManager scoreManager, SoundEffect hitSound, SoundEffect defeatSound, SoundEffect playerAttackSound, float movementSpeed)
        {
            _atlas = atlas;
            _sprite = new AnimatedSprite(atlas.GetAnimation("Mage_Idle"));
            _position = position;
            _movementSpeed = movementSpeed;
            _facingRight = true;
            _lastMovementDirection = Vector2.UnitX;

            _movementController = new PlayerMovementController(this);
            _combatController = new PlayerCombatController(this, combatManager, combatEffectsManager, scoreManager, hitSound, defeatSound, playerAttackSound);

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

        public override void TakeDamage(int amount, Vector2? knockback = null)
        {
            if (IsDefeated) return;

            _currentHealth -= amount;
            if (_currentHealth < 0) _currentHealth = 0;

            if (knockback.HasValue)
                _movementController.SetVelocity(knockback.Value);
            Flash();
        }

        public override void Flash()
        {
            _animationController.Flash();
        }
        public void StartAttack()
        {
            IsAttacking = true;
        }
        public override void SetPosition(Vector2 pos)
        {
            _position = pos;
            _sprite.Position = pos;
        }

        public void EquipWeapon(Weapon weapon)
        {
            if (weapon == null) return; // Prevent unequipping
            EquippedWeapon = weapon;
        }

        public void Update(GameTime gameTime, IEnumerable<NPC> npcs)
        {
            _movementController.Update(gameTime, npcs); // Handles knockback
            _animationController.UpdateFlash((float)gameTime.ElapsedGameTime.TotalSeconds);
            _combatController.Update(gameTime, npcs); // Now handles hit detection
            _animationController.UpdateAnimation(_movementController.IsMoving());

            _sprite.Position = _position;
            _sprite.Effects = _facingRight ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
        }

        public void ClampToBounds(Rectangle bounds)
        {
            float clampedX = MathHelper.Clamp(Position.X, bounds.Left, bounds.Right - Sprite.Width);
            float clampedY = MathHelper.Clamp(Position.Y, bounds.Top, bounds.Bottom - Sprite.Height);
            SetPosition(new Vector2(clampedX, clampedY));
        }

        public void Move(Vector2 movement, Rectangle roomBounds, float spriteWidth, float spriteHeight, IEnumerable<NPC> npcs)
        {
            // Only allow player input to move if not being knocked back
            if (_movementController.IsKnockedBack) return;

            if (movement != Vector2.Zero)
            {
                _lastMovementDirection = Vector2.Normalize(movement);
                _facingRight = _lastMovementDirection.X >= 0;
            }

            Vector2 newPosition = Position + movement;
            float clampedX = MathHelper.Clamp(
                newPosition.X,
                roomBounds.Left,
                roomBounds.Right - spriteWidth
            );
            float clampedY = MathHelper.Clamp(
                newPosition.Y,
                roomBounds.Top,
                roomBounds.Bottom - spriteHeight
            );
            Vector2 candidate = new Vector2(clampedX, clampedY);

            // Check collision with NPCs
            Rectangle candidateBounds = new Rectangle(
                (int)candidate.X + 8,
                (int)candidate.Y + 16,
                (int)Sprite.Width / 2,
                (int)Sprite.Height / 2
            );
            foreach (var npc in npcs)
            {
                if (candidateBounds.Intersects(npc.Bounds))
                    return; // Block movement if collision
            }

            SetPosition(candidate);
        }
        public Rectangle GetAttackArea()
        {
            if (EquippedWeapon == null) return Rectangle.Empty;

            float attackReach = EquippedWeapon.Length; // Use the weapon's actual length
            int attackWidth, attackHeight;
            Vector2 attackCenter = Position + new Vector2(Sprite.Width / 2, Sprite.Height / 2);
            Vector2 offset = Vector2.Zero;

            // Check if the primary movement is horizontal or vertical
            if (System.Math.Abs(LastMovementDirection.X) > System.Math.Abs(LastMovementDirection.Y))
            {
                // Horizontal attack
                attackWidth = 32;
                attackHeight = (int)Sprite.Height;
                offset.X = (LastMovementDirection.X > 0 ? 1 : -1) * attackReach;
            }
            else
            {
                // Vertical attack
                attackWidth = (int)Sprite.Width;
                attackHeight = 32;
                offset.Y = (LastMovementDirection.Y > 0 ? 1 : -1) * attackReach;
            }

            int x = (int)(attackCenter.X + offset.X - attackWidth / 2);
            int y = (int)(attackCenter.Y + offset.Y - attackHeight / 2);

            return new Rectangle(x, y, attackWidth, attackHeight);
        }

        public void SetLastMovementDirection(Vector2 dir)
        {
            _lastMovementDirection = dir;
        }

        public void SetFacingRight(bool facingRight)
        {
            _facingRight = facingRight;
        }

        public PlayerCombatController CombatController => _combatController;
    }
}