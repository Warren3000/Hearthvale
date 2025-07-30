using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Hearthvale.GameCode.Entities;
public class Weapon
{
    public string Name { get; }
    public AnimatedSprite Sprite { get; }
    public float Rotation { get; set; } = 0f; // Radians
    public Vector2 Position { get; set; }
    public Vector2 ManualOffset { get; set; } = Vector2.Zero;
    public Vector2 Offset { get; set; } = Vector2.Zero;
    public TextureAtlas _atlas { get; set; }
    public int Level { get; private set; }
    public int XP { get; private set; }
    public int Damage { get; private set; }
    public float Scale
    {
        get => Sprite.Scale.X; // Assuming uniform scaling
        set => Sprite.Scale = new Vector2(value, value);
    }

    public Weapon(string name, int baseDamage, TextureAtlas atlas)
    {
        Name = name;
        _atlas = atlas;
        Damage = baseDamage;
        // For single-frame weapons, create an animation with one frame
        var region = atlas.GetRegion(name);
        var animation = new Animation(
            new List<TextureRegion> { region },
            TimeSpan.FromSeconds(0.2)
        );
        Sprite = new AnimatedSprite(animation);
        Sprite.Origin = new Vector2(0, Sprite.Region.Height);
    }
    public void Update(GameTime gameTime)
    {
        Sprite.Update(gameTime);
    }

    public void Draw(SpriteBatch spriteBatch, Vector2 playerPosition)
    {
        // Center of the player sprite
        Vector2 playerCenter = playerPosition + new Vector2(Sprite.Width / 2f, Sprite.Height / 1.4f);

        // Final weapon position
        Vector2 finalPosition = playerCenter + Offset + ManualOffset;

        Sprite.Position = finalPosition;
        Sprite.Rotation = Rotation;
        Sprite.Draw(spriteBatch, Sprite.Position);
    }
    //public void StartAttack()
    //{
    //    if (_atlas.HasAnimation(Name + "_Attack"))
    //    {
    //        Sprite.Animation = _atlas.GetAnimation(Name + "_Attack");
    //    }
    //}

    public void GainXP(int amount)
    {
        XP += amount;
        if (XP >= XPToNextLevel())
        {
            LevelUp();
        }
    }

    private void LevelUp()
    {
        Level++;
        XP = 0;
        Damage += 2; // Example increment
    }

    private int XPToNextLevel() => Level * 10;
}