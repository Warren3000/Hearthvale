using Microsoft.Xna.Framework.Audio;

namespace Hearthvale.Content.NPC;
public class NpcHealthController
{
    private int _maxHealth;
    private int _currentHealth;
    private bool _isDefeated;
    private float _defeatTimer;
    private float _stunTimer;
    private readonly SoundEffect _defeatSound;

    public bool IsDefeated => _isDefeated;
    public bool IsReadyToRemove { get; private set; }
    public int Health => _currentHealth;

    public NpcHealthController(int maxHealth, SoundEffect defeatSound)
    {
        _maxHealth = maxHealth;
        _currentHealth = maxHealth;
        _defeatSound = defeatSound;
    }

    public void TakeDamage(int amount, float stunDuration = 0.3f)
    {
        if (_isDefeated) return;
        _currentHealth -= amount;
        _stunTimer = stunDuration;
        if (_currentHealth <= 0)
        {
            _currentHealth = 0;
            _isDefeated = true;
            _defeatTimer = 1.0f;
            _defeatSound?.Play();
        }
    }

    public bool Update(float elapsed)
    {
        if (_isDefeated)
        {
            if (_defeatTimer > 0)
            {
                _defeatTimer -= elapsed;
                if (_defeatTimer <= 0)
                    IsReadyToRemove = true;
            }
            return true;
        }
        if (_stunTimer > 0)
        {
            _stunTimer -= elapsed;
            return true;
        }
        return false;
    }
}