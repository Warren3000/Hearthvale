using MonoGameLibrary.Graphics;

namespace Hearthvale.GameCode.Entities.Interfaces
{
    public interface IAnimatable
    {
        AnimatedSprite Sprite { get; }
        void Flash();
    }
}
