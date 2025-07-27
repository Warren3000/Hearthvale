using Hearthvale;
using Hearthvale.Managers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

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
    }
    public bool CanAttack => _attackTimer <= 0f;
    public void HandlePlayerAttack(int playerAttackPower)
    {
        if (!CanAttack)
            return;

        Rectangle attackArea = _player.GetAttackArea();
        foreach (var npc in _npcManager.Npcs)
        {
            if (npc.Bounds.Intersects(attackArea))
            {
                Vector2 knockback = Vector2.Normalize(npc.Position - _player.Position) * 2f;
                npc.TakeDamage(playerAttackPower, knockback);

                _hitSound?.Play();
                if (npc.IsDefeated)
                    _defeatSound?.Play();
                _scoreManager.Add(1);
                npc.Flash();

                _effectsManager.PlayHitEffects(npc.Position, playerAttackPower);
            }
        }

        _attackTimer = _attackCooldown; // Reset cooldown after attack
    }
}