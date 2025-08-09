using Hearthvale.GameCode.Entities.Interfaces;
using Hearthvale.GameCode.Entities.NPCs.Components;
using Hearthvale.GameCode.Entities.Players;
using Hearthvale.GameCode.Utils;
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

public enum NpcAiType
{
    Wander,
    ChasePlayer
}

public class NPC : Character, ICombatNpc, IDialog
{
    private readonly NpcAnimationController _animationController;
    private readonly NpcMovementComponent _npcMovement;
    private readonly NpcHealthController _healthController;
    private readonly NpcCombatComponent _combatComponent;

    public int AttackPower { get; set; } = 1;
    private float _attackCooldown = 1.5f;
    private Vector2 _lastMovementDirection = Vector2.Zero;

    public bool CanAttack => _combatComponent.CanAttack;
    public void ResetAttackTimer() => _combatComponent.StartAttackCooldown(_attackCooldown);
    public bool IsReadyToRemove => _healthController.IsReadyToRemove;

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
    private NpcAiType _aiType;

    public NPC(string name, Dictionary<string, Animation> animations, Vector2 position, Rectangle bounds, SoundEffect defeatSound, int maxHealth)
    {
        Name = name;
        var sprite = new AnimatedSprite(animations["Idle"]);

        // Initialize base class components first
        InitializeComponents();

        // Create specialized NPC components
        _animationController = new NpcAnimationController(sprite, animations);
        _npcMovement = new NpcMovementComponent(this, position, 60.0f, bounds, (int)sprite.Width, (int)sprite.Height);
        _healthController = new NpcHealthController(maxHealth, defeatSound);
        _combatComponent = new NpcCombatComponent(this, AttackPower);

        // Initialize health component
        HealthComponent.SetMaxHealth(maxHealth);

        // Initialize animation and movement components
        AnimationComponent.SetSprite(sprite);
        MovementComponent.SetPosition(position);

        // Set AI type based on name
        _aiType = name.ToLower() switch
        {
            "merchant" => NpcAiType.Wander,
            "knight" => NpcAiType.ChasePlayer,
            "heavyknight" => NpcAiType.ChasePlayer,
            _ => NpcAiType.Wander
        };
    }

    public override bool TakeDamage(int amount, Vector2? knockback = null)
    {
        if (_healthController.CanTakeDamage)
        {
            bool justDefeated = _healthController.TakeDamage(amount);

            if (knockback.HasValue)
                _npcMovement.SetVelocity(knockback.Value);

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
    }

    public void Update(GameTime gameTime, IEnumerable<NPC> allNpcs, Character player, IEnumerable<Rectangle> rectangles)
    {
        float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // Update combat component
        _combatComponent.Update(elapsed);

        // Check for player hits - returns true if hit occurred
        if (_combatComponent.CheckPlayerHit(player))
        {
            // Notify combat manager about the hit - this will be called from CombatManager
        }

        UpdateHealthAndAnimation(elapsed);

        if (IsDefeated)
            return;

        if (_healthController.IsStunned)
        {
            UpdateKnockback(gameTime);
            SetPosition(Position);
            return;
        }

        if (IsAttacking)
        {
            _attackAnimTimer -= elapsed;
            if (_attackAnimTimer <= 0)
            {
                IsAttacking = false;
            }
        }

        // --- AI Behavior ---
        switch (_aiType)
        {
            case NpcAiType.Wander:
                _npcMovement.SetChaseTarget(null); // Wander randomly
                break;
            case NpcAiType.ChasePlayer:
                _npcMovement.SetChaseTarget(player.Position); // Chase player
                break;
        }

        _npcMovement.Update(elapsed, candidatePos =>
        {
            var allObstacles = rectangles.ToList();
            if (!player.IsDefeated)
                allObstacles.Add(player.Bounds);
            foreach (var npc in allNpcs)
            {
                if (npc != this && !npc.IsDefeated)
                    allObstacles.Add(npc.Bounds);
            }
            Rectangle candidateRect = new Rectangle(
                (int)candidatePos.X + 8,
                (int)candidatePos.Y + 16,
                (int)Sprite.Width / 2,
                (int)Sprite.Height / 2
            );
            return allObstacles.Any(r => candidateRect.Intersects(r));
        });

        UpdateWeapon(gameTime, player);

        float attackRange = EquippedWeapon?.Length ?? 32f;
        if (CanAttack && !IsAttacking && Vector2.Distance(Position, player.Position) < attackRange)
        {
            StartAttack();
        }

        // Update the base movement component with the NPC's specialized movement
        MovementComponent.SetPosition(_npcMovement.Position);
    }

    public void StartAttack()
    {
        IsAttacking = true;
        _attackAnimTimer = AttackAnimDuration;
        EquippedWeapon?.StartSwing(FacingRight);
        ResetAttackTimer(); // Start global attack cooldown
    }

    private void UpdateWeapon(GameTime gameTime, Character player)
    {
        if (EquippedWeapon != null)
        {
            Vector2 directionToPlayer = player.Position - Position;
            if (directionToPlayer != Vector2.Zero)
            {
                // Convert to cardinal direction
                CardinalDirection cardinalDirection = directionToPlayer.ToCardinalDirection();
                
                // Update facing based on cardinal direction
                FacingRight = (cardinalDirection == CardinalDirection.East);
                
                // Set the weapon rotation based on cardinal direction
                EquippedWeapon.Rotation = cardinalDirection.ToRotation();
                
                EquippedWeapon.Position = Position + new Vector2(Sprite.Width / 2, Sprite.Height / 2);
                EquippedWeapon.Update(gameTime);
            }
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
        else if (!_npcMovement.IsIdle)
        {
            desiredAnimation = "Walk";
        }

        _animationController.SetAnimation(desiredAnimation);
        Sprite.Update(new GameTime(new TimeSpan(), TimeSpan.FromSeconds(elapsed)));
    }

    public override void Flash()
    {
        _animationController.Flash();
    }

    public Vector2 GetVelocity()
    {
        return _npcMovement.GetVelocity();
    }

    public bool HandleProjectileHit(int damage, Vector2 knockback)
    {
        return _combatComponent.HandleProjectileHit(damage, knockback);
    }

    public bool CheckPlayerHit(Character player)
    {
        return _combatComponent.CheckPlayerHit(player);
    }

    public void ApplyStatusEffect(string effectType)
    {
        _combatComponent.ApplyStatusEffect(effectType);
    }

    protected override Vector2 GetAttackDirection()
    {
        float angle = EquippedWeapon?.Rotation ?? 0f;
        return new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
    }


    // Add these fields to store obstacle information
    private IEnumerable<Rectangle> _currentObstacles;
    private IEnumerable<NPC> _currentNpcs;
    private Character _player;

    /// <summary>
    /// Updates the obstacle references for knockback collision detection.
    /// </summary>
    public void UpdateObstacles(IEnumerable<Rectangle> obstacleRects, IEnumerable<NPC> npcs, Character player)
    {
        _currentObstacles = obstacleRects;
        _currentNpcs = npcs;
        _player = player;
    }

    public override IEnumerable<Rectangle> GetObstacleRectangles()
    {
        var obstacles = new List<Rectangle>();

        // Add static obstacles
        if (_currentObstacles != null)
        {
            obstacles.AddRange(_currentObstacles);
        }

        // Add other NPC bounds (not self, not defeated)
        if (_currentNpcs != null)
        {
            foreach (var npc in _currentNpcs)
            {
                if (npc != this && !npc.IsDefeated)
                {
                    obstacles.Add(npc.Bounds);
                }
            }
        }

        // Add player bounds
        if (_player != null && !_player.IsDefeated)
        {
            obstacles.Add(_player.Bounds);
        }

        return obstacles;
    }
}