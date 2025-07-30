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
        private const float AttackDuration = 0.3f;
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
                // Apply damage at the start of the attack (or at a specific frame)
                if (_attackTimer > 0f && _attackTimer <= AttackDuration * 0.5f)
                {
                    Rectangle attackArea = _player.GetAttackArea();
                    foreach (var npc in nPCs)
                    {
                        if (npc.Bounds.Intersects(attackArea) && _player.EquippedWeapon != null)
                        {
                            npc.TakeDamage(_player.EquippedWeapon.Damage);
                            _effectsManager.ShowCombatText(npc.Position, _player.EquippedWeapon.Damage.ToString(), Color.Yellow);
                        }
                    }
                }
                if (_attackTimer <= 0f)
                {
                    IsAttacking = false;
                    _player.IsAttacking = false; // Add this line
                    _player.UpdateAnimation(keyboard, false);
                }
            }

            if (IsAttacking && _player.EquippedWeapon != null)
            {
                float attackProgress = 1f - (_attackTimer / AttackDuration);

                float arcAngleStart = _player.FacingRight ? -MathF.PI / 2 : MathF.PI + MathF.PI / 2;
                float arcAngleEnd = _player.FacingRight ? MathF.PI / 2 : MathF.PI - MathF.PI / 2;
                float arcAngle = MathHelper.Lerp(arcAngleStart, arcAngleEnd, attackProgress);

                float radius = 32f;
                Vector2 playerCenter = _player.Position + new Vector2(_player.Sprite.Width / 2, _player.Sprite.Height / 2);

                Vector2 weaponOffset = new Vector2(MathF.Cos(arcAngle), MathF.Sin(arcAngle)) * radius;
                _player.EquippedWeapon.Position = playerCenter + weaponOffset;
                _player.EquippedWeapon.Sprite.Rotation = arcAngle;
                _player.EquippedWeapon.Offset = Vector2.Zero;
            }
            else if (_player.EquippedWeapon != null)
            {
                _player.EquippedWeapon.Rotation = (movement != Vector2.Zero)
                    ? (float)Math.Atan2(_player.LastMovementDirection.Y, _player.LastMovementDirection.X) + MathF.PI / 4f
                    : _player.EquippedWeapon.Rotation;

                float angle = _player.EquippedWeapon.Rotation;
                Vector2 orbitOffset = new Vector2(
                    MathF.Cos(angle) * _player.WeaponOrbitRadius,
                    MathF.Sin(angle) * _player.WeaponOrbitRadius
                );
                Vector2 directionalOffset = Vector2.Zero;
                if (_player.LastMovementDirection.Y < 0)
                    directionalOffset.X -= 4;
                if (_player.LastMovementDirection.X < 0)
                    directionalOffset.Y += 4;
                _player.EquippedWeapon.Offset = orbitOffset + _player.EquippedWeapon.ManualOffset + directionalOffset;
                _player.EquippedWeapon.Position = _player.Position + new Vector2(_player.Sprite.Width / 2, _player.Sprite.Height / 2) + _player.EquippedWeapon.Offset;
            }

            _player.EquippedWeapon?.Update(gameTime);
        }

        public void StartAttack()
        {
            _player.StartAttack();
            IsAttacking = true;
            _player.IsAttacking = true; // Add this line
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
