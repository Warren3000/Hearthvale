using Hearthvale.GameCode.Entities.Characters;
using Hearthvale.GameCode.Entities.NPCs;
using Hearthvale.GameCode.Managers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary.Graphics;
using System.Collections.Generic;

namespace Hearthvale.GameCode.Entities.Players
{
    public class Player : Character
    {
        private readonly TextureAtlas _atlas;
        private PlayerCombatController _combatController;
        private PlayerMovementController _movementController;
        private CombatEffectsManager _effectsManager;
        private readonly NpcAnimationController _animationController;
        private readonly NpcHealthController _healthController;

        public float MovementSpeed { get; }
        public bool IsAttacking { get; set; }
        private float _attackTimer = 0f;
        private const float AttackDuration = 0.3f;
        private float _weaponOrbitRadius = 3f;
        public float WeaponOrbitRadius => _weaponOrbitRadius;
        public Weapon EquippedWeapon { get; private set; }
        private Vector2 _lastMovementDirection = Vector2.UnitX;
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

        public Player(TextureAtlas atlas, Vector2 startPosition, CombatEffectsManager effectsManager, float movementSpeed = 3.0f)
        {
            _atlas = atlas;
            _sprite = _atlas.CreateAnimatedSprite("Mage_Idle");
            _sprite.Scale = new Vector2(1f, 1f);
            _position = startPosition;
            MovementSpeed = movementSpeed;
            _maxHealth = 10;
            _currentHealth = 10;
            _effectsManager = effectsManager;
            _combatController = new PlayerCombatController(this, _effectsManager);
            _movementController = new PlayerMovementController(this);
            _animationController = new NpcAnimationController(_sprite, new Dictionary<string, Animation>());
            _healthController = new NpcHealthController(_maxHealth, null);
            IsAttacking = false;
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

        public void Update(GameTime gameTime, KeyboardState keyboard, IEnumerable<NPC> npcs)
        {
            Vector2 movement = _movementController.Update(gameTime, keyboard);
            _animationController.UpdateFlash((float)gameTime.ElapsedGameTime.TotalSeconds);
            _combatController.Update(gameTime, keyboard, movement, npcs);
            UpdateAnimation(keyboard, movement != Vector2.Zero);

            _sprite.Position = _position;
            _sprite.Effects = _facingRight ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
        }

        public void ClampToBounds(Rectangle bounds)
        {
            float clampedX = MathHelper.Clamp(Position.X, bounds.Left, bounds.Right - Sprite.Width);
            float clampedY = MathHelper.Clamp(Position.Y, bounds.Top, bounds.Bottom - Sprite.Height);
            SetPosition(new Vector2(clampedX, clampedY));
        }

        public void UpdateAnimation(KeyboardState keyboard, bool moving)
        {
            string desiredAnimation = moving ? "Mage_Walk" : "Mage_Idle";
            if (_currentAnimationName != desiredAnimation)
            {
                _sprite.Animation = _atlas.GetAnimation(desiredAnimation);
                _currentAnimationName = desiredAnimation;
            }
            // Weapon animation should be handled separately, e.g. in the weapon class or controller
        }
        public void Move(Vector2 movement, Rectangle roomBounds, float spriteWidth, float spriteHeight, IEnumerable<NPC> npcs)
        {
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
            int width = 32;
            int height = (int)_sprite.Height;
            int offsetX = _facingRight ? (int)_sprite.Width : -width;
            int x = (int)_position.X + offsetX;
            int y = (int)_position.Y;
            return new Rectangle(x, y, width, height);
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