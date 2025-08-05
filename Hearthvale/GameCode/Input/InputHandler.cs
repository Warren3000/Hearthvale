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
        private static InputHandler _instance;
        public static InputHandler Instance => _instance ?? throw new InvalidOperationException("InputHandler not initialized. Call Initialize first.");

        private Camera2D _camera;
        private readonly Action<Vector2> _movePlayerCallback;
        private readonly float _movementSpeed;
        private readonly Action SpawnNPCCallback;
        private readonly Action _projectileAttackCallback;
        private readonly Action _meleeAttackCallback;
        private readonly Action _rotateWeaponLeftCallback;
        private readonly Action _rotateWeaponRightCallback;
        private readonly Action _interactionCallback;

        // Add this field to store the movement vector
        private Vector2 _currentMovementVector = Vector2.Zero;

        public InputHandler(
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
            _camera = camera;
            _movementSpeed = movementSpeed;
            _movePlayerCallback = movePlayerCallback;
            SpawnNPCCallback = spawnNpcCallback;
            _projectileAttackCallback = projectileAttackCallback;
            _meleeAttackCallback = meleeAttackCallback;
            _rotateWeaponLeftCallback = rotateWeaponLeftCallback;
            _rotateWeaponRightCallback = rotateWeaponRightCallback;
            _interactionCallback = interactionCallback;
        }
        public static void Initialize(
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
            // Allow re-initialization by disposing the old instance
            _instance = new InputHandler(
                camera, movementSpeed, movePlayerCallback, spawnNpcCallback,
                projectileAttackCallback, meleeAttackCallback,
                rotateWeaponLeftCallback, rotateWeaponRightCallback, interactionCallback);
        }

        /// <summary>
        /// Resets the InputHandler singleton to allow re-initialization
        /// </summary>
        public static void Reset()
        {
            _instance = null;
        }

        public Vector2 GetMovement()
        {
            return _currentMovementVector;
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

            // Update the movement vector field
            _currentMovementVector = moveDirection != Vector2.Zero ? Vector2.Normalize(moveDirection) * _movementSpeed : Vector2.Zero;

            if (moveDirection != Vector2.Zero)
            {
                _movePlayerCallback?.Invoke(_currentMovementVector);
            }

            if (keyboard.WasKeyJustPressed(Keys.K))
            {
                _camera?.Shake(0.5f, 8f);
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

            // Update the movement vector field
            _currentMovementVector = moveDirection != Vector2.Zero ? moveDirection * _movementSpeed : Vector2.Zero;

            if (moveDirection != Vector2.Zero)
            {
                _movePlayerCallback?.Invoke(_currentMovementVector);
            }

            if (gamePadOne.WasButtonJustPressed(Buttons.Start))
            {
                // This callback is now handled in GameScene
            }

            // Optionally, add gamepad support for firing projectiles here
            if (gamePadOne.WasButtonJustPressed(Buttons.RightShoulder))
                 _projectileAttackCallback?.Invoke();
            if (gamePadOne.WasButtonJustPressed(Buttons.X))
                _meleeAttackCallback?.Invoke();
        }

        public bool WasPausePressed()
        {
            var keyboard = Core.Input.Keyboard;
            return keyboard.WasKeyJustPressed(Keys.Escape);
        }

        public bool WasDebugGridTogglePressed()
        {
            var keyboard = Core.Input.Keyboard;
            return keyboard.WasKeyJustPressed(Keys.F9);
        }

        public bool WasDialogAdvancePressed()
        {
            var keyboard = Core.Input.Keyboard;
            return keyboard.WasKeyJustPressed(Keys.Enter);
        }
    }
}