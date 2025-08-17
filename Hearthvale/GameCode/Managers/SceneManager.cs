using Hearthvale.GameCode.Utils;
using Microsoft.Xna.Framework;
using MonoGameLibrary;
using MonoGameLibrary.Scenes;

namespace Hearthvale.GameCode.Managers
{
    /// <summary>
    /// Simple scene manager that wraps Core.ChangeScene
    /// </summary>
    public static class SceneManager
    {
        public static void ChangeScene(Scene newScene)
        {
            Log.Info(LogArea.Scene, $"🔄 Changing scene to: {newScene.GetType().Name}");
            Core.ChangeScene(newScene);
        }

        public static void Update(GameTime gameTime)
        {
            // The Core already handles scene updates
        }

        public static void Draw(GameTime gameTime)
        {
            // The Core already handles scene drawing
        }
    }
}