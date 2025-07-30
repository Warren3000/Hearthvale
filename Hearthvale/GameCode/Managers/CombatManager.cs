using Hearthvale.GameCode.Entities.NPCs;
using Hearthvale.GameCode.Entities.Players;
using Hearthvale.GameCode.Managers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using System.Diagnostics;

namespace Hearthvale.GameCode.Managers;
public class CombatManager
{
    private readonly NpcManager _npcManager;
    private readonly Player _player;
    private readonly ScoreManager _scoreManager;
    private readonly CombatEffectsManager _effectsManager;
    private readonly SoundEffect _hitSound;
    private readonly SoundEffect _defeatSound;

    private float _attackCooldown = 0.5f; // seconds between attacks
    private float _attackTimer = 0f;
    private float _playerDamageCooldown = 1.0f; // seconds of immunity after being hit
    private float _playerDamageTimer = 0f;


    public CombatManager(
        NpcManager npcManager,
        Player player,
        ScoreManager scoreManager,
        CombatEffectsManager effectsManager,
        SoundEffect hitSound,
        SoundEffect defeatSound)
    {
        _npcManager = npcManager;
        _player = player;
        _scoreManager = scoreManager;
        _effectsManager = effectsManager;
        _hitSound = hitSound;
        _defeatSound = defeatSound;
    }

    public void Update(GameTime gameTime)
    {
        if (_attackTimer > 0)
            _attackTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (_playerDamageTimer > 0)
            _playerDamageTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;

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
    public void HandlePlayerAttack(int playerAttackPower)
    {
        if (!CanAttack)
            return;

        Rectangle attackArea = _player.GetAttackArea();
        foreach (var npc in _npcManager.Npcs)
        {
            if (npc.Bounds.Intersects(attackArea))
            {
                // Calculate knockback direction and magnitude
                Vector2 direction = Vector2.Normalize(npc.Position - _player.Position);
                float knockbackStrength = 150f; // Adjust this value for desired pushback
                Vector2 knockback = direction * knockbackStrength;

                if (_player.EquippedWeapon == null)
                    return;
                if (_player.EquippedWeapon != null)
                {
                    npc.TakeDamage(_player.EquippedWeapon.Damage, knockback);
                    _effectsManager.ShowCombatText(npc.Position, _player.EquippedWeapon.Damage.ToString(), Color.Yellow);
                }
                _hitSound?.Play();
                if (npc.IsDefeated)
                    _defeatSound?.Play();
                _scoreManager.Add(1);
                npc.Flash();
                if (_player.EquippedWeapon == null)
                {
                    Debug.WriteLine("Player has no equipped weapon to attack with.");
                }
            }
        }

        _attackTimer = _attackCooldown; // Reset cooldown after attack
    }
}