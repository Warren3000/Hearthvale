using Hearthvale.GameCode.Entities.StatusEffects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hearthvale.GameCode.Entities.Interfaces
{
    public interface IStatusEffectHandler
    {
        void ApplyEffect(StatusEffect effect);
        bool HasActiveEffect(StatusEffectType type);
    }
}
