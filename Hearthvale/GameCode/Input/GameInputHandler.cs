using Hearthvale.GameCode.Entities;
using Hearthvale.GameCode.Managers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;

namespace Hearthvale.GameCode.Input
{
    public class GameInputHandler
    {
        private readonly Player _player;
        private readonly NpcManager _npcManager;
        private readonly GameUIManager _uiManager;
        private readonly Action PauseGame;
        private readonly Action QuitGame;
        private KeyboardState _previousKeyboard;

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

            //player attack
            // Attack: only trigger on key down event
            if (keyboard.IsKeyDown(Keys.Space) && !_previousKeyboard.IsKeyDown(Keys.Space) && !_player.IsAttacking)
            {
                _player.StartAttack();
            }
            _previousKeyboard = keyboard;

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
