using Hearthvale.GameCode.Input;
using Hearthvale.GameCode.UI;
using Microsoft.Xna.Framework;
using System;

namespace Hearthvale.GameCode.Managers
{
    /// <summary>
    /// Handles the initialization of input-related singletons.
    /// </summary>
    public static class InputManagerInitializer
    {
        public static void InitializeForTitleScreen()
        {
            InputHandler.Initialize(
                movementSpeed: 0f,
                movePlayerCallback: v => { },
                spawnNpcCallback: () => { },
                projectileAttackCallback: () => { },
                meleeAttackCallback: () => { },
                rotateWeaponLeftCallback: () => { },
                rotateWeaponRightCallback: () => { },
                interactionCallback: () => { }
            );
        }

        public static void InitializeForGameScene(
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
            InputHandler.Initialize(
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
                () => GameUIManager.Instance.ResumeGame(null),
                () => GameUIManager.Instance.IsPausePanelVisible,
                () => GameUIManager.Instance.HideDialog(),
                () => GameUIManager.Instance.IsDialogOpen
            );
        }
    }
}