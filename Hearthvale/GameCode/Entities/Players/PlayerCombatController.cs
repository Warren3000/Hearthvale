using Hearthvale.GameCode.Entities.NPCs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using Hearthvale.GameCode.Managers;
using Microsoft.Xna.Framework.Audio;
using System.Linq;
using System.Diagnostics;

namespace Hearthvale.GameCode.Entities.Players
{
    public class PlayerCombatController
    {
        private readonly Player _player;
        private float _attackTimer = 0f;
        private const float AttackDuration = 0.25f;
        private readonly CombatEffectsManager _effectsManager;
        private readonly ScoreManager _scoreManager;
        private readonly SoundEffect _hitSound;
        private readonly SoundEffect _defeatSound;
        private readonly SoundEffect _playerAttackSound;
        private readonly CombatManager _combatManager;

        public bool IsAttacking { get; private set; }
        private readonly List<NPC> _hitNpcsThisSwing = new();

        public PlayerCombatController(Player player, CombatManager combatManager, CombatEffectsManager combatEffectsManager, ScoreManager scoreManager, SoundEffect hitSound, SoundEffect defeatSound, SoundEffect playerAttackSound)
        {
            _player = player;
            _combatManager = combatManager;
            _effectsManager = combatEffectsManager;
            _scoreManager = scoreManager;
            _hitSound = hitSound;
            _defeatSound = defeatSound;
            _playerAttackSound = playerAttackSound;
        }

        public void Update(GameTime gameTime, IEnumerable<NPC> npcs)
        {
            if (IsAttacking)
            {
                _attackTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (_attackTimer <= 0f)
                {
                    IsAttacking = false;
                    _player.IsAttacking = false;
                }
            }

            if (_player.EquippedWeapon != null)
            {
                if (!IsAttacking)
                {
                    const float rotationOffset = MathHelper.Pi / 4f;
                    _player.EquippedWeapon.Rotation = (float)Math.Atan2(_player.LastMovementDirection.Y, _player.LastMovementDirection.X) + rotationOffset;
                }

                Vector2 playerCenter = _player.Position + new Vector2(_player.Sprite.Width / 2f, _player.Sprite.Height / 2f);
                Vector2 orbitOffset = _player.LastMovementDirection * _player.WeaponOrbitRadius;
                _player.EquippedWeapon.Offset = orbitOffset + _player.EquippedWeapon.ManualOffset;
                _player.EquippedWeapon.Position = playerCenter + _player.EquippedWeapon.Offset;
            }

            _player.EquippedWeapon?.Update(gameTime);

            if (_player.EquippedWeapon?.IsSlashing == true)
            {
                Rectangle attackArea = _player.GetAttackArea();
                var hittableNpcs = npcs.Where(n => !n.IsDefeated && !_hitNpcsThisSwing.Contains(n));

                foreach (var npc in hittableNpcs.ToList())
                {
                    if (attackArea.Intersects(npc.Bounds))
                    {
                        // --- Apply Knockback ---
                        Vector2 direction = Vector2.Normalize(npc.Position - _player.Position);
                        float knockbackStrength = 150f; // Adjust as needed
                        Vector2 knockback = direction * knockbackStrength;
                        
                        _combatManager.HandleNpcHit(npc, _player.EquippedWeapon.Damage, knockback);
                        
                        _hitNpcsThisSwing.Add(npc);
                    }
                }
            }
        }

        public void StartMeleeAttack()
        {
            if (!_combatManager.CanAttack()) return;
            
            Debug.WriteLine($"[PlayerCombatController] Starting new melee attack. Clearing hit list ({_hitNpcsThisSwing.Count} items).");
            _hitNpcsThisSwing.Clear();

            if (_player.EquippedWeapon != null)
            {
                _player.EquippedWeapon.StartSwing(_player.FacingRight);
            }
            _player.StartAttack();
            IsAttacking = true;
            _player.IsAttacking = true;
            _attackTimer = AttackDuration;
            
            _combatManager.StartCooldown();
            _playerAttackSound?.Play(0.5f, 0, 0);
        }

        public void StartProjectileAttack()
        {
            if (!_combatManager.CanAttack() || _player.EquippedWeapon == null) return;

            Vector2 spawnPosition = _player.Position + new Vector2(_player.Sprite.Width / 2, _player.Sprite.Height / 2);
            var projectile = _player.EquippedWeapon.Fire(_player.LastMovementDirection, spawnPosition);

            if (projectile != null)
            {
                _combatManager.RegisterProjectile(projectile);
                _combatManager.StartCooldown();
                _playerAttackSound?.Play();
            }
        }

        public void TakeDamage(int amount)
        {
            _player.TakeDamage(amount);
        }

        public void Heal(int amount)
        {
            _player.Heal(amount);
        }
    }
}