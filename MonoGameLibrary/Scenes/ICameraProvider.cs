using Microsoft.Xna.Framework;

namespace MonoGameLibrary.Scenes
{
    public interface ICameraProvider
    {
        Matrix GetViewMatrix();
    }
}