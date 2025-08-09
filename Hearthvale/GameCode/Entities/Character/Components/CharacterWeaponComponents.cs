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

            Vector2 origin = _character.Position + new Vector2(_character.Sprite.Width / 2, _character.Sprite.Height / 2);
            Vector2 direction = CalculateAttackDirection();

            return CalculateWeaponArea(origin, direction);
        }

        private Vector2 CalculateAttackDirection()
        {
            const float visualRotationOffset = MathHelper.PiOver4;
            float totalRotation = EquippedWeapon.Rotation + visualRotationOffset;
            return new Vector2((float)Math.Cos(totalRotation), (float)Math.Sin(totalRotation));
        }

        private Rectangle CalculateWeaponArea(Vector2 origin, Vector2 direction)
        {
            const float handleOffset = 8f;
            const float thickness = 12f;

            float length = EquippedWeapon.Length - handleOffset;
            Vector2 perp = new Vector2(-direction.Y, direction.X) * (thickness / 2);

            Vector2[] points = {
                origin + perp,
                origin - perp,
                origin + direction * length + perp,
                origin + direction * length - perp
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

            return new Rectangle((int)minX, (int)minY, (int)(maxX - minX), (int)(maxY - minY));
        }

        public void DrawWeapon(SpriteBatch spriteBatch, Vector2 characterCenter, bool drawBehind)
        {
            if (EquippedWeapon == null) return;

            if (drawBehind)
            {
                EquippedWeapon.Draw(spriteBatch, characterCenter);
            }
        }

        public void DrawWeaponInFront(SpriteBatch spriteBatch, Vector2 characterCenter)
        {
            EquippedWeapon?.Draw(spriteBatch, characterCenter);
        }
    }
}