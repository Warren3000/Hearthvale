using Hearthvale.GameCode.Entities.Players;
using Hearthvale.GameCode.Input;
using Hearthvale.GameCode.Managers;
using Hearthvale.GameCode.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;
using MonoGameLibrary.Scenes;
using System;
using System.Collections.Generic;

namespace Hearthvale.Scenes
{
    /// <summary>
    /// Factory class for creating fully initialized GameScene instances.
    /// </summary>
    public static class GameSceneFactory
    {
        private const float MOVEMENT_SPEED = 2.0f;

        /// <summary>
        /// Creates a fully initialized GameScene with all dependencies set up.
        /// </summary>
        /// <returns>A ready-to-use GameScene instance.</returns>
        public static GameScene CreateGameScene()
        {
            // Load required assets
            var atlas = TextureAtlas.FromFile(Core.Content, "images/xml/atlas-definition.xml");
            var font = Core.Content.Load<SpriteFont>("fonts/04B_30");
            var debugFont = Core.Content.Load<SpriteFont>("fonts/DebugFont");
            var uiSoundEffect = Core.Content.Load<SoundEffect>("audio/ui");

            // Initialize the camera
            var camera = new Camera2D(Core.GraphicsDevice.Viewport) { Zoom = 3.0f };

            // Set the camera's initial position to a reasonable default
            camera.Position = new Vector2(1296, 256); // This should be updated when the actual player spawn is known

            CameraManager.Initialize(camera);

            // Initialize CombatEffectsManager
            var combatEffectsManager = new CombatEffectsManager();
            CombatEffectsManager.Initialize();

            // Initialize other required singletons
            TilesetManager.Initialize();

            // Initialize GameUIManager
            GameUIManager.Initialize(
                atlas,
                font,
                debugFont,
                () => GameUIManager.Instance.ResumeGame(uiSoundEffect),
                () => GameUIManager.Instance.QuitGame(uiSoundEffect, () => SceneManager.ChangeScene(new TitleScene()))
            );

            // Initialize DebugManager
            var whitePixel = new Texture2D(Core.GraphicsDevice, 1, 1);
            whitePixel.SetData(new[] { Color.White });
            DebugManager.Initialize(whitePixel);

            // Initialize DataManager
            DataManager.Initialize();

            // Initialize ScoreManager
            var scorePosition = new Vector2(Core.GraphicsDevice.Viewport.Width - 150, 25);
            var scoreOrigin = Vector2.Zero;
            ScoreManager.Initialize(font, scorePosition, scoreOrigin);

            // Initialize InputHandler with placeholder callbacks
            // The actual GameScene will reinitialize this with proper callbacks in its LoadContent method
            InputHandler.Initialize(
                movementSpeed: MOVEMENT_SPEED,
                movePlayerCallback: movement => { /* Will be set by GameScene */ },
                spawnNpcCallback: () => { /* Will be set by GameScene */ },
                projectileAttackCallback: () => { /* Will be set by GameScene */ },
                meleeAttackCallback: () => { /* Will be set by GameScene */ },
                rotateWeaponLeftCallback: () => { /* Will be set by GameScene */ },
                rotateWeaponRightCallback: () => { /* Will be set by GameScene */ },
                interactionCallback: () => { /* Will be set by GameScene */ },
                toggleDebugModeCallback: () => DebugManager.Instance.ToggleDebugMode(),
                toggleDebugGridCallback: () => DebugManager.Instance.ShowUIDebugGrid = !DebugManager.Instance.ShowUIDebugGrid,
                pauseGameCallback: () => GameUIManager.Instance.PauseGame(),
                resumeGameCallback: () => GameUIManager.Instance.ResumeGame(null),
                isPausedCallback: () => GameUIManager.Instance.IsPausePanelVisible,
                closeDialogCallback: () => GameUIManager.Instance.HideDialog(),
                isDialogOpenCallback: () => GameUIManager.Instance.IsDialogOpen
            );

            // Return a new GameScene instance
            return new GameScene(
                combatEffectsManager,
                CameraManager.Instance,
                InputHandler.Instance
            );
        }
    }
}