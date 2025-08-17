using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

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

        public Rectangle GetAttackArea()
        {
            if (EquippedWeapon == null) return Rectangle.Empty;

            // Prefer the weapon's current world position (grip/handle point) if available.
            // Falls back to character center if not yet set.
            Vector2 basePoint = (EquippedWeapon.Sprite != null)
                ? EquippedWeapon.Sprite.Position
                : _character.Position + new Vector2(_character.Sprite.Width / 2f, _character.Sprite.Height / 2f);

            Vector2 direction = CalculateAttackDirection();

            return CalculateWeaponArea(basePoint, direction);
        }

        private Vector2 CalculateAttackDirection()
        {
            const float visualRotationOffset = MathHelper.PiOver4;
            float totalRotation = EquippedWeapon.Rotation + visualRotationOffset;
            return new Vector2((float)Math.Cos(totalRotation), (float)Math.Sin(totalRotation));
        }

        private Rectangle CalculateWeaponArea(Vector2 origin, Vector2 direction)
        {
            // Match the same notion of handle/blade length used by DebugManager arcs and Weapon polygon
            const float handleOffset = 6f;
            const float thickness = 12f;

            float length = MathF.Max(0f, EquippedWeapon.Length - handleOffset);

            // Oriented rectangle along the blade direction, starting just past the handle
            Vector2 perp = new Vector2(-direction.Y, direction.X) * (thickness / 2f);
            Vector2 start = origin + direction * handleOffset;
            Vector2 end = start + direction * length;

            Vector2[] points = {
                start + perp,
                start - perp,
                end + perp,
                end - perp
            };

            return CalculateBoundingRectangle(points);
        }

        private Rectangle CalculateBoundingRectangle(Vector2[] points)
        {
            float minX = float.MaxValue, maxX = float.MinValue;
            float minY = float.MaxValue, maxY = float.MinValue;

            foreach (var point in points)
            {
                minX = MathF.Min(minX, point.X);
                maxX = MathF.Max(maxX, point.X);
                minY = MathF.Min(minY, point.Y);
                maxY = MathF.Max(maxY, point.Y);
            }

            // Use floor/ceil to ensure the AABB fully contains the rotated rect
            int x = (int)MathF.Floor(minX);
            int y = (int)MathF.Floor(minY);
            int w = (int)MathF.Ceiling(maxX - minX);
            int h = (int)MathF.Ceiling(maxY - minY);

            return new Rectangle(x, y, w, h);
        }

        public void DrawWeapon(SpriteBatch spriteBatch, Vector2 characterCenter, bool drawBehind)
        {
            if (EquippedWeapon == null) return;

            // Ensure deterministic z-order against the character sprite
            float baseDepth = _character.Sprite?.LayerDepth ?? 0.5f;
            EquippedWeapon.Sprite.LayerDepth = Math.Clamp(baseDepth - 0.0001f, 0f, 1f);

            if (drawBehind)
            {
                EquippedWeapon.Draw(spriteBatch, characterCenter);
            }
        }

        public void DrawWeaponInFront(SpriteBatch spriteBatch, Vector2 characterCenter)
        {
            if (EquippedWeapon == null) return;

            // Draw slightly in front of the character
            float baseDepth = _character.Sprite?.LayerDepth ?? 0.5f;
            EquippedWeapon.Sprite.LayerDepth = Math.Clamp(baseDepth + 0.0001f, 0f, 1f);

            EquippedWeapon.Draw(spriteBatch, characterCenter);
        }
    }
}