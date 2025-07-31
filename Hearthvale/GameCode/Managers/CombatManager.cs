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

    private float _attackCooldown = 0.5f; // seconds between attacks
    private float _attackTimer = 0f;
    private float _playerDamageCooldown = 1.0f; // seconds of immunity after being hit
    private float _playerDamageTimer = 0f;


    public CombatManager(
        NpcManager npcManager,
        Player player,
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

    public void Update(GameTime gameTime)
    {
        if (_attackTimer > 0)
            _attackTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (_playerDamageTimer > 0)
            _playerDamageTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;

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

        foreach (var npc in _npcManager.Npcs)
        {
            if (!npc.IsDefeated && npc.Bounds.Intersects(_player.Bounds) && npc.CanAttack)
            {
                TryDamagePlayer(npc.AttackPower);
                npc.ResetAttackTimer();
            }
        }
    }
    public bool CanAttack => _attackTimer <= 0f;

    public void TryDamagePlayer(int amount)
    {
        if (_playerDamageTimer > 0 || _player.IsDefeated)
            return;

        _player.TakeDamage(amount);
        _playerDamageTimer = _playerDamageCooldown;
        _effectsManager.ShowCombatText(_player.Position, amount.ToString(), Color.Yellow);
    }
    public void HandlePlayerAttack()
    {
        if (!CanAttack || _player.EquippedWeapon == null)
            return;

        var projectile = _player.EquippedWeapon.Fire(_player.LastMovementDirection);
        if (projectile != null)
        {
            _projectiles.Add(projectile);
            _attackTimer = _attackCooldown; // Reset cooldown after attack
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