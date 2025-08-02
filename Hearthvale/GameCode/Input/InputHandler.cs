using Hearthvale.GameCode.Entities.Players; // Add this
using Hearthvale.GameCode.Managers; // Add this
using Hearthvale.GameCode.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary;
using MonoGameLibrary.Input;
using System;

namespace Hearthvale.GameCode.Input
{
    public class InputHandler
    {
        private Camera2D _camera;
        private readonly Action PauseGameCallback;
        private readonly Action<Vector2> _movePlayerCallback;
        private readonly float _movementSpeed;
        private readonly Action SpawnNPCCallback;
        private readonly Action QuitCallback;
        private readonly Action _projectileAttackCallback;
        private readonly Action _meleeAttackCallback;
        private readonly Action _rotateWeaponLeftCallback;
        private readonly Action _rotateWeaponRightCallback;
        private readonly Action _interactionCallback;

        public InputHandler(
            Camera2D camera,
            float movementSpeed,
            Action pauseGameCallback,
            Action<Vector2> movePlayerCallback,
            Action spawnNpcCallback,
            Action quitCallback,
            Action projectileAttackCallback,
            Action meleeAttackCallback,
            Action rotateWeaponLeftCallback,
            Action rotateWeaponRightCallback,
            Action interactionCallback)
        {
            _camera = camera;
            _movementSpeed = movementSpeed;
            PauseGameCallback = pauseGameCallback;
            _movePlayerCallback = movePlayerCallback;
            SpawnNPCCallback = spawnNpcCallback;
            QuitCallback = quitCallback;
            _projectileAttackCallback = projectileAttackCallback;
            _meleeAttackCallback = meleeAttackCallback;
            _rotateWeaponLeftCallback = rotateWeaponLeftCallback;
            _rotateWeaponRightCallback = rotateWeaponRightCallback;
            _interactionCallback = interactionCallback;
        }

        public void Update(GameTime gameTime)
        {
            KeyboardInfo keyboard = Core.Input.Keyboard;
            GamePadInfo gamePadOne = Core.Input.GamePads[(int)PlayerIndex.One];

            // Handle keyboard input
            HandleKeyboard(keyboard);

            // Handle gamepad input
            HandleGamePad(gamePadOne);
        }
        public static bool IsKeyPressed(Keys key)
        {
            return Keyboard.GetState().IsKeyDown(key);
        }

        private void HandleKeyboard(KeyboardInfo keyboard)
        {
            // Player Movement
            Vector2 moveDirection = Vector2.Zero;
            if (keyboard.IsKeyDown(Keys.W)) moveDirection.Y = -1;
            if (keyboard.IsKeyDown(Keys.S)) moveDirection.Y = 1;
            if (keyboard.IsKeyDown(Keys.A)) moveDirection.X = -1;
            if (keyboard.IsKeyDown(Keys.D)) moveDirection.X = 1;

            if (moveDirection != Vector2.Zero)
            {
                moveDirection.Normalize();
                _movePlayerCallback?.Invoke(moveDirection * _movementSpeed);
            }

            if (keyboard.WasKeyJustPressed(Keys.K))
            {
                _camera?.Shake(0.5f, 8f);
            }

            if (keyboard.WasKeyJustPressed(Keys.Enter))
            {
                PauseGameCallback?.Invoke();
            }

            if (keyboard.WasKeyJustPressed(Keys.Escape))
            {
                QuitCallback?.Invoke();
            }

            // Change IsKeyDown to WasKeyJustPressed for a single, clean attack per press.
            if (keyboard.WasKeyJustPressed(Keys.Space))
            {
                _meleeAttackCallback?.Invoke();
            }

            if (keyboard.WasKeyJustPressed(Keys.M))
            {
                Core.Audio.ToggleMute();
            }

            if (keyboard.WasKeyJustPressed(Keys.N))
            {
                SpawnNPCCallback?.Invoke();
            }

            if (keyboard.WasKeyJustPressed(Keys.OemPlus))
            {
                Core.Audio.SongVolume += 0.1f;
                Core.Audio.SoundEffectVolume += 0.1f;
            }

            if (keyboard.WasKeyJustPressed(Keys.OemMinus))
            {
                Core.Audio.SongVolume -= 0.1f;
                Core.Audio.SoundEffectVolume -= 0.1f;
            }

            // --- Add this block for firing projectiles ---
            if (keyboard.WasKeyJustPressed(Keys.F))
            {
                _projectileAttackCallback?.Invoke();
            }

            // --- Add weapon rotation support ---
            if (keyboard.WasKeyJustPressed(Keys.Q))
            {
                _rotateWeaponLeftCallback?.Invoke();
            }
            if (keyboard.WasKeyJustPressed(Keys.E))
            {
                _rotateWeaponRightCallback?.Invoke();
            }

            if (keyboard.WasKeyJustPressed(Keys.I))
            {
                _interactionCallback?.Invoke();
            }
        }

        private void HandleGamePad(GamePadInfo gamePadOne)
        {
            // Player Movement
            Vector2 moveDirection = gamePadOne.LeftThumbStick;
            if (moveDirection != Vector2.Zero)
            {
                _movePlayerCallback?.Invoke(moveDirection * _movementSpeed);
            }

            if (gamePadOne.WasButtonJustPressed(Buttons.Start))
            {
                PauseGameCallback?.Invoke();
            }

            // Optionally, add gamepad support for firing projectiles here
            if (gamePadOne.WasButtonJustPressed(Buttons.RightShoulder))
                 _projectileAttackCallback?.Invoke();
            if (gamePadOne.WasButtonJustPressed(Buttons.X))
                _meleeAttackCallback?.Invoke();
        }
    }
}