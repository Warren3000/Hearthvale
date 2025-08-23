using Microsoft.Xna.Framework;
using System;

namespace Hearthvale.GameCode.Entities.Components
{
    public class CharacterHealthComponent
    {
        private int _maxHealth;
        private int _currentHealth;
        private readonly Character _character;

        public int MaxHealth => _maxHealth;
        public int CurrentHealth => _currentHealth;
        public bool IsDefeated => _currentHealth <= 0;
        public bool IsReadyToRemove => _character.IsDefeated && _currentHealth <= 0;

        public CharacterHealthComponent(Character character, int maxHealth)
        {
            _character = character;
            _maxHealth = maxHealth;
            _currentHealth = maxHealth;
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
    }
}