using Hearthvale.GameCode.Entities.NPCs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using MonoGameLibrary.Graphics;
using System.Collections.Generic;

public class Enemy : NPC
{
    // Add enemy-specific properties or methods here
    public bool IsBoss { get; set; }
    public void TriggerSpecialAttack() { /* ... */ }

    // Constructor matching base NPC constructor
    public Enemy(
        string name,
        Dictionary<string, Animation> animations,
        Vector2 position,
        Rectangle bounds,
        SoundEffect soundEffect,
        int maxHealth)
        : base(name, animations, position, bounds, soundEffect, maxHealth)
    {
    }

    // Override NPC methods if needed
}
