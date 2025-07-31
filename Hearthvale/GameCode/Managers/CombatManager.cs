using Hearthvale.GameCode.Entities;
using Hearthvale.GameCode.Entities.NPCs;
using Hearthvale.GameCode.Entities.Players;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Hearthvale.GameCode.Managers;
public class CombatManager
{
    private readonly NpcManager _npcManager;
    private readonly Player _player;
    private readonly ScoreManager _scoreManager;
    private readonly CombatEffectsManager _effectsManager;
    private readonly SoundEffect _hitSound;
    private readonly SoundEffect _defeatSound;
    private readonly List<Projectile> _projectiles = new();
    private Rectangle _worldBounds;
    private readonly SpriteBatch _spriteBatch;

    private readonly SoundEffect _playerAttackSound; 
    private float _attackCooldown = 0.5f; // seconds between attacks
    private float _attackTimer = 0f;
    private float _playerDamageCooldown = 1.0f; // seconds of immunity after being hit
    private float _playerDamageTimer = 0f;
    private readonly List<NPC> _hitNpcsThisSwing = new();
    private readonly Dictionary<NPC, bool> _npcHitPlayerThisSwing = new();


    public CombatManager(
    NpcManager npcManager,
    Player player,
    ScoreManager scoreManager,
    SpriteBatch spriteBatch,
    CombatEffectsManager effectsManager,
    SoundEffect hitSound,
    SoundEffect defeatSound,
    SoundEffect playerAttackSound, // Add this parameter
    Rectangle worldBounds)
    {
        _npcManager = npcManager;
        _player = player;
        _scoreManager = scoreManager;
        _spriteBatch = spriteBatch;
        _effectsManager = effectsManager;
        _hitSound = hitSound;
        _defeatSound = defeatSound;
        _playerAttackSound = playerAttackSound; // Assign
        _worldBounds = worldBounds;
    }

    public void Update(GameTime gameTime)
    {
        if (_attackTimer > 0)
            _attackTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (_playerDamageTimer > 0)
            _playerDamageTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;

        // --- NPC Melee Attack Hit Detection ---
        foreach (var npc in _npcManager.Npcs.Where(n => !n.IsDefeated))
        {
            if (npc.IsAttacking && npc.EquippedWeapon?.IsSlashing == true)
            {
                // Check if this NPC has already hit the player during its current swing
                if (!_npcHitPlayerThisSwing.ContainsKey(npc))
                {
                    _npcHitPlayerThisSwing[npc] = false;
                }

                if (_npcHitPlayerThisSwing[npc] == false)
                {
                    Rectangle npcAttackArea = npc.GetAttackArea();
                    if (npcAttackArea.Intersects(_player.Bounds))
                    {
                        TryDamagePlayer(npc.AttackPower);
                        _npcHitPlayerThisSwing[npc] = true; // Mark as hit for this swing
                    }
                }
            }
            else if (!npc.IsAttacking)
            {
                // Reset when the NPC is no longer attacking
                if (_npcHitPlayerThisSwing.ContainsKey(npc))
                {
                    _npcHitPlayerThisSwing.Remove(npc);
                }
            }
        }

        // --- Player Melee Attack Hit Detection Logic ---
        if (_player.IsAttacking && _player.EquippedWeapon?.IsSlashing == true)
        {
            Rectangle attackArea = _player.GetAttackArea();
            var hittableNpcs = _npcManager.Npcs.Where(n => !n.IsDefeated && !_hitNpcsThisSwing.Contains(n));

            foreach (var npc in hittableNpcs.ToList()) // ToList() prevents collection modification issues
            {
                // Revert to Intersects for more reliable close-range hit detection.
                // The _hitNpcsThisSwing list now correctly prevents single swings from hitting multiple times.
                if (attackArea.Intersects(npc.Bounds))
                {
                    Vector2 direction = Vector2.Normalize(npc.Position - _player.Position);
                    float knockbackStrength = 150f;
                    Vector2 knockback = direction * knockbackStrength;

                    npc.TakeDamage(_player.EquippedWeapon.Damage, knockback);
                    _effectsManager.ShowCombatText(npc.Position, _player.EquippedWeapon.Damage.ToString(), Color.Yellow);
                    _hitSound?.Play();
                    if (npc.IsDefeated)
                    {
                        _defeatSound?.Play();
                        _scoreManager.Add(1);
                    }
                    
                    _hitNpcsThisSwing.Add(npc); // Prevent hitting the same NPC multiple times in one swing
                }
            }
        }
        else if (!_player.IsAttacking)
        {
            if (_hitNpcsThisSwing.Count > 0)
            {
                _hitNpcsThisSwing.Clear();
            }
        }

        // Update projectiles and check for collisions
        for (int i = _projectiles.Count - 1; i >= 0; i--)
        {
            var projectile = _projectiles[i];
            projectile.Update(gameTime);

            if (!projectile.IsActive || !_worldBounds.Contains(projectile.Position))
            {
                _projectiles.RemoveAt(i);
                continue;
            }

            // Only check for collisions after the grace period
            if (projectile.CanCollide)
            {
                foreach (var npc in _npcManager.Npcs.Where(n => !n.IsDefeated))
                {
                    if (projectile.BoundingBox.Intersects(npc.Bounds))
                    {
                        Vector2 direction = Vector2.Normalize(npc.Position - _player.Position);
                        float knockbackStrength = 150f;
                        Vector2 knockback = direction * knockbackStrength;

                        npc.TakeDamage(projectile.Damage, knockback);
                        _effectsManager.ShowCombatText(npc.Position, projectile.Damage.ToString(), Color.Yellow);
                        _hitSound?.Play();
                        if (npc.IsDefeated)
                        {
                            _defeatSound?.Play();
                            _scoreManager.Add(1);
                        }
                        projectile.IsActive = false; // Deactivate projectile on hit
                        break; // Projectile hits one NPC at most
                    }
                }
            }
        }
    }
    public bool CanAttack => _attackTimer <= 0f;

    public void TryDamagePlayer(int amount)
    {
        if (_playerDamageTimer > 0 || _player.IsDefeated)
            return;

        // Calculate knockback source from the closest attacking NPC
        NPC closestAttacker = _npcManager.Npcs
            .Where(n => n.IsAttacking)
            .OrderBy(n => Vector2.Distance(n.Position, _player.Position))
            .FirstOrDefault();

        if (closestAttacker != null)
        {
            Vector2 direction = Vector2.Normalize(_player.Position - closestAttacker.Position);
            float knockbackStrength = 100f;
            Vector2 knockback = direction * knockbackStrength;
            _player.TakeDamage(amount, knockback);
        }
        else
        {
            _player.TakeDamage(amount); // Take damage without knockback if source is unclear
        }

        _playerDamageTimer = _playerDamageCooldown;
        _effectsManager.ShowCombatText(_player.Position, amount.ToString(), Color.Yellow);
        _hitSound?.Play(); // Play hit sound for player
    }
    public void HandlePlayerMeleeAttack()
    {
        if (!CanAttack) return;

        _player.CombatController.StartAttack(); // This now just triggers the animation and IsAttacking state
        _attackTimer = _attackCooldown;
        _playerAttackSound?.Play(0.5f, 0, 0); // Play swing sound

        // The damage logic has been moved to the Update method.
    }

    public void HandlePlayerProjectileAttack()
    {
        if (!CanAttack || _player.EquippedWeapon == null)
            return;

        // Spawn the projectile from the player's center for consistency
        Vector2 spawnPosition = _player.Position + new Vector2(_player.Sprite.Width / 2, _player.Sprite.Height / 2);
        var projectile = _player.EquippedWeapon.Fire(_player.LastMovementDirection, spawnPosition);

        if (projectile != null)
        {
            _projectiles.Add(projectile);
            _attackTimer = _attackCooldown; // Reset cooldown after attack
            _playerAttackSound?.Play(); // Play attack sound
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