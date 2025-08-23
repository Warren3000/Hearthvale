using Hearthvale.GameCode.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace Hearthvale.GameCode.Entities.Components
{
    public class CharacterWeaponComponent
    {
        private readonly Character _character;
        public Weapon EquippedWeapon { get; private set; }

        public CharacterWeaponComponent(Character character)
        {
            _character = character;
        }

        public void EquipWeapon(Weapon weapon)
        {
            EquippedWeapon = weapon;
        }

        /// <summary>
        /// Updates the weapon position and rotation. For NPCs, pass a target to aim at.
        /// </summary>
        public void Update(GameTime gameTime, Vector2? targetPosition = null)
        {
            if (EquippedWeapon == null) return;

            // Calculate character center using tight bounds for consistent positioning
            Rectangle tightBounds = _character.GetTightSpriteBounds();
            Vector2 characterCenter = new Vector2(
                tightBounds.Left + tightBounds.Width / 2f,
                tightBounds.Top + tightBounds.Height / 2f
            );

            // Calculate weapon offset based on facing direction
            Vector2 weaponOffset = CalculateWeaponOffset();
            
            // Set the weapon's offset, but not its final position.
            // The final position will be calculated in Weapon.Draw based on the owner's center.
            EquippedWeapon.Offset = weaponOffset;

            // Update rotation based on whether we have a target (NPC) or use facing direction (Player)
            if (targetPosition.HasValue && !IsAttacking())
            {
                // NPC logic: aim at target
                Vector2 toTarget = targetPosition.Value - characterCenter;
                if (toTarget.LengthSquared() > 0.0001f)
                {
                    float angle = MathF.Atan2(toTarget.Y, toTarget.X);
                    EquippedWeapon.Rotation = angle;

                    // Update character facing based on target direction
                    _character.FacingRight = toTarget.X >= 0f;
                    _character.MovementComponent.FacingDirection = AngleToCardinal(angle);
                }
            }
            else if (!IsAttacking())
            {
                // Player logic: use character's facing direction
                CardinalDirection facing = _character.MovementComponent.FacingDirection;
                EquippedWeapon.Rotation = facing.ToRotation();
            }

            // Let weapon update its own animation state
            EquippedWeapon.Update(gameTime);
        }

        /// <summary>
        /// Calculates weapon offset based on character's facing direction
        /// </summary>
        private Vector2 CalculateWeaponOffset()
        {
            // Base offset distance from character center
            float offsetDistance = 0f; // Adjust this value to move weapon further/closer

            // Get the character's facing direction
            CardinalDirection facing = _character.MovementComponent.FacingDirection;

            // Apply directional offset based on facing
            Vector2 offset = facing.ToVector() * offsetDistance;

            // REMOVED: Don't add weapon's offset here - it's added in Weapon.Draw()
            return offset;
        }

        /// <summary>
        /// Starts a weapon swing attack
        /// </summary>
        public void StartSwing(CardinalDirection facingDirection)
        {
            if (EquippedWeapon == null) return;

            bool swingClockwise = facingDirection switch
            {
                CardinalDirection.North => true,
                CardinalDirection.East => true,
                CardinalDirection.South => false,
                CardinalDirection.West => false,
                _ => true
            };

            EquippedWeapon.StartSwing(swingClockwise);
        }

        /// <summary>
        /// Gets whether the character is currently attacking
        /// </summary>
        private bool IsAttacking()
        {
            // Check if character has an IsAttacking property
            var isAttackingProp = _character.GetType().GetProperty("IsAttacking");
            if (isAttackingProp != null)
            {
                return (bool)isAttackingProp.GetValue(_character);
            }
            return false;
        }

        /// <summary>
        /// Helper method to convert angle to cardinal direction
        /// </summary>
        private static CardinalDirection AngleToCardinal(float angleRadians)
        {
            // Normalize angle to [0, 2π)
            float normalizedAngle = angleRadians;
            while (normalizedAngle < 0) normalizedAngle += MathF.Tau;
            while (normalizedAngle >= MathF.Tau) normalizedAngle -= MathF.Tau;

            // Convert to degrees for easier reasoning
            float degrees = normalizedAngle * (180f / MathF.PI);

            // Map angle ranges to cardinal directions
            return degrees switch
            {
                >= 315f or < 45f => CardinalDirection.East,
                >= 45f and < 135f => CardinalDirection.South,
                >= 135f and < 225f => CardinalDirection.West,
                >= 225f and < 315f => CardinalDirection.North,
                _ => CardinalDirection.East // fallback
            };
        }

        /// <summary>
        /// Gets the weapon's attack area when slashing - ONLY used for combat hit detection
        /// </summary>
        public Rectangle GetAttackArea()
        {
            if (EquippedWeapon?.IsSlashing != true)
                return Rectangle.Empty; // No attack area when not slashing

            // Use the tight bounds center for weapon positioning
            Rectangle tightBounds = _character.GetTightSpriteBounds();
            Vector2 characterCenter = new Vector2(
                tightBounds.Left + tightBounds.Width / 2f,
                tightBounds.Top + tightBounds.Height / 1.4f
            );

            // Calculate weapon offset based on facing
            Vector2 weaponOffset = CalculateWeaponOffset();
            Vector2 weaponCenter = characterCenter + weaponOffset;

            // Return attack area centered on weapon position
            return new Rectangle(
                (int)(weaponCenter.X - 20),
                (int)(weaponCenter.Y - 20),
                40,
                40
            );
        }
        
        /// <summary>
        /// Gets the weapon's hit polygon for precise combat detection - ONLY used for attack calculations
        /// </summary>
        public List<Vector2> GetCombatHitPolygon()
        {
            if (EquippedWeapon?.IsSlashing != true)
                return new List<Vector2>(); // Empty when not attacking

            Rectangle tightBounds = _character.GetTightSpriteBounds();
            Vector2 characterCenter = new Vector2(
                tightBounds.Left + tightBounds.Width / 2f,
                tightBounds.Top + tightBounds.Height / 1.4f
            );

            return EquippedWeapon.GetTransformedHitPolygon(characterCenter);
        }
    }
}