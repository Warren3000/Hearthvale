using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace Hearthvale.GameCode.Entities.Components
{
    public class CharacterAudioComponent
    {
        private readonly Character _character;
        private Dictionary<string, SoundEffect> _soundEffects = new();

        // All characters need sound effects (footsteps, attacks, damage)
        public void PlaySound(string soundName) { /* implementation */ }
    }
}