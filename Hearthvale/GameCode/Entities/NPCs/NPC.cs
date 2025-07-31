using Hearthvale.GameCode.Entities.Characters;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Graphics;
using System;
using System.Collections.Generic;

namespace Hearthvale.GameCode.Entities.NPCs;

public class NPC : Character
{
    private readonly NpcAnimationController _animationController;
    private readonly NpcMovementController _movementController;
    private readonly NpcHealthController _healthController;

    public string DialogText { get; set; } = "Hello, adventurer!";
    public int AttackPower { get; set; } = 1;
    private float _attackCooldown = 1.5f;
    private float _attackTimer = 0f;
    public Weapon EquippedWeapon { get; private set; }

    public bool CanAttack => _attackTimer <= 0f;
    public void ResetAttackTimer() => _attackTimer = _attackCooldown;
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

    public NPC(Dictionary<string, Animation> animations, Vector2 position, Rectangle bounds, SoundEffect defeatSound, int maxHealth)
    {
        var sprite = new AnimatedSprite(animations["Idle"]);
        _animationController = new NpcAnimationController(sprite, animations);
        _movementController = new NpcMovementController(position, 60.0f, bounds);
        _healthController = new NpcHealthController(maxHealth, defeatSound);

        _sprite = sprite;
        _position = position;
        _maxHealth = 10;
        _currentHealth = 10;

        _animationController.SetAnimation("Idle");
        _movementController.SetIdle();
    }
    public override void Draw(SpriteBatch spriteBatch)
    {
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
    }

    public void EquipWeapon(Weapon weapon)
    {
        EquippedWeapon = weapon;
    }

    public override void TakeDamage(int amount, Vector2? knockback = null)
    {
        if (_healthController.CanTakeDamage)
        {
            _healthController.TakeDamage(amount);
            if (knockback.HasValue)
                _movementController.SetVelocity(knockback.Value);
            Flash();
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
        UpdateHealth(elapsed);
        UpdateMovement(elapsed, allNpcs, player);
        UpdateAnimation(elapsed);
        UpdateWeapon(gameTime, player);

        _sprite.Position = Position; // Ensure sprite position matches movement
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

            EquippedWeapon.Rotation = (float)Math.Atan2(directionToPlayer.Y, directionToPlayer.X) + MathHelper.PiOver4;
            EquippedWeapon.Position = Position + new Vector2(Sprite.Width / 2, Sprite.Height / 2);
            EquippedWeapon.Update(gameTime);
        }
    }

    private void UpdateHealth(float elapsed)
    {
        _animationController.UpdateFlash(elapsed);
        if (_healthController.Update(elapsed))
        {
            _animationController.SetAnimation(_healthController.IsDefeated ? "Defeated" : "Hit");
            Sprite.Update(new GameTime());
        }
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

        if (_attackTimer > 0)
            _attackTimer -= elapsed;
    }

    private void UpdateAnimation(float elapsed)
    {
        if (_movementController.IsIdle)
            _animationController.SetAnimation("Idle");
        else
            _animationController.SetAnimation("Walk");

        Sprite.Update(new GameTime());
    }
}