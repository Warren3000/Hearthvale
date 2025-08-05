using Hearthvale.GameCode.Entities;
using Hearthvale.GameCode.Entities.Characters;
using Hearthvale.GameCode.Entities.NPCs;
using Hearthvale.GameCode.Data;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System;

namespace Hearthvale.GameCode.Managers;

/// <summary>
/// Manages all combat logic, including player/NPC attacks and projectile handling.
/// </summary>
public class CombatManager
{
    private static CombatManager _instance;
    public static CombatManager Instance => _instance ?? throw new InvalidOperationException("CombatManager not initialized. Call Initialize first.");

    private const float ATTACK_COOLDOWN = 0.5f; // seconds between attacks
    private const float PLAYER_DAMAGE_COOLDOWN = 1.0f; // seconds of immunity after being hit
    private const float PROJECTILE_KNOCKBACK = 150f;
    private const float NPC_ATTACK_KNOCKBACK = 100f;

    private readonly NpcManager _npcManager;
    private Character _player;
    private readonly ScoreManager _scoreManager;
    private readonly CombatEffectsManager _effectsManager;
    private readonly SoundEffect _hitSound;
    private readonly SoundEffect _defeatSound;
    private readonly List<Projectile> _projectiles = new();
    private Rectangle _worldBounds;
    private readonly SpriteBatch _spriteBatch;

    private float _attackCooldown = ATTACK_COOLDOWN; 
    private float _attackTimer = 0f;
    private float _playerDamageCooldown = PLAYER_DAMAGE_COOLDOWN; 
    private float _playerDamageTimer = 0f;
    private readonly Dictionary<Character, bool> _npcHitPlayerThisSwing = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="CombatManager"/> class.
    /// </summary>
    public CombatManager(
        NpcManager npcManager,
        Character player,
        ScoreManager scoreManager,
        SpriteBatch spriteBatch,
        CombatEffectsManager effectsManager,
        SoundEffect hitSound,
        SoundEffect defeatSound,
        Rectangle worldBounds)
    {
        _npcManager = npcManager;
        _player = player;
        _scoreManager = scoreManager;
        _spriteBatch = spriteBatch;
        _effectsManager = effectsManager;
        _hitSound = hitSound;
        _defeatSound = defeatSound;
        _worldBounds = worldBounds;
    }
    public static void Initialize(
        NpcManager npcManager,
        Character player,
        ScoreManager scoreManager,
        SpriteBatch spriteBatch,
        CombatEffectsManager effectsManager,
        SoundEffect hitSound,
        SoundEffect defeatSound,
        Rectangle worldBounds)
    {
        
        _instance = new CombatManager(npcManager, player, scoreManager, spriteBatch, effectsManager, hitSound, defeatSound, worldBounds);
    }

    public void Update(GameTime gameTime)
    {
        if (_player == null)
            return;
        float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;
        if (_attackTimer > 0)
        {
            _attackTimer -= elapsed;
        }

        if (_playerDamageTimer > 0)
            _playerDamageTimer -= elapsed;

        // --- NPC Melee Attack Hit Detection ---
        foreach (var npc in _npcManager.Npcs.Where(n => !n.IsDefeated))
        {
            // Only process if NPC supports combat (has attack info)
            if (npc is ICombatNpc combatNpc && combatNpc.IsAttacking && combatNpc.EquippedWeapon?.IsSlashing == true)
            {
                if (!_npcHitPlayerThisSwing.ContainsKey(npc) || _npcHitPlayerThisSwing[npc] == false)
                {
                    Rectangle npcAttackArea = new Rectangle(
                        combatNpc.GetAttackArea().X,
                        combatNpc.GetAttackArea().Y,
                        combatNpc.GetAttackArea().Width,
                        combatNpc.GetAttackArea().Height);
                    if (npcAttackArea.Intersects(_player.Bounds))
                    {
                        TryDamagePlayer(combatNpc.AttackPower, npc.Position);
                        _npcHitPlayerThisSwing[npc] = true;
                    }
                }
            }
            else if (!(npc is ICombatNpc combatNpc2 && combatNpc2.IsAttacking) && _npcHitPlayerThisSwing.ContainsKey(npc))
            {
                _npcHitPlayerThisSwing.Remove(npc);
            }
        }

        // --- Projectile Hit Detection ---
        // Only player projectiles are supported. If enemy projectiles are added, add ownership checks here.
        for (int i = _projectiles.Count - 1; i >= 0; i--)
        {
            var projectile = _projectiles[i];
            projectile.Update(gameTime);

            if (!projectile.IsActive || !_worldBounds.Contains(projectile.Position))
            {
                _projectiles.RemoveAt(i);
                continue;
            }

            if (projectile.CanCollide)
            {
                foreach (var npc in _npcManager.Npcs.Where(n => !n.IsDefeated))
                {
                    if (projectile.BoundingBox.Intersects(npc.Bounds))
                    {
                        Vector2 direction = Vector2.Normalize(npc.Position - _player.Position);
                        Vector2 knockback = direction * PROJECTILE_KNOCKBACK;

                        HandleNpcHit(npc, projectile.Damage, knockback);
                        
                        projectile.IsActive = false;
                        break; 
                    }
                }
            }
        }
    }
    public bool CanAttack() => _attackTimer <= 0f;
    public void SetPlayer(Character player)
    {
        _player = player;
    }
    public void StartCooldown()
    {
        _attackTimer = _attackCooldown;
    }

    public void RegisterProjectile(Projectile projectile)
    {
        if (projectile != null)
        {
            _projectiles.Add(projectile);
        }
    }

    public void TryDamagePlayer(int amount, Vector2 attackerPosition)
    {
        if (_playerDamageTimer > 0 || _player.IsDefeated)
            return;

        Vector2 direction = Vector2.Normalize(_player.Position - attackerPosition);
        if (direction.LengthSquared() == 0) // Handle case where positions are identical
        {
            direction = Vector2.UnitX; // Default knockback direction
        }
        
        float knockbackStrength = NPC_ATTACK_KNOCKBACK;
        Vector2 knockback = direction * knockbackStrength;
        _player.TakeDamage(amount, knockback);

        _playerDamageTimer = _playerDamageCooldown;
        _effectsManager.ShowCombatText(_player.Position, amount.ToString(), Color.Red);
        _hitSound?.Play();
    }
    public void HandleNpcHit(Character npc, int damage, Vector2? knockback = null)
    {
        Debug.WriteLine($"[CombatManager] HandleNpcHit called for '{npc.GetType().Name}' with {damage} damage.");
        bool justDefeated = npc.TakeDamage(damage, knockback);
        _effectsManager.ShowCombatText(npc.Position, damage.ToString(), Color.Yellow);
        _hitSound?.Play();

        Debug.WriteLine($"[CombatManager] 'justDefeated' returned: {justDefeated}");
        if (justDefeated)
        {
            Debug.WriteLine("[CombatManager] justDefeated is TRUE. Granting XP.");
            _defeatSound?.Play();
            ScoreManager.Instance.Add(1);

            if (npc is NPC typedNpc)
            {
                var stats = DataManager.Instance.GetCharacterStats(typedNpc.Name);
                _player.EquippedWeapon?.GainXP(stats.XpYield);
            }
        }
    }
    public void DrawProjectiles(SpriteBatch spriteBatch)
    {
        foreach (var projectile in _projectiles)
        {
            projectile.Draw(spriteBatch);
        }
    }
}