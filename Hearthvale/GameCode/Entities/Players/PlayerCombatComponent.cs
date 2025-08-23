using Hearthvale.GameCode.Entities.NPCs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using Hearthvale.GameCode.Managers;
using Microsoft.Xna.Framework.Audio;
using System.Linq;
using System.Diagnostics;
using Hearthvale.GameCode.Utils;
using Hearthvale.GameCode.Entities.Players;

namespace Hearthvale.GameCode.Entities.Components
{
    public class PlayerCombatComponent
    {
        private readonly Player _player;
        private float _attackTimer = 0f;
        private const float AttackDuration = 0.25f;
        private readonly SoundEffect _hitSound;
        private readonly SoundEffect _defeatSound;
        private readonly SoundEffect _playerAttackSound;

        public bool IsAttacking { get; private set; }
        private readonly List<NPC> _hitNpcsThisSwing = new();

        public PlayerCombatComponent(Player player, SoundEffect hitSound, SoundEffect defeatSound, SoundEffect playerAttackSound)
        {
            _player = player;
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

            // Set weapon orbit offset based on movement
            if (_player.EquippedWeapon != null)
            {
                Vector2 orbitOffset = _player.MovementComponent.LastMovementVector * _player.WeaponOrbitRadius;
                _player.EquippedWeapon.Offset = orbitOffset;
            }

            // Weapon position/rotation update is now handled by Player.Update() -> Character.UpdateWeapon()
            // Remove any weapon rotation setting code here

            if (_player.EquippedWeapon?.IsSlashing == true)
            {
                Vector2 playerCenter = _player.GetVisualCenter();
                var hittableNpcs = npcs.Where(n => !n.IsDefeated && !_hitNpcsThisSwing.Contains(n));

                var hitPoly = _player.EquippedWeapon.GetTransformedHitPolygon(playerCenter);
                foreach (var npc in hittableNpcs.ToList())
                {
                    var npcRect = npc.Bounds;
                    var corners = new[]
                    {
                new Vector2(npcRect.Left, npcRect.Top),
                new Vector2(npcRect.Right, npcRect.Top),
                new Vector2(npcRect.Right, npcRect.Bottom),
                new Vector2(npcRect.Left, npcRect.Bottom)
            };
                    if (corners.Any(corner => GeometryUtils.PointInPolygon(corner, hitPoly)))
                    {
                        Vector2 direction = npc.Position - _player.Position;
                        if (direction.LengthSquared() > 0)
                        {
                            direction.Normalize();
                        }
                        else
                        {
                            direction = Vector2.UnitX; // Fallback direction
                        }
                        float knockbackStrength = 150f;
                        Vector2 knockback = direction * knockbackStrength;

                        CombatManager.Instance.HandleNpcHit(npc, _player.EquippedWeapon.Damage, knockback);
                        _hitNpcsThisSwing.Add(npc);
                    }
                }
            }
        }

        public void StartMeleeAttack()
        {
            if (!CombatManager.Instance.CanAttack()) return;

            _hitNpcsThisSwing.Clear();

            if (_player.EquippedWeapon != null)
            {
                CardinalDirection facing = _player.MovementComponent.FacingDirection;
                _player.WeaponComponent.StartSwing(facing);
            }

            _player.StartAttack();
            IsAttacking = true;
            _player.IsAttacking = true;
            _attackTimer = AttackDuration;

            CombatManager.Instance.StartCooldown();
            _playerAttackSound?.Play(0.5f, 0, 0);
        }

        public void StartProjectileAttack()
        {
            if (!CombatManager.Instance.CanAttack() || _player.EquippedWeapon == null) return;

            // Spawn projectile from the visual center to match art (not from padded top-left center)
            Vector2 spawnPosition = _player.GetVisualCenter();
            var projectile = _player.EquippedWeapon.Fire(_player.MovementComponent.LastMovementVector, spawnPosition);

            if (projectile != null)
            {
                CombatManager.Instance.RegisterProjectile(projectile);
                CombatManager.Instance.StartCooldown();
                _playerAttackSound?.Play();
            }
        }

        public void TakeDamage(int amount) => _player.TakeDamage(amount);
        public void Heal(int amount) => _player.Heal(amount);
    }
}