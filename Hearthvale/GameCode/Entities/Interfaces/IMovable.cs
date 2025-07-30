using Microsoft.Xna.Framework;

namespace Hearthvale.GameCode.Entities.Interfaces
{
    public interface IMovable
    {
        Vector2 Position { get; }
        void SetPosition(Vector2 position);
    }
}
