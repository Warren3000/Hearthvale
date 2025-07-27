using Hearthvale.Content.NPC;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Graphics;
using System.Collections.Generic;

namespace Hearthvale.GameCode.Entities;
public class NPC
{
    // --- Controllers ---
    private readonly NpcAnimationController _animationController;
    private readonly NpcMovementController _movementController;
    private readonly NpcHealthController _healthController;

    // --- State & Data ---
    public string DialogText { get; set; } = "Hello, adventurer!";
    public int AttackPower { get; set; } = 1;
    private float _attackCooldown = 1.5f;
    private float _attackTimer = 0f;
    public bool CanAttack => _attackTimer <= 0f;
    public void ResetAttackTimer() => _attackTimer = _attackCooldown;
    public bool IsReadyToRemove => _healthController.IsReadyToRemove;
    public bool IsDefeated => _healthController.IsDefeated;
    public int Health => _healthController.Health;

    // --- Properties for external use ---
    public AnimatedSprite Sprite => _animationController.Sprite;
    public Vector2 Position => _movementController.Position;
    public Rectangle Bounds => new Rectangle(
        (int)Position.X + 8,
        (int)Position.Y + 16,
        (int)Sprite.Width / 2,
        (int)Sprite.Height / 2
    );

    public bool FacingRight
    {
        get => Sprite.Effects != SpriteEffects.FlipHorizontally;
        set => Sprite.Effects = value ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
    }

    // --- Constructor ---
    public NPC(Dictionary<string, Animation> animations, Vector2 position, Rectangle bounds, SoundEffect defeatSound)
    {
        var sprite = new AnimatedSprite(animations["Idle"]);
        _animationController = new NpcAnimationController(sprite, animations);
        _movementController = new NpcMovementController(position, 60.0f, bounds);
        _healthController = new NpcHealthController(10, defeatSound);

        // Set initial state
        _animationController.SetAnimation("Idle");
        _movementController.SetIdle();
    }

    // --- Main Update ---
    public void Update(GameTime gameTime, IEnumerable<NPC> allNpcs, Player player)
    {
        float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;

        _animationController.UpdateFlash(elapsed);

        // Health/stun/defeat logic
        if (_healthController.Update(elapsed))
        {
            // If stunned or defeated, skip movement/AI
            _animationController.SetAnimation(_healthController.IsDefeated ? "Defeated" : "Hit");
            Sprite.Update(gameTime);
            return;
        }

        // Movement & collision
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

        if (_attackTimer > 0)
            _attackTimer -= elapsed;

        // Animation state
        if (_movementController.IsIdle)
            _animationController.SetAnimation("Idle");
        else
            _animationController.SetAnimation("Walk");

        Sprite.Update(gameTime);
    }

    // --- Public Actions ---
    public void TakeDamage(int amount, Vector2? knockback = null)
    {
        _healthController.TakeDamage(amount);
        if (knockback.HasValue)
            _movementController.SetVelocity(knockback.Value);
        _animationController.Flash();
    }
    public void Flash()
    {
        _animationController.Flash();
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        Sprite.Draw(spriteBatch, Position);
    }
}

