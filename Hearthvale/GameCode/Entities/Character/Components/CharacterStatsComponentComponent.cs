using Hearthvale.GameCode.Entities.Stats;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Graphics;
using System;
using System.Collections.Generic;

namespace Hearthvale.GameCode.Entities.Components
{
    public class CharacterStatsComponent
    {
        private readonly Character _character;
        private Dictionary<StatType, int> _baseStats = new();
        private Dictionary<StatType, int> _bonusStats = new();

        // Both players and NPCs need attributes like strength, defense, etc.
        public int GetStat(StatType type)
        {
            return _baseStats.GetValueOrDefault(type, 0) + _bonusStats.GetValueOrDefault(type, 0);
        }
    }
}