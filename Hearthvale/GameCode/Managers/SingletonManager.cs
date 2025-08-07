using Hearthvale.GameCode.Input;
using Hearthvale.GameCode.Managers;
using Hearthvale.GameCode.UI;
using Hearthvale.Scenes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;
using System;
using System.Collections.Generic;

namespace Hearthvale.GameCode.Managers
{
    /// <summary>
    /// Centralized manager for initializing all singleton instances in the correct order.
    /// </summary>
    public static class SingletonManager
    {
        private static bool _isInitialized = false;

        /// <summary>
        /// Initializes all singleton managers required for the title screen.
        /// </summary>
        public static void InitializeForTitleScreen()
        {
            if (_isInitialized)
                return;

            // Only initialize what's needed for title screen
            DataManager.Initialize(); // Game data is always needed
            var whitePixel = CreateWhitePixel();
            DebugManager.Initialize(whitePixel);
            TilesetManager.Initialize(); // Initialize TilesetManager for wall/floor tilesets
            InputHandler.Initialize(
                camera: null,
                movementSpeed: 0f,
                movePlayerCallback: v => { },
                spawnNpcCallback: () => { },
                projectileAttackCallback: () => { },
                meleeAttackCallback: () => { },
                rotateWeaponLeftCallback: () => { },
                rotateWeaponRightCallback: () => { },
                interactionCallback: () => { }
            );

            _isInitialized = true;
        }

        /// <summary>
        /// Initializes all singleton managers required for the game scene.
        /// </summary>
        public static void InitializeForGameScene(
            TextureAtlas atlas,
            SpriteFont font,
            SpriteFont debugFont,
            SoundEffect uiSoundEffect,
            Camera2D camera,
            float movementSpeed,
            Action<Vector2> movePlayerCallback,
            Action spawnNpcCallback,
            Action projectileAttackCallback,
            Action meleeAttackCallback,
            Action rotateWeaponLeftCallback,
            Action rotateWeaponRightCallback,
            Action interactionCallback)
        {
            // Initialize core singletons
            var scoreTextPosition = new Vector2(0, font.LineSpacing * 1f);
            var scoreTextOrigin = new Vector2(0, font.MeasureString("Score").Y * 0.5f);
            ScoreManager.Initialize(font, scoreTextPosition, scoreTextOrigin);

            GameUIManager.Initialize(atlas, font, debugFont,
                () => GameUIManager.Instance.ResumeGame(uiSoundEffect),
                () => GameUIManager.Instance.QuitGame(uiSoundEffect, () => Core.ChangeScene(new TitleScene()))
            );

            CombatEffectsManager.Initialize(camera);

            // Initialize utility singletons
            var whitePixel = GameUIManager.Instance.WhitePixel; // Use the existing white pixel from GameUIManager
            DebugManager.Initialize(whitePixel);
            DataManager.Initialize(); // Initialize DataManager singleton
            CameraManager.Initialize(camera); // Initialize CameraManager singleton

            // Re-initialize InputHandler with game-specific callbacks including UI/Debug
            InputHandler.Initialize(
                camera,
                movementSpeed,
                movePlayerCallback,
                spawnNpcCallback,
                projectileAttackCallback,
                meleeAttackCallback,
                rotateWeaponLeftCallback,
                rotateWeaponRightCallback,
                interactionCallback,
                // Debug and UI callbacks
                () => DebugManager.Instance.ToggleDebugMode(),
                () => DebugManager.Instance.ShowUIDebugGrid = !DebugManager.Instance.ShowUIDebugGrid,
                () => GameUIManager.Instance.PauseGame(),
                () => GameUIManager.Instance.ResumeGame(uiSoundEffect),
                () => GameUIManager.Instance.IsPausePanelVisible,
                () => GameUIManager.Instance.HideDialog(),
                () => GameUIManager.Instance.IsDialogOpen
            );

            _isInitialized = true;
        }

        /// <summary>
        /// Creates a white pixel texture for drawing operations.
        /// </summary>
        private static Texture2D CreateWhitePixel()
        {
            var whitePixel = new Texture2D(Core.GraphicsDevice, 1, 1);
            whitePixel.SetData(new[] { Color.White });
            return whitePixel;
        }

        /// <summary>
        /// Resets the initialization state. Call this when changing scenes to allow re-initialization.
        /// </summary>
        public static void Reset()
        {
            _isInitialized = false;

            // Reset individual singletons that need it
            InputHandler.Reset(); // You'll need to add this method
            // Don't reset ScoreManager, GameUIManager, DataManager, or CameraManager as they should persist
        }

        /// <summary>
        /// Prepares for scene transition by resetting only what's needed
        /// </summary>
        public static void PrepareForSceneTransition()
        {
            _isInitialized = false;
            InputHandler.Reset(); // Only reset InputHandler, keep others
        }

        /// <summary>
        /// Checks if all required singletons are initialized and ready to use.
        /// </summary>
        public static bool AreAllSingletonsReady()
        {
            try
            {
                // Test access to all singletons to ensure they're initialized
                var inputHandler = InputHandler.Instance;
                var debugManager = DebugManager.Instance;
                var dataManager = DataManager.Instance;
                var cameraManager = CameraManager.Instance;

                // Only test these if we're in game scene (they won't exist in title)
                if (_isInitialized)
                {
                    var scoreManager = ScoreManager.Instance;
                    var uiManager = GameUIManager.Instance;
                }

                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }
    }
}