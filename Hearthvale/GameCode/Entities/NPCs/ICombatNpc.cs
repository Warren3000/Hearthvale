using System.Drawing;

namespace Hearthvale.GameCode.Entities.NPCs
{
    /// <summary>
    /// Interface for NPCs that support combat actions.
    /// </summary>
    public interface ICombatNpc
    {
        bool IsAttacking { get; }
        int AttackPower { get; }
        Weapon EquippedWeapon { get; }
        Microsoft.Xna.Framework.Rectangle GetAttackArea();
        bool CheckPlayerHit(Character player);
    }
}
