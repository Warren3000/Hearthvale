using System.Collections.Generic;
using Hearthvale.GameCode.Entities.Characters;
using Hearthvale.GameCode.Entities.NPCs;
using Hearthvale.GameCode.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Hearthvale.GameCode.Managers;

/// <summary>
/// Manages dialog interactions between the player and NPCs.
/// </summary>
public class DialogManager
{
    private readonly GameUIManager _uiManager;
    private readonly Character _player;
    private readonly IEnumerable<Character> _npcs;
    private readonly float _dialogDistance;

    private Character _currentDialogNpc;

    /// <summary>
    /// Gets a value indicating whether the dialog is open.
    /// </summary>
    public bool IsDialogOpen => _uiManager.IsDialogOpen;

    /// <summary>
    /// Initializes a new instance of the <see cref="DialogManager"/> class.
    /// </summary>
    /// <param name="uiManager">The UI manager.</param>
    /// <param name="player">The player.</param>
    /// <param name="npcs">The NPCs.</param>
    /// <param name="dialogDistance">The dialog interaction distance.</param>
    public DialogManager(GameUIManager uiManager, Character player, IEnumerable<Character> npcs, float dialogDistance)
    {
        _uiManager = uiManager;
        _player = player;
        _npcs = npcs;
        _dialogDistance = dialogDistance;
    }

    /// <summary>
    /// Updates dialog state and handles player-NPC dialog interactions.
    /// </summary>
    public void Update()
    {
        if (_uiManager.IsDialogOpen)
        {
            // If the NPC is defeated while dialog is open, or if Enter is pressed, close it.
            if (_currentDialogNpc == null || _currentDialogNpc.IsDefeated || InputHandler.IsKeyPressed(Keys.Enter))
            {
                _uiManager.HideDialog();
                _currentDialogNpc = null;
            }
            return;
        }

        foreach (var npc in _npcs)
        {
            // Only interact with NPCs that are not defeated and have dialog
            if (!npc.IsDefeated && npc is INpcDialog dialogNpc && Vector2.Distance(_player.Position, npc.Position) < _dialogDistance)
            {
                if (InputHandler.IsKeyPressed(Keys.E) && _currentDialogNpc != npc)
                {
                    _uiManager.ShowDialog(dialogNpc.DialogText);
                    _currentDialogNpc = npc;
                    break;
                }
            }
        }
    }
}