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

namespace Hearthvale.GameCode.Entities.Players
{
    public class PlayerCombatController
    {
        private readonly Player _player;
        private float _attackTimer = 0f;
        private const float AttackDuration = 0.25f;
        private readonly SoundEffect _hitSound;
        private readonly SoundEffect _defeatSound;
        private readonly SoundEffect _playerAttackSound;

        public bool IsAttacking { get; private set; }
        private readonly List<NPC> _hitNpcsThisSwing = new();

        public PlayerCombatController(Player player, SoundEffect hitSound, SoundEffect defeatSound, SoundEffect playerAttackSound)
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

            if (_player.EquippedWeapon != null)
            {
                if (!IsAttacking)
                {
                    // Get the cardinal direction and convert to rotation angle
                    CardinalDirection facing = _player.MovementComponent.FacingDirection;
                    _player.EquippedWeapon.Rotation = facing.ToRotation();
                }

                Vector2 playerCenter = _player.Position + new Vector2(_player.Sprite.Width / 2f, _player.Sprite.Height / 2f);
                Vector2 orbitOffset = _player.MovementComponent.LastMovementVector * _player.WeaponOrbitRadius;
                _player.EquippedWeapon.Offset = orbitOffset + _player.EquippedWeapon.ManualOffset;
                _player.EquippedWeapon.Position = playerCenter + _player.EquippedWeapon.Offset;
            }

            _player.EquippedWeapon?.Update(gameTime);

            if (_player.EquippedWeapon?.IsSlashing == true)
            {
                Vector2 playerCenter = _player.Position + new Vector2(_player.Sprite.Width / 2f, _player.Sprite.Height / 2f);
                var hittableNpcs = npcs.Where(n => !n.IsDefeated && !_hitNpcsThisSwing.Contains(n));

                var hitPoly = _player.EquippedWeapon.GetTransformedHitPolygon(playerCenter);
                foreach (var npc in hittableNpcs.ToList())
                {
                    var npcRect = npc.Bounds;
                    // Check if any corner of the NPC's bounds is inside the hit polygon
                    var corners = new[]
                    {
                        new Vector2(npcRect.Left, npcRect.Top),
                        new Vector2(npcRect.Right, npcRect.Top),
                        new Vector2(npcRect.Right, npcRect.Bottom),
                        new Vector2(npcRect.Left, npcRect.Bottom)
                    };
                    if (corners.Any(corner => GeometryUtils.PointInPolygon(corner, hitPoly)))
                    {
                        // --- Apply Knockback ---
                        Vector2 direction = Vector2.Normalize(npc.Position - _player.Position);
                        float knockbackStrength = 150f; // Adjust as needed
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
                // Always set the rotation first based on cardinal direction
                CardinalDirection facing = _player.MovementComponent.FacingDirection;
                _player.EquippedWeapon.Rotation = facing.ToRotation();
                
                // Always use the correct swing direction based on facing
                bool swingClockwise;
                
                // Determine swing direction based on cardinal direction
                // This ensures the swing always looks good from that direction
                switch (facing)
                {
                    case CardinalDirection.North:
                        swingClockwise = true;
                        break;
                    case CardinalDirection.East:
                        swingClockwise = true;
                        break;
                    case CardinalDirection.South:
                        swingClockwise = false;
                        break;
                    case CardinalDirection.West:
                        swingClockwise = false;
                        break;
                    default:
                        swingClockwise = true;
                        break;
                }
                
                _player.EquippedWeapon.StartSwing(swingClockwise);
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

            Vector2 spawnPosition = _player.Position + new Vector2(_player.Sprite.Width / 2, _player.Sprite.Height / 2);
            var projectile = _player.EquippedWeapon.Fire(_player.MovementComponent.LastMovementVector, spawnPosition);

            if (projectile != null)
            {
                CombatManager.Instance.RegisterProjectile(projectile);
                CombatManager.Instance.StartCooldown();
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