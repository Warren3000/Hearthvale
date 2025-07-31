using Hearthvale.GameCode.Entities.NPCs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using Hearthvale.GameCode.Managers;

namespace Hearthvale.GameCode.Entities.Players
{
    public class PlayerCombatController
    {
        private readonly Player _player;
        private float _attackTimer = 0f;
        private const float AttackDuration = 0.25f; // WindUp (0.15) + Slash (0.1)
        private CombatEffectsManager _effectsManager;

        public bool IsAttacking { get; private set; }

        public PlayerCombatController(Player player, CombatEffectsManager combatEffectsManager)
        {
            _player = player;
            _effectsManager = combatEffectsManager;
        }

        public void Update(GameTime gameTime, KeyboardState keyboard, Vector2 movement, IEnumerable<NPC> nPCs)
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
                // Only update orbiting rotation if not swinging
                if (!IsAttacking)
                {
                    // The weapon should always point in the last moved direction
                    const float rotationOffset = MathHelper.Pi / 4f; // 45 degrees in radians
                    _player.EquippedWeapon.Rotation = (float)Math.Atan2(_player.LastMovementDirection.Y, _player.LastMovementDirection.X) + rotationOffset;
                }

                // The weapon's position should be based on the player's center, not its own rotation angle
                Vector2 playerCenter = _player.Position + new Vector2(_player.Sprite.Width / 2f, _player.Sprite.Height / 2f);
                
                // Calculate offset from player center based on direction
                Vector2 orbitOffset = _player.LastMovementDirection * _player.WeaponOrbitRadius;

                _player.EquippedWeapon.Offset = orbitOffset + _player.EquippedWeapon.ManualOffset;
                _player.EquippedWeapon.Position = playerCenter + _player.EquippedWeapon.Offset;
            }

            _player.EquippedWeapon?.Update(gameTime);
        }

        public void StartAttack()
        {
            if (_player.EquippedWeapon != null)
            {
                // The swing direction (clockwise/counter-clockwise) should be based on horizontal facing
                _player.EquippedWeapon.StartSwing(_player.FacingRight);
            }
            _player.StartAttack();
            IsAttacking = true;
            _player.IsAttacking = true;
            _attackTimer = AttackDuration;
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