using Hearthvale.GameCode.Data.Models;
using Hearthvale.GameCode.Utils;
using Hearthvale.GameCode.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hearthvale.GameCode.Entities.Components
{
    public class CharacterWeaponComponent
    {
        private readonly Character _character;
        public Weapon EquippedWeapon { get; private set; }
        private WeaponSwingProfile _activeSwingProfile = WeaponSwingProfile.Default;

        public CharacterWeaponComponent(Character character)
        {
            _character = character;
        }

        public void EquipWeapon(Weapon weapon)
        {
            EquippedWeapon = weapon;
        }
        public void UnequipWeapon()
        {
            EquippedWeapon = null;
            _activeSwingProfile = WeaponSwingProfile.Default;
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
            float lateralOffset = 0f;
            float verticalOffset = 0f;

            if (IsAttacking() && _activeSwingProfile?.Shape != null)
            {
                if (_activeSwingProfile.Shape.ForwardOffset.HasValue)
                    offsetDistance += _activeSwingProfile.Shape.ForwardOffset.Value;
                
                if (_activeSwingProfile.Shape.LateralOffset.HasValue)
                    lateralOffset += _activeSwingProfile.Shape.LateralOffset.Value;

                if (_activeSwingProfile.Shape.VerticalOffset.HasValue)
                    verticalOffset += _activeSwingProfile.Shape.VerticalOffset.Value;
            }

            // Get the character's facing direction
            CardinalDirection facing = _character.MovementComponent.FacingDirection;
            Vector2 direction = facing.ToVector();
            
            // Calculate perpendicular vector (right of facing)
            Vector2 right = new Vector2(-direction.Y, direction.X);

            // Apply directional offsets
            Vector2 offset = (direction * offsetDistance) + (right * lateralOffset);
            
            // Apply vertical offset (world Y axis)
            offset.Y += verticalOffset;

            return offset;
        }

        /// <summary>
        /// Starts a weapon swing attack
        /// </summary>
        public void StartSwing(CardinalDirection facingDirection, WeaponSwingProfile swingProfile = null)
        {
            if (EquippedWeapon == null) return;

            // Ensure the weapon pivot is aligned with the initiating facing the instant the swing begins.
            // Without this, input triggered before the next Update() leaves Rotation stuck on the previous frame.
            EquippedWeapon.Rotation = facingDirection.ToRotation();
            EquippedWeapon.Offset = CalculateWeaponOffset();

            _activeSwingProfile = swingProfile ?? WeaponSwingProfile.Default;

            ApplyDefensiveColliderOverride(_activeSwingProfile);

            if (swingProfile != null)
            {
                EquippedWeapon.SetNextSwingProfile(swingProfile);
            }
            else
            {
                EquippedWeapon.SetNextSwingProfile(WeaponSwingProfile.Default);
            }

            bool swingClockwise = facingDirection switch
            {
                CardinalDirection.North => true,
                CardinalDirection.NorthEast => true,
                CardinalDirection.East => true,
                CardinalDirection.SouthEast => false,
                CardinalDirection.South => false,
                CardinalDirection.SouthWest => false,
                CardinalDirection.West => false,
                CardinalDirection.NorthWest => true,
                _ => true
            };

            EquippedWeapon.StartSwing(swingClockwise);
        }

        private void ApplyDefensiveColliderOverride(WeaponSwingProfile swingProfile)
        {
            var defensiveShape = swingProfile?.DefensiveBodyShape
                ?? swingProfile?.SourceProfile?.DefensiveBodyShape;

            if (defensiveShape == null)
            {
                _character.CollisionComponent?.ClearProfileCollider();
            }
            else
            {
                _character.CollisionComponent?.ApplyProfileCollider(defensiveShape);
            }
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
            return CardinalDirectionExtensions.FromAngle(angleRadians);
        }

        /// <summary>
        /// Gets the weapon's attack area when slashing - ONLY used for combat hit detection
        /// </summary>
        public Rectangle GetAttackArea()
        {
            if (EquippedWeapon?.IsSlashing != true)
                return Rectangle.Empty;

            var polygon = GetCombatHitPolygon();
            if (polygon.Count == 0)
                return Rectangle.Empty;

            float minX = polygon.Min(p => p.X);
            float maxX = polygon.Max(p => p.X);
            float minY = polygon.Min(p => p.Y);
            float maxY = polygon.Max(p => p.Y);

            int left = (int)MathF.Floor(minX);
            int top = (int)MathF.Floor(minY);
            int width = Math.Max(1, (int)MathF.Ceiling(maxX - minX));
            int height = Math.Max(1, (int)MathF.Ceiling(maxY - minY));

            return new Rectangle(left, top, width, height);
        }

        public List<Vector2> GetCombatHitPolygon()
        {
            if (EquippedWeapon?.IsSlashing != true)
                return new List<Vector2>(); // Empty when not attacking

            Rectangle tightBounds = _character.GetTightSpriteBounds();
            Vector2 characterCenter = new Vector2(
                tightBounds.Left + tightBounds.Width / 2f,
                tightBounds.Top + tightBounds.Height / 2f
            );

            var shape = _activeSwingProfile?.Shape ?? new AttackShapeDefinition { Type = AttackShapeKind.Arc };
            return shape.Type switch
            {
                AttackShapeKind.Box => BuildBoxPolygon(shape, characterCenter),
                AttackShapeKind.Thrust => BuildThrustPolygon(shape, characterCenter),
                AttackShapeKind.Area => BuildAreaPolygon(shape, characterCenter),
                _ => EquippedWeapon.GetTransformedHitPolygon(characterCenter)
            };
        }

        public MagicEffectDefinition GetActiveMagicEffect() => _activeSwingProfile?.Magic;

        private List<Vector2> BuildBoxPolygon(AttackShapeDefinition shape, Vector2 origin)
        {
            float angle = EquippedWeapon?.Rotation ?? _character.MovementComponent.FacingDirection.ToRotation();
            Vector2 forward = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
            Vector2 right = new Vector2(-forward.Y, forward.X);

            float length = shape.Length ?? shape.Height ?? (EquippedWeapon?.Length ?? 32f);
            float width = shape.Width ?? 24f;
            float forwardOffset = shape.ForwardOffset ?? (length * 0.5f);
            float lateralOffset = shape.LateralOffset ?? 0f;
            float verticalOffset = shape.VerticalOffset ?? 0f;

            Vector2 center = origin + forward * forwardOffset + right * lateralOffset + new Vector2(0f, verticalOffset);
            Vector2 halfForward = forward * (length * 0.5f);
            Vector2 halfRight = right * (width * 0.5f);

            return new List<Vector2>
            {
                center - halfForward - halfRight,
                center + halfForward - halfRight,
                center + halfForward + halfRight,
                center - halfForward + halfRight
            };
        }

        private List<Vector2> BuildThrustPolygon(AttackShapeDefinition shape, Vector2 origin)
        {
            float angle = EquippedWeapon?.Rotation ?? _character.MovementComponent.FacingDirection.ToRotation();
            Vector2 forward = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
            Vector2 right = new Vector2(-forward.Y, forward.X);

            float length = shape.Length ?? (EquippedWeapon?.Length ?? 40f);
            float thickness = shape.Thickness ?? shape.Width ?? 12f;
            float forwardOffset = shape.ForwardOffset ?? (length * 0.5f);
            float lateralOffset = shape.LateralOffset ?? 0f;
            float verticalOffset = shape.VerticalOffset ?? 0f;

            Vector2 center = origin + forward * forwardOffset + right * lateralOffset + new Vector2(0f, verticalOffset);
            Vector2 halfForward = forward * (length * 0.5f);
            Vector2 halfRight = right * (thickness * 0.5f);

            return new List<Vector2>
            {
                center - halfForward - halfRight,
                center + halfForward - halfRight,
                center + halfForward + halfRight,
                center - halfForward + halfRight
            };
        }

        private List<Vector2> BuildAreaPolygon(AttackShapeDefinition shape, Vector2 origin)
        {
            float radius = Math.Max(1f, shape.Radius ?? (EquippedWeapon?.Length ?? 32f));
            int segments = Math.Clamp(shape.SegmentCount ?? 12, 6, 64);
            float angleOffset = _character.MovementComponent.FacingDirection.ToRotation();
            Vector2 forward = new Vector2(MathF.Cos(angleOffset), MathF.Sin(angleOffset));
            Vector2 right = new Vector2(-forward.Y, forward.X);

            float forwardOffset = shape.ForwardOffset ?? 0f;
            float lateralOffset = shape.LateralOffset ?? 0f;
            float verticalOffset = shape.VerticalOffset ?? 0f;

            Vector2 center = origin + forward * forwardOffset + right * lateralOffset + new Vector2(0f, verticalOffset);

            float coneDegrees = shape.ConeAngleDegrees ?? 360f;
            bool isFullCircle = coneDegrees >= 360f - float.Epsilon;
            float coneRadians = MathHelper.ToRadians(MathHelper.Clamp(coneDegrees, 1f, 360f));

            var points = new List<Vector2>(segments + (isFullCircle ? 0 : 2));

            if (!isFullCircle)
            {
                points.Add(center);
            }

            float startAngle = isFullCircle
                ? 0f
                : angleOffset - coneRadians * 0.5f;

            float step = coneRadians / segments;
            for (int i = 0; i <= segments; i++)
            {
                float angle = startAngle + step * i;
                Vector2 dir = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
                Vector2 point = center + dir * radius;
                points.Add(point);
            }

            return points;
        }
    }
}