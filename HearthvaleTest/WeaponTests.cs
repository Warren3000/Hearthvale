using Hearthvale.GameCode.Data;
using Hearthvale.GameCode.Entities;
using Microsoft.Xna.Framework;
using MonoGameLibrary.Graphics;
using System.Collections.Generic;
using Xunit;

namespace HearthvaleTest
{
    public class WeaponTests
    {
        [Fact]
        public void Weapon_Initializes_With_Correct_Values()
        {
            var (weapon, _, _) = CreateTestWeapon();
            Assert.Equal("Sword", weapon.Name);
            Assert.Equal(10, weapon.Damage);
            Assert.NotNull(weapon.Sprite);
            Assert.True(weapon.Length > 0);
        }

        [Fact]
        public void Weapon_Scale_Changes_Length()
        {
            var (weapon, _, _) = CreateTestWeapon();
            float originalLength = weapon.Length;
            weapon.Scale = 2.0f;
            Assert.True(weapon.Length > originalLength);
        }

        [Fact]
        public void Weapon_GainXP_LevelsUp_And_IncreasesDamage()
        {
            var (weapon, _, _) = CreateTestWeapon();
            int originalDamage = weapon.Damage;
            for (int i = 0; i < 10; i++) weapon.GainXP(10); // Should level up at least once
            Assert.True(weapon.Level > 0);
            Assert.True(weapon.Damage > originalDamage);
        }

        [Fact]
        public void Weapon_StartSwing_ChangesState()
        {
            var (weapon, _, _) = CreateTestWeapon();
            weapon.StartSwing(true);
            Assert.True(weapon.Sprite.Animation != null);
        }

        private (Weapon, TextureAtlas, TextureAtlas) CreateTestWeapon()
        {
            var graphicsDevice = new Microsoft.Xna.Framework.Graphics.GraphicsDevice(
                Microsoft.Xna.Framework.Graphics.GraphicsAdapter.DefaultAdapter,
                Microsoft.Xna.Framework.Graphics.GraphicsProfile.Reach,
                new Microsoft.Xna.Framework.Graphics.PresentationParameters()
            );
            var dummyTexture = new Microsoft.Xna.Framework.Graphics.Texture2D(graphicsDevice, 1, 1);
            var region = new TextureRegion(dummyTexture, 0, 0, 1, 1);
            var atlas = new TextureAtlas(dummyTexture);
            atlas.AddRegion("Sword", 0, 0, 1, 1);
            var anim = new Animation(new List<TextureRegion> { region }, System.TimeSpan.FromSeconds(0.1));
            atlas.AddAnimation("Sword", anim);
            atlas.AddAnimation("Swing", anim);
            var projectileAtlas = new TextureAtlas(dummyTexture);
            projectileAtlas.AddRegion("Arrow", 0, 0, 1, 1);
            var projAnim = new Animation(new List<TextureRegion> { region }, System.TimeSpan.FromSeconds(0.1));
            projectileAtlas.AddAnimation("Arrow-Wooden-Attack", projAnim);

            var weaponStats = new WeaponStats { BaseDamage = 10, Scale = 1.0f };
            var weapon = new Weapon("Sword", weaponStats, atlas, projectileAtlas);
            return (weapon, atlas, projectileAtlas);
        }
    }
}
