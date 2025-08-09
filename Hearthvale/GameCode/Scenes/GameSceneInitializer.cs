using Hearthvale.GameCode.Entities.Players;
using Hearthvale.GameCode.Input;
using Hearthvale.GameCode.Managers;
using Hearthvale.GameCode.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Tiled;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;
using System;
using System.Collections.Generic;

namespace Hearthvale.Scenes
{
    /// <summary>
    /// Handles the initialization of GameScene and its dependencies.
    /// </summary>
    public static class GameSceneInitializer
    {
        /// <summary>
        /// Creates and initializes a new GameScene instance.
        /// </summary>
        /// <param name="atlas">The texture atlas for the game.</param>
        /// <param name="font">The primary font for the game.</param>
        /// <param name="debugFont">The debug font for the game.</param>
        /// <param name="uiSoundEffect">The sound effect for UI interactions.</param>
        /// <param name="movementSpeed">The movement speed for the player.</param>
        /// <returns>A fully initialized GameScene instance.</returns>
        public static GameScene CreateGameScene(
            TextureAtlas atlas,
            SpriteFont font,
            SpriteFont debugFont,
            SoundEffect uiSoundEffect,
            float movementSpeed, // Add this parameter
            Player player,
            Tilemap tilemap,
            NpcManager npcManager,
            List<Rectangle> allObstacles,
            Action rotateWeaponLeftCallback,
            Action rotateWeaponRightCallback,
            Action interactionCallback
        )
        {
            // Initialize the CameraManager with a new Camera2D instance
            var camera = new Camera2D(Core.GraphicsDevice.Viewport) { Zoom = 3.0f };
            CameraManager.Initialize(camera);

            // Initialize CombatEffectsManager
            var combatEffectsManager = new CombatEffectsManager();

            // Initialize InputHandler
            InputHandler.Initialize(
                movementSpeed: movementSpeed,
                movePlayerCallback: movement => player.Move(
                movement,
                new Rectangle(0, 0, tilemap.Columns * (int)tilemap.TileWidth, tilemap.Rows * (int)tilemap.TileHeight),
                player.Sprite.Width,
                player.Sprite.Height,
                npcManager.Npcs,
                allObstacles
            ),
                spawnNpcCallback: () => npcManager.SpawnRandomNpcAroundPlayer(player), // Spawn a random NPC near the player
                projectileAttackCallback: () => player.CombatController.StartProjectileAttack(), // Start a projectile attack
                meleeAttackCallback: () => player.CombatController.StartMeleeAttack(), // Start a melee attack
                rotateWeaponLeftCallback: rotateWeaponLeftCallback, // Rotate the player's weapon to the left
                rotateWeaponRightCallback: rotateWeaponRightCallback, // Rotate the player's weapon to the right
                interactionCallback: interactionCallback, // Handle player interaction with objects or NPCs
                toggleDebugModeCallback: () => DebugManager.Instance.ToggleDebugMode(), // Toggle debug mode
                toggleDebugGridCallback: () => DebugManager.Instance.ShowUIDebugGrid = !DebugManager.Instance.ShowUIDebugGrid, // Toggle the debug grid
                pauseGameCallback: () => GameUIManager.Instance.PauseGame(), // Pause the game
                resumeGameCallback: () => GameUIManager.Instance.ResumeGame(null), // Resume the game
                isPausedCallback: () => GameUIManager.Instance.IsPausePanelVisible, // Check if the game is paused
                closeDialogCallback: () => GameUIManager.Instance.HideDialog(), // Close the dialog
                isDialogOpenCallback: () => GameUIManager.Instance.IsDialogOpen // Check if a dialog is open
            );

            // Initialize TilesetManager
            TilesetManager.Initialize();

            // Initialize GameUIManager
            GameUIManager.Initialize(
                atlas,
                font,
                debugFont,
                () => GameUIManager.Instance.ResumeGame(uiSoundEffect),
                () => GameUIManager.Instance.QuitGame(uiSoundEffect, () => Core.ChangeScene(new TitleScene()))
            );

            // Initialize DebugManager
            var whitePixel = new Texture2D(Core.GraphicsDevice, 1, 1);
            whitePixel.SetData(new[] { Color.White });
            DebugManager.Initialize(whitePixel);

            // Initialize DataManager
            DataManager.Initialize();

            // Initialize ScoreManager
            var scorePosition = new Vector2(Core.GraphicsDevice.Viewport.Width - 150, 25); // Top-right corner
            var scoreOrigin = Vector2.Zero; // No origin offset
            ScoreManager.Initialize(font, scorePosition, scoreOrigin);

            // Return a new GameScene instance with the initialized dependencies
            return new GameScene(
                combatEffectsManager,
                CameraManager.Instance,
                InputHandler.Instance
            );
        }
    }
}