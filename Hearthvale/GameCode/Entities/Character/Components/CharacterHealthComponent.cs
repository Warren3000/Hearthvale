using Microsoft.Xna.Framework;
using System;

namespace Hearthvale.GameCode.Entities.Components
{
    public class CharacterHealthComponent
    {
        private int _maxHealth;
        private int _currentHealth;
        private float _defeatTimer;
        private readonly Character _character;
        private float _hitCooldown = 0.3f;
        private float _hitTimer = 0f;
        private float _stunTimer;
        private float _attackCooldownTimer;
        public bool CanTakeDamage => _hitTimer <= 0f;
        public int MaxHealth => _maxHealth;
        public int CurrentHealth => _currentHealth;
        public bool IsDefeated => _currentHealth <= 0;
        //public bool IsReadyToRemove => _character.IsDefeated && _currentHealth <= 0;
        public bool IsReadyToRemove { get; private set; }

        public CharacterHealthComponent(Character character, int maxHealth)
        {
            _character = character;
            _maxHealth = maxHealth;
            _currentHealth = maxHealth;
        }
        public float DefeatTimerProgress
        {
            get
            {
                if (!IsDefeated || _defeatTimer <= 0f) return 0f;
                return _defeatTimer;
            }
        }

        public bool TakeDamage(int amount, Vector2? knockback = null)
        {
            if (IsDefeated) return false;

            bool wasDefeated = IsDefeated;
            _currentHealth = Math.Max(0, _currentHealth - amount);

            _character.Flash();

            return !wasDefeated && IsDefeated;
        }

        public bool Revive()
        {
            if (!IsDefeated) return false;

            _currentHealth = _maxHealth;
            return true;
        }

        public void Heal(int amount)
        {
            _currentHealth = Math.Min(_maxHealth, _currentHealth + amount);
        }

        public void SetMaxHealth(int maxHealth)
        {
            _maxHealth = Math.Max(1, maxHealth);
            _currentHealth = Math.Min(_currentHealth, _maxHealth);
        }
        public bool Update(float elapsed)
        {
            if (IsDefeated)
            {
                if (_defeatTimer > 0)
                {
                    _defeatTimer -= elapsed;
                    if (_defeatTimer <= 0)
                        IsReadyToRemove = true;
                }
                return false; // No need to update animation state further
            }

            if (_hitTimer > 0)
            {
                _hitTimer -= elapsed;
            }

            if (_stunTimer > 0)
            {
                _stunTimer -= elapsed;
            }

            if (_attackCooldownTimer > 0)
            {
                _attackCooldownTimer -= elapsed;
            }

            return _stunTimer > 0; // Return true only if stunned to trigger hit animation
        }
    }
}