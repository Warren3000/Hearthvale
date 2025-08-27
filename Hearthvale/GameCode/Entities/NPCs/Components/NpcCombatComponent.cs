using Hearthvale.GameCode.Entities;
using Hearthvale.GameCode.Entities.NPCs;
using Hearthvale.GameCode.Utils;
using Microsoft.Xna.Framework;
using System;

namespace Hearthvale.GameCode.Entities.Components
{
    public class NpcCombatComponent
    {
        private readonly NPC _owner;
        private bool _hasHitPlayerThisSwing = false;

        public int AttackPower { get; set; } = 1;
        public bool CanAttack { get; private set; } = true;
        private float _attackCooldown = 1.5f;
        private float _attackTimer = 0f;

        public NpcCombatComponent(NPC owner, int attackPower = 1)
        {
            _owner = owner;
            AttackPower = attackPower;
        }

        public void Update(float deltaTime)
        {
            if (_attackTimer > 0)
            {
                _attackTimer -= deltaTime;
                if (_attackTimer <= 0)
                {
                    CanAttack = true;
                }
            }

            // Reset hit flag when not attacking
            if (!_owner.IsAttacking)
            {
                _hasHitPlayerThisSwing = false;
            }
        }

        public void StartAttackCooldown(float duration)
        {
            _attackTimer = duration;
            CanAttack = false;
        }

        public bool CheckPlayerHit(Character player)
        {
            // Don't check for hit if not in active combat state
            if (!_owner.IsAttacking || _hasHitPlayerThisSwing || _owner.EquippedWeapon?.IsSlashing != true)
                return false;

            // Use combat-specific attack area (only active during swing)
            Rectangle attackArea = _owner.WeaponComponent.GetAttackArea();

            // Use player's combat bounds (tight sprite bounds)
            Rectangle playerBounds = player.GetCombatBounds();

            if (attackArea.Intersects(playerBounds))
            {
                _hasHitPlayerThisSwing = true;
                return true;
            }

            return false;
        }
    }
}