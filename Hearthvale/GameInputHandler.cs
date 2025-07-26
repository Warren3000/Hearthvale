using Hearthvale.Scenes;
using Hearthvale.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;

namespace Hearthvale
{
    public class GameInputHandler
    {
        private readonly Player _player;
        private readonly NpcManager _npcManager;
        private readonly GameUIManager _uiManager;
        private readonly Action PauseGame;
        private readonly Action QuitGame;

        public GameInputHandler(Player player, NpcManager npcManager, GameUIManager uiManager, Action pauseGame, Action quitGame)
        {
            _player = player;
            _npcManager = npcManager;
            _uiManager = uiManager;
            PauseGame = pauseGame;
            QuitGame = quitGame;
        }

        public void Update(GameTime gameTime)
        {
            KeyboardState keyboard = Keyboard.GetState();

            // Pause
            if (keyboard.IsKeyDown(Keys.Escape))
                PauseGame?.Invoke();

            // Quit (optional, e.g. F10)
            if (keyboard.IsKeyDown(Keys.F10))
                QuitGame?.Invoke();

            // Player attack
            _player.IsAttacking = keyboard.IsKeyDown(Keys.Space);

            // Interact with NPCs
            if (keyboard.IsKeyDown(Keys.E))
            {
                foreach (var npc in _npcManager.Npcs)
                {
                    if (Vector2.Distance(_player.Position, npc.Position) < 40f)
                    {
                        _uiManager.ShowDialog($"Hello, I am {npc.GetType().Name}!");
                        break;
                    }
                }
            }
        }
    }
}
