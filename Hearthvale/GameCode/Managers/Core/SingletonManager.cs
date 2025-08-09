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

namespace Hearthvale.GameCode.Managers
{
    /// <summary>
    /// Centralized manager for initializing all singleton instances in the correct order.
    /// </summary>
    public static class SingletonManager
    {
        private static bool _isInitialized = false;
        private static CombatEffectsManager _combatEffectsManager;
        private static CameraManager _cameraManager;
        private static InputHandler _inputHandler;

        /// <summary>
        /// Initializes all singleton managers required for the title screen.
        /// </summary>
        public static void InitializeForTitleScreen()
        {
            if (_isInitialized)
                return;

            // Delegate initialization to specialized initializers
            UIManagerInitializer.InitializeForTitleScreen();
            InputManagerInitializer.InitializeForTitleScreen();

            _isInitialized = true;
        }

        /// <summary>
        /// Initializes all singleton managers required for the game scene.
        /// </summary>
        public static void InitializeForGameScene(
            float movementSpeed,
            Action<Vector2> movePlayerCallback,
            Action spawnNpcCallback,
            Action projectileAttackCallback,
            Action meleeAttackCallback,
            Action rotateWeaponLeftCallback,
            Action rotateWeaponRightCallback,
            Action interactionCallback
        )
        {
            _combatEffectsManager = new CombatEffectsManager();

            InputHandler.Initialize(
                movementSpeed,
                movePlayerCallback,
                spawnNpcCallback,
                projectileAttackCallback,
                meleeAttackCallback,
                rotateWeaponLeftCallback,
                rotateWeaponRightCallback,
                interactionCallback
            );
        }

        /// <summary>
        /// Resets the initialization state. Call this when changing scenes to allow re-initialization.
        /// </summary>
        public static void Reset()
        {
            _isInitialized = false;

            // Reset individual singletons that need it
            InputHandler.Reset();
        }
        public static CombatEffectsManager GetCombatEffectsManager()
        {
            return _combatEffectsManager;
        }
        /// <summary>
        /// Prepares for scene transition by resetting only what's needed.
        /// </summary>
        public static void PrepareForSceneTransition()
        {
            _isInitialized = false;
            InputHandler.Reset();
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

                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        public static GameScene CreateGameScene()
        {
            return new GameScene(SingletonManager.GetCombatEffectsManager(), CameraManager.Instance, InputHandler.Instance);
        }
    }
}