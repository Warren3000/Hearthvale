using Hearthvale.GameCode.Entities;
using Hearthvale.GameCode.Utils;
using Microsoft.Xna.Framework;
using System;

namespace Hearthvale.GameCode.Entities.NPCs.Components
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
            // Don't check for hit if:
            // - NPC is not attacking
            // - We already hit the player this swing
            // - The weapon isn't in the slashing phase
            if (!_owner.IsAttacking || _hasHitPlayerThisSwing || _owner.EquippedWeapon?.IsSlashing != true)
                return false;

            // Get the attack area using analyzed sprite bounds for the weapon
            Rectangle attackArea = _owner.GetAttackArea();

            // Use orientation-aware bounds instead of direct bounds for accurate hit detection
            Rectangle playerBounds = player.GetOrientationAwareBounds();

            // Check for intersection using the analyzed bounds
            if (attackArea.Intersects(playerBounds))
            {
                _hasHitPlayerThisSwing = true;
                return true;
            }

            return false;
        }

        public void ResetHitFlag()
        {
            _hasHitPlayerThisSwing = false;
        }

        public bool HandleProjectileHit(int damage, Vector2 knockback)
        {
            return _owner.TakeDamage(damage, knockback);
        }

        public void ApplyStatusEffect(string effectType)
        {
            switch (effectType)
            {
                case "Burn":
                    // Implement burn logic
                    break;
                case "Magic":
                    // Implement magic effect logic
                    break;
                default:
                    break;
            }
        }
    }
}