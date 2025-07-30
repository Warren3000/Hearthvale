using Hearthvale.GameCode.Entities.NPCs;
using Hearthvale.GameCode.Entities.Players;
using Hearthvale.GameCode.Input;
using Hearthvale.GameCode.Managers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Hearthvale.GameCode.Managers;
public class DialogManager
{
    private readonly GameUIManager _uiManager;
    private readonly Player _player;
    private readonly NpcManager _npcManager;
    private readonly float _dialogDistance;

    private NPC _currentDialogNpc;

    public bool IsDialogOpen => _uiManager.IsDialogOpen;

    public DialogManager(GameUIManager uiManager, Player player, NpcManager npcManager, float dialogDistance)
    {
        _uiManager = uiManager;
        _player = player;
        _npcManager = npcManager;
        _dialogDistance = dialogDistance;
    }

    public void Update()
    {
        if (_uiManager.IsDialogOpen)
        {
            if (InputHandler.IsKeyPressed(Keys.Enter))
            {
                _uiManager.HideDialog();
                _currentDialogNpc = null;
            }
            return;
        }

        foreach (var npc in _npcManager.Npcs)
        {
            if (!npc.IsDefeated && Vector2.Distance(_player.Position, npc.Position) < _dialogDistance)
            {
                if (InputHandler.IsKeyPressed(Keys.E) && _currentDialogNpc != npc)
                {
                    _uiManager.ShowDialog(npc.DialogText);
                    _currentDialogNpc = npc;
                    break;
                }
            }
        }
    }
}