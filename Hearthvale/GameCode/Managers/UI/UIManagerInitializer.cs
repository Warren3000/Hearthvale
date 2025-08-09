using Hearthvale.GameCode.UI;
using Hearthvale.Scenes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;

namespace Hearthvale.GameCode.Managers
{
    /// <summary>
    /// Handles the initialization of UI-related singletons.
    /// </summary>
    public static class UIManagerInitializer
    {
        public static void InitializeForTitleScreen()
        {
            DataManager.Initialize(); // Game data is always needed
            var whitePixel = CreateWhitePixel();
            DebugManager.Initialize(whitePixel);
            TilesetManager.Initialize(); // Initialize TilesetManager for wall/floor tilesets
        }

        public static void InitializeForGameScene(
            TextureAtlas atlas,
            SpriteFont font,
            SpriteFont debugFont,
            SoundEffect uiSoundEffect,
            Camera2D camera)
        {
            var scoreTextPosition = new Vector2(0, font.LineSpacing * 1f);
            var scoreTextOrigin = new Vector2(0, font.MeasureString("Score").Y * 0.5f);
            ScoreManager.Initialize(font, scoreTextPosition, scoreTextOrigin);

            GameUIManager.Initialize(atlas, font, debugFont,
                () => GameUIManager.Instance.ResumeGame(uiSoundEffect),
                () => GameUIManager.Instance.QuitGame(uiSoundEffect, () => Core.ChangeScene(new TitleScene()))
            );

            CombatEffectsManager.Initialize();

            var whitePixel = GameUIManager.Instance.WhitePixel; // Use the existing white pixel from GameUIManager
            DebugManager.Initialize(whitePixel);
            DataManager.Initialize();
            CameraManager.Initialize(camera);
        }

        private static Texture2D CreateWhitePixel()
        {
            var whitePixel = new Texture2D(Core.GraphicsDevice, 1, 1);
            whitePixel.SetData(new[] { Color.White });
            return whitePixel;
        }
    }
}