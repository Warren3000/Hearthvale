using Microsoft.Xna.Framework;

namespace Hearthvale.GameCode.Entities.Interfaces
{
    public interface IDamageable
    {
        int Health { get; }
        bool IsDefeated { get; }
        void TakeDamage(int amount, Vector2? knockback = null);
    }
}
