using System.Collections.Generic;
using Hearthvale.GameCode.Entities.Characters;
using Hearthvale.GameCode.Entities.Interfaces;
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

    private Character _currentSpeaker;

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
            if (_currentSpeaker == null || _currentSpeaker.IsDefeated || InputHandler.IsKeyPressed(Keys.Enter))
            {
                _uiManager.HideDialog();
                _currentSpeaker = null;
            }
            return;
        }

        // Player initiates dialog with NPCs
        foreach (var npc in _npcs)
        {
            if (!npc.IsDefeated && npc is IDialog dialogCharacter && Vector2.Distance(_player.Position, npc.Position) < _dialogDistance)
            {
                if (InputHandler.IsKeyPressed(Keys.E) && _currentSpeaker != npc)
                {
                    _uiManager.ShowDialog(dialogCharacter.DialogText);
                    _currentSpeaker = npc;
                    break;
                }
            }
        }

        // NPCs initiate dialog with player (example: if NPC wants to talk)
        foreach (var npc in _npcs)
        {
            if (!npc.IsDefeated && Vector2.Distance(_player.Position, npc.Position) < _dialogDistance)
            {
                // Replace this condition with your own logic for when an NPC wants to talk to the player
                bool npcWantsToTalk = false; // TODO: Set this based on your game logic

                if (npcWantsToTalk && _currentSpeaker != _player)
                {
                    _uiManager.ShowDialog(_player.DialogText);
                    _currentSpeaker = _player;
                    break;
                }
            }
        }
    }
}