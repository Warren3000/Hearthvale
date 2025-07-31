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
    private Player _player;
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
    private readonly Dictionary<NPC, bool> _npcHitPlayerThisSwing = new();


    public CombatManager(
    NpcManager npcManager,
    Player player,
    ScoreManager scoreManager,
    SpriteBatch spriteBatch,
    CombatEffectsManager effectsManager,
    SoundEffect hitSound,
    SoundEffect defeatSound,
    SoundEffect playerAttackSound,
    Rectangle worldBounds)
    {
        _npcManager = npcManager;
        _player = player;
        _scoreManager = scoreManager;
        _spriteBatch = spriteBatch;
        _effectsManager = effectsManager;
        _hitSound = hitSound;
        _defeatSound = defeatSound;
        _playerAttackSound = playerAttackSound;
        _worldBounds = worldBounds;
    }

    public void Update(GameTime gameTime)
    {
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
            if (npc.IsAttacking && npc.EquippedWeapon?.IsSlashing == true)
            {
                if (!_npcHitPlayerThisSwing.ContainsKey(npc) || _npcHitPlayerThisSwing[npc] == false)
                {
                    _npcHitPlayerThisSwing[npc] = false;
                    Rectangle npcAttackArea = npc.GetAttackArea();
                    if (npcAttackArea.Intersects(_player.Bounds))
                    {
                        TryDamagePlayer(npc.AttackPower);
                        _npcHitPlayerThisSwing[npc] = true;
                    }
                }
            }
            else if (!npc.IsAttacking && _npcHitPlayerThisSwing.ContainsKey(npc))
            {
                _npcHitPlayerThisSwing.Remove(npc);
            }
        }

        // --- Projectile Hit Detection ---
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
                        projectile.IsActive = false;
                        break; 
                    }
                }
            }
        }
    }
    public bool CanAttack() => _attackTimer <= 0f;
    public void SetPlayer(Player player)
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

    public void TryDamagePlayer(int amount)
    {
        if (_playerDamageTimer > 0 || _player.IsDefeated)
            return;

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
            _player.TakeDamage(amount);
        }

        _playerDamageTimer = _playerDamageCooldown;
        _effectsManager.ShowCombatText(_player.Position, amount.ToString(), Color.Red);
        _hitSound?.Play();
    }
    
    public void DrawProjectiles(SpriteBatch spriteBatch)
    {
        foreach (var projectile in _projectiles)
        {
            projectile.Draw(spriteBatch);
        }
    }
}