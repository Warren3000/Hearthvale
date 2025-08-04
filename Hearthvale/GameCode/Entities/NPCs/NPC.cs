using Hearthvale.GameCode.Entities.Characters;
using Hearthvale.GameCode.Entities.Interfaces;
using Hearthvale.GameCode.Entities.Players;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Hearthvale.GameCode.Entities.NPCs;

public class NPC : Character, ICombatNpc, IDialog
{
    private readonly NpcAnimationController _animationController;
    private readonly NpcMovementController _movementController;
    private readonly NpcHealthController _healthController;

    
    public int AttackPower { get; set; } = 1;
    private float _attackCooldown = 1.5f;
    // private float _attackTimer = 0f; // REMOVED: This is now managed by the health controller's stun timer

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

    public NPC(string name, Dictionary<string, Animation> animations, Vector2 position, Rectangle bounds, SoundEffect defeatSound, int maxHealth, Tilemap tilemap, int wallTileId)
    {
        Name = name; // Assign the name
        var sprite = new AnimatedSprite(animations["Idle"]);
        _animationController = new NpcAnimationController(sprite, animations);
        _movementController = new NpcMovementController(position, 60.0f, bounds, tilemap, wallTileId, (int)sprite.Width, (int)sprite.Height);
        _healthController = new NpcHealthController(maxHealth, defeatSound);

        _sprite = sprite;
        _position = position;
        _maxHealth = maxHealth;
        _currentHealth = maxHealth;

        _animationController.SetAnimation("Idle");
        _movementController.SetIdle();
    }

    public override void EquipWeapon(Weapon weapon)
    {
        base.EquipWeapon(weapon);
    }

    public override bool TakeDamage(int amount, Vector2? knockback = null)
    {
        if (_healthController.CanTakeDamage)
        {
            bool justDefeated = _healthController.TakeDamage(amount);
            _currentHealth = _healthController.Health;
            if (knockback.HasValue)
                _movementController.SetVelocity(knockback.Value);
            Flash();
            return justDefeated;
        }
        else
        {
            Debug.WriteLine($"----[NPC.{Name}] CanTakeDamage is FALSE. Damage blocked.");
        }
        return false;
    }

    public override void Heal(int amount)
    {
        _healthController.Heal(amount);
        _currentHealth = _healthController.Health;
    }

    public void Update(GameTime gameTime, IEnumerable<NPC> allNpcs, Character player, IEnumerable<Rectangle> rectangles)
    {
        float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;

        UpdateHealthAndAnimation(elapsed);

        if (IsDefeated)
            return;

        if (_healthController.IsStunned)
        {
            UpdateKnockback(gameTime); // Handles knockback and wall bounce
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
        UpdateMovement(elapsed, allNpcs, player, rectangles);
        
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
                directionToPlayer = Vector2.UnitX;
            }

            // Set facing direction based on player's position
            if (Math.Abs(directionToPlayer.X) > Math.Abs(directionToPlayer.Y))
            {
                _facingRight = directionToPlayer.X > 0;
            }

            // The base rotation should simply match the direction.
            // The visual offset is handled in the Weapon's Draw method.
            EquippedWeapon.Rotation = (float)Math.Atan2(directionToPlayer.Y, directionToPlayer.X);

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

    public void UpdateMovement(float elapsed, IEnumerable<NPC> allNpcs, Character player, IEnumerable<Rectangle> obstacleRects)
    {
        if (_healthController.IsStunned)
            return;

        Vector2 velocity = _movementController.GetVelocity();
        if (velocity == Vector2.Zero)
            return;

        Vector2 candidatePosition = Position + velocity * elapsed;

        // Add player and other NPCs to obstacles
        var allObstacles = obstacleRects.ToList();
        if (!player.IsDefeated)
            allObstacles.Add(player.Bounds);

        foreach (var npc in allNpcs)
        {
            if (npc != this && !npc.IsDefeated)
                allObstacles.Add(npc.Bounds);
        }

        // Prevent movement into any obstacle (walls, dungeon elements, other NPCs, player)
        if (!TrySetPosition(candidatePosition, allObstacles))
            return;

        SetPosition(candidatePosition);
    }

    public override void Flash()
    {
        _animationController.Flash();
    }

    public Vector2 GetVelocity()
    {
        return _movementController.GetVelocity();
    }

    protected override Vector2 GetAttackDirection()
    {
        float angle = EquippedWeapon?.Rotation ?? 0f;
        return new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
    }

    protected override bool ShouldDrawWeaponBehind()
    {
        return GetVelocity().Y < 0;
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        // Calculate the true center of the NPC for weapon drawing
        Vector2 npcCenter = Position + new Vector2(Sprite.Width / 2f, Sprite.Height / 2f);

        // Draw weapon behind if needed
        if (EquippedWeapon != null && ShouldDrawWeaponBehind())
            EquippedWeapon.Draw(spriteBatch, npcCenter);

        // Draw NPC sprite at its top-left position
        Sprite.Draw(spriteBatch, Position);

        // Draw weapon in front if needed
        if (EquippedWeapon != null && !ShouldDrawWeaponBehind())
            EquippedWeapon.Draw(spriteBatch, npcCenter);
    }

    public override Rectangle GetAttackArea()
    {
        return base.GetAttackArea();
    }
}