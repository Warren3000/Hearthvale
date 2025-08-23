using System;
using System.Diagnostics;
using Microsoft.Xna.Framework.Audio;

namespace Hearthvale.GameCode.Entities.Components
{
    public class NpcHealthComponent
    {
        private int _maxHealth;
        private int _currentHealth;
        private float _hitCooldown = 0.3f;
        private float _hitTimer = 0f;
        private bool _isDefeated;
        private float _defeatTimer;
        private float _stunTimer;
        private float _attackCooldownTimer; // New timer for attack cooldown
        private readonly SoundEffect _defeatSound;

        public bool CanTakeDamage => _hitTimer <= 0f;
        public bool IsDefeated => _isDefeated;
        public bool IsReadyToRemove { get; private set; }
        public int Health => _currentHealth;
        public int MaxHealth => _maxHealth; // ADD THIS PROPERTY
        public bool IsStunned => _stunTimer > 0f;
        public bool IsOnCooldown => _hitTimer > 0f || _stunTimer > 0f || _attackCooldownTimer > 0f;

        public float DefeatTimerProgress
        {
            get
            {
                if (!_isDefeated || _defeatTimer <= 0f) return 0f;
                return _defeatTimer;
            }
        }

        public NpcHealthComponent(int maxHealth, SoundEffect defeatSound)
        {
            _maxHealth = maxHealth;
            _currentHealth = maxHealth;
            _defeatSound = defeatSound;
        }
        public bool TakeDamage(int amount, float stunDuration = 0.3f)
        {
            if (!CanTakeDamage) return false;

            int oldHealth = _currentHealth;
            _currentHealth -= amount;
            _stunTimer = stunDuration;
            _hitTimer = _hitCooldown;

            Debug.WriteLine($"------[NpcHealthController] Damage applied. Health: {oldHealth} -> {_currentHealth}.");

            if (_currentHealth <= 0 && !_isDefeated)
            {
                Debug.WriteLine("------[NpcHealthController] Health is <= 0 and not already defeated. SETTING DEFEATED.");
                _currentHealth = 0;
                _isDefeated = true;
                _defeatTimer = 1.0f;
                _defeatSound?.Play();
                return true; // NPC was just defeated
            }
            return false; // NPC was damaged but not defeated
        }

        public void Heal(int amount)
        {
            if (!_isDefeated)
                _currentHealth = Math.Min(_maxHealth, _currentHealth + amount);
        }

        public void StartAttackCooldown(float duration)
        {
            _attackCooldownTimer = duration;
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