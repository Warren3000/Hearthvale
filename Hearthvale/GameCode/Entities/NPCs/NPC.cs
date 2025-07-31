using Hearthvale.GameCode.Entities.Characters;
using Hearthvale.GameCode.Entities.Players;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Hearthvale.GameCode.Entities.NPCs;

public class NPC : Character
{
    private readonly NpcAnimationController _animationController;
    private readonly NpcMovementController _movementController;
    private readonly NpcHealthController _healthController;

    public string DialogText { get; set; } = "Hello, adventurer!";
    public int AttackPower { get; set; } = 1;
    private float _attackCooldown = 1.5f;
    // private float _attackTimer = 0f; // REMOVED: This is now managed by the health controller's stun timer
    public Weapon EquippedWeapon { get; private set; }

    public bool CanAttack => !_healthController.IsOnCooldown; // Use the health controller's state
    public void ResetAttackTimer() => _healthController.StartAttackCooldown(_attackCooldown);
    public bool IsReadyToRemove => _healthController.IsReadyToRemove;
    public override bool IsDefeated => _healthController.IsDefeated;
    public override int Health => _healthController.Health;
    public override AnimatedSprite Sprite => _animationController.Sprite;
    public override Vector2 Position => _movementController.Position;
    public override Rectangle Bounds => new Rectangle(
        (int)Position.X + 8,
        (int)Position.Y + 16,
        (int)Sprite.Width / 2,
        (int)Sprite.Height / 2
    );

    public bool IsAttacking { get; private set; }
    private float _attackAnimTimer = 0f;
    private const float AttackAnimDuration = 0.25f; // WindUp (0.15) + Slash (0.1)

    public string Name { get; private set; }

    public NPC(string name, Dictionary<string, Animation> animations, Vector2 position, Rectangle bounds, SoundEffect defeatSound, int maxHealth)
    {
        Name = name; // Assign the name
        var sprite = new AnimatedSprite(animations["Idle"]);
        _animationController = new NpcAnimationController(sprite, animations);
        _movementController = new NpcMovementController(position, 60.0f, bounds);
        _healthController = new NpcHealthController(maxHealth, defeatSound);

        _sprite = sprite;
        _position = position;
        _maxHealth = maxHealth;
        _currentHealth = maxHealth;

        _animationController.SetAnimation("Idle");
        _movementController.SetIdle();
    }
    public override void Draw(SpriteBatch spriteBatch)
    {
        Color originalColor = _sprite.Color;
        Color weaponOriginalColor = EquippedWeapon != null ? EquippedWeapon.Sprite.Color : Color.White;
        float alpha = 1f;
        if (IsDefeated && IsReadyToRemove == false)
        {
            float progress = _healthController.DefeatTimerProgress;
            alpha = MathHelper.Clamp(progress, 0f, 1f);
            _sprite.Color = Color.White * alpha;
            if (EquippedWeapon != null)
                EquippedWeapon.Sprite.Color = Color.White * alpha;
        }
        else
        {
            _sprite.Color = Color.White;
            if (EquippedWeapon != null)
                EquippedWeapon.Sprite.Color = Color.White;
        }
        bool drawWeaponBehind = _movementController.GetVelocity().Y < 0;
        if (drawWeaponBehind)
        {
            EquippedWeapon?.Draw(spriteBatch, Position);
            _sprite.Draw(spriteBatch, Position);
        }
        else
        {
            _sprite.Draw(spriteBatch, Position);
            EquippedWeapon?.Draw(spriteBatch, Position);
        }
        _sprite.Color = originalColor;
        if (EquippedWeapon != null)
            EquippedWeapon.Sprite.Color = weaponOriginalColor;
    }

    public void EquipWeapon(Weapon weapon)
    {
        EquippedWeapon = weapon;
    }

    public override void TakeDamage(int amount, Vector2? knockback = null)
    {
        Debug.WriteLine($"--[NPC.{Name}] TakeDamage called for {amount} damage. Checking CanTakeDamage...");
        if (_healthController.CanTakeDamage)
        {
            Debug.WriteLine($"----[NPC.{Name}] CanTakeDamage is TRUE. Applying damage.");
            _healthController.TakeDamage(amount);
            if (knockback.HasValue)
                _movementController.SetVelocity(knockback.Value);
            Flash();
        }
        else
        {
            Debug.WriteLine($"----[NPC.{Name}] CanTakeDamage is FALSE. Damage blocked.");
        }
    }

    public override void Flash()
    {
        _animationController.Flash();
    }

    public override void SetPosition(Vector2 position)
    {
        _movementController.Position = position;
        _sprite.Position = position;
    }

    public void Update(GameTime gameTime, IEnumerable<NPC> allNpcs, Character player)
    {
        float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // Step 1: Update health and animation states first. This is the highest priority.
        UpdateHealthAndAnimation(elapsed);

        // Step 2: If defeated, do nothing else.
        if (IsDefeated)
        {
            return;
        }

        // Step 3: If stunned by a hit, only process knockback. Do not process AI or attacks.
        if (_healthController.IsStunned)
        {
            _movementController.Update(elapsed, _ => false); // Update knockback but not AI
            _sprite.Position = Position;
            return;
        }
        
        // Step 4: Handle the attack animation timer if currently attacking.
        if (IsAttacking)
        {
            _attackAnimTimer -= elapsed;
            if (_attackAnimTimer <= 0)
            {
                IsAttacking = false;
            }
        }

        // Step 5: Process regular movement and AI.
        UpdateMovement(elapsed, allNpcs, player);
        
        // Step 6: Update weapon position and rotation.
        UpdateWeapon(gameTime, player);

        // Step 7: Decide whether to initiate a new attack.
        // This can only happen if not already attacking and not on any cooldown.
        float attackRange = EquippedWeapon?.Length ?? 32f; // Use a default range if no weapon
        if (CanAttack && !IsAttacking && Vector2.Distance(Position, player.Position) < attackRange)
        {
            StartAttack();
        }

        _sprite.Position = Position; // Ensure sprite position matches movement
    }

    public void StartAttack()
    {
        IsAttacking = true;
        _attackAnimTimer = AttackAnimDuration;
        EquippedWeapon?.StartSwing(_facingRight);
        ResetAttackTimer(); // Start global attack cooldown
    }

    public Rectangle GetAttackArea()
    {
        if (EquippedWeapon == null) return Rectangle.Empty;

        float attackReach = EquippedWeapon.Length; // Use the weapon's actual length
        Vector2 direction = Vector2.Zero;
        float angle = EquippedWeapon.Rotation;
        direction.X = (float)System.Math.Cos(angle);
        direction.Y = (float)System.Math.Sin(angle);

        int attackWidth, attackHeight;
        Vector2 attackCenter = Position + new Vector2(Sprite.Width / 2, Sprite.Height / 2);
        Vector2 offset = Vector2.Zero;

        if (System.Math.Abs(direction.X) > System.Math.Abs(direction.Y))
        {
            attackWidth = 32;
            attackHeight = (int)Sprite.Height;
            offset.X = (direction.X > 0 ? 1 : -1) * attackReach;
        }
        else
        {
            attackWidth = (int)Sprite.Width;
            attackHeight = 32;
            offset.Y = (direction.Y > 0 ? 1 : -1) * attackReach;
        }

        int x = (int)(attackCenter.X + offset.X - attackWidth / 2);
        int y = (int)(attackCenter.Y + offset.Y - attackHeight / 2);

        return new Rectangle(x, y, attackWidth, attackHeight);
    }

    private void UpdateWeapon(GameTime gameTime, Character player)
    {
        if (EquippedWeapon != null)
        {
            Vector2 directionToPlayer = player.Position - Position;
            if (directionToPlayer != Vector2.Zero)
            {
                directionToPlayer.Normalize();
            }
            else
            {
                directionToPlayer = Vector2.UnitX; // Default if positions are identical
            }

            // Set facing direction based on player's position
            if (Math.Abs(directionToPlayer.X) > System.Math.Abs(directionToPlayer.Y))
            {
                _facingRight = directionToPlayer.X > 0;
            }

            // Align weapon rotation with the direction to the player
            const float rotationOffset = MathHelper.Pi / 4f; // 45 degrees in radians
            EquippedWeapon.Rotation = (float)System.Math.Atan2(directionToPlayer.Y, directionToPlayer.X) + rotationOffset;
            EquippedWeapon.Position = Position + new Vector2(Sprite.Width / 2, Sprite.Height / 2);
            EquippedWeapon.Update(gameTime);
        }
    }

    private void UpdateHealthAndAnimation(float elapsed)
    {
        _animationController.UpdateFlash(elapsed);
        _healthController.Update(elapsed);

        // Determine the correct animation based on a clear priority
        string desiredAnimation = "Idle"; // Default animation

        if (_healthController.IsDefeated)
        {
            desiredAnimation = "Defeated";
        }
        else if (_healthController.IsStunned)
        {
            desiredAnimation = "Hit";
        }
        else if (IsAttacking)
        {
            // This could be a specific "Attack" animation if you have one
            desiredAnimation = "Walk"; // Or whatever looks best
        }
        else if (!_movementController.IsIdle)
        {
            desiredAnimation = "Walk";
        }

        _animationController.SetAnimation(desiredAnimation);
        Sprite.Update(new GameTime(new TimeSpan(), TimeSpan.FromSeconds(elapsed)));
    }

    private void UpdateMovement(float elapsed, IEnumerable<NPC> allNpcs, Character player)
    {
        _movementController.Update(elapsed, nextPos =>
        {
            Rectangle nextBounds = new Rectangle(
                (int)nextPos.X + 8,
                (int)nextPos.Y + 16,
                (int)Sprite.Width / 2,
                (int)Sprite.Height / 2
            );
            if (nextBounds.Intersects(player.Bounds))
                return true;
            foreach (var npc in allNpcs)
            {
                if (npc == this) continue;
                if (nextBounds.Intersects(npc.Bounds))
                    return true;
            }
            return false;
        });

        float spriteWidth = Sprite.Width;
        float spriteHeight = Sprite.Height;
        Rectangle bounds = _movementController.Bounds;
        float clampedX = MathHelper.Clamp(Position.X, bounds.Left, bounds.Right - spriteWidth);
        float clampedY = MathHelper.Clamp(Position.Y, bounds.Top, bounds.Bottom - spriteHeight);
        _movementController.Position = new Vector2(clampedX, clampedY);

        var velocity = _movementController.GetVelocity();
        if (velocity.X != 0)
            _facingRight = velocity.X > 0;
    }
}