using Hearthvale.GameCode.Entities.Players;
using Hearthvale.GameCode.Managers;
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
        private readonly Action _spawnNPCCallback;
        private readonly Action _projectileAttackCallback;
        private readonly Action _meleeAttackCallback;
        private readonly Action _rotateWeaponLeftCallback;
        private readonly Action _rotateWeaponRightCallback;
        private readonly Action _interactionCallback;

        // Add new callbacks for debug and UI functions
        private readonly Action _toggleDebugModeCallback;
        private readonly Action _toggleDebugGridCallback;
        private readonly Action _pauseGameCallback;
        private readonly Action _resumeGameCallback;
        private readonly Func<bool> _isPausedCallback;
        private readonly Action _closeDialogCallback;
        private readonly Func<bool> _isDialogOpenCallback;

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
            Action interactionCallback,
            Action toggleDebugModeCallback,
            Action toggleDebugGridCallback,
            Action pauseGameCallback,
            Action resumeGameCallback,
            Func<bool> isPausedCallback,
            Action closeDialogCallback,
            Func<bool> isDialogOpenCallback)
        {
            _camera = camera;
            _movementSpeed = movementSpeed;
            _movePlayerCallback = movePlayerCallback;
            _spawnNPCCallback = spawnNpcCallback;
            _projectileAttackCallback = projectileAttackCallback;
            _meleeAttackCallback = meleeAttackCallback;
            _rotateWeaponLeftCallback = rotateWeaponLeftCallback;
            _rotateWeaponRightCallback = rotateWeaponRightCallback;
            _interactionCallback = interactionCallback;
            _toggleDebugModeCallback = toggleDebugModeCallback;
            _toggleDebugGridCallback = toggleDebugGridCallback;
            _pauseGameCallback = pauseGameCallback;
            _resumeGameCallback = resumeGameCallback;
            _isPausedCallback = isPausedCallback;
            _closeDialogCallback = closeDialogCallback;
            _isDialogOpenCallback = isDialogOpenCallback;
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
            Action interactionCallback,
            Action toggleDebugModeCallback = null,
            Action toggleDebugGridCallback = null,
            Action pauseGameCallback = null,
            Action resumeGameCallback = null,
            Func<bool> isPausedCallback = null,
            Action closeDialogCallback = null,
            Func<bool> isDialogOpenCallback = null)
        {
            _instance = new InputHandler(
                camera, movementSpeed, movePlayerCallback, spawnNpcCallback,
                projectileAttackCallback, meleeAttackCallback,
                rotateWeaponLeftCallback, rotateWeaponRightCallback, interactionCallback,
                toggleDebugModeCallback, toggleDebugGridCallback, pauseGameCallback,
                resumeGameCallback, isPausedCallback, closeDialogCallback, isDialogOpenCallback);
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

            // Handle UI/Debug input first (highest priority)
            if (HandleUIAndDebugInput(keyboard))
                return; // Early return if UI consumed the input

            // Handle game input only if not paused
            if (_isPausedCallback?.Invoke() == false)
            {
                HandleKeyboard(keyboard);
                HandleGamePad(gamePadOne);
            }
        }

        /// <summary>
        /// Handles UI and debug input. Returns true if input was consumed.
        /// </summary>
        private bool HandleUIAndDebugInput(KeyboardInfo keyboard)
        {

            // Debug grid toggle (Escape when not paused, or custom key)
            if (keyboard.WasKeyJustPressed(Keys.Escape))
            {
                if (_isPausedCallback?.Invoke() == true)
                {
                    _resumeGameCallback?.Invoke();
                }
                else
                {
                    _pauseGameCallback?.Invoke();
                }
                return false;
            }

            // Dialog handling
            if (_isDialogOpenCallback?.Invoke() == true && keyboard.WasKeyJustPressed(Keys.Enter))
            {
                _closeDialogCallback?.Invoke();
                return true; // Consume input, don't process further
            }

            // Toggle UIDebugGrid
            if (keyboard.WasKeyJustPressed(Keys.F2))
            {
                DebugManager.Instance.ShowUIDebugGrid = !DebugManager.Instance.ShowUIDebugGrid;
                return false;
            }

            // Toggle Collision Boxes
            if (keyboard.WasKeyJustPressed(Keys.F3))
            {
                DebugManager.Instance.ShowCollisionBoxes = !DebugManager.Instance.ShowCollisionBoxes;
                _toggleDebugModeCallback?.Invoke();
                return false;
            }

            // Toggle Tileset Viewer
            if (keyboard.WasKeyJustPressed(Keys.F4))
            {
                DebugManager.Instance.ShowTilesetViewer = !DebugManager.Instance.ShowTilesetViewer;
                return false;
            }

            // Toggle Tile Coordinates Overlay (F6 - new key that doesn't conflict with existing debug features)
            if (keyboard.WasKeyJustPressed(Keys.F6))
            {
                GameUIManager.Instance.ToggleTileCoordinates();
                return false;
            }

            return false; // Input not consumed
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

            // Camera shake (K)
            if (keyboard.WasKeyJustPressed(Keys.K))
            {
                _camera?.Shake(0.5f, 8f);
            }

            // Combat actions
            if (keyboard.WasKeyJustPressed(Keys.Space))
            {
                _meleeAttackCallback?.Invoke();
            }

            if (keyboard.WasKeyJustPressed(Keys.F))
            {
                _projectileAttackCallback?.Invoke();
            }

            // Audio controls
            if (keyboard.WasKeyJustPressed(Keys.M))
            {
                Core.Audio.ToggleMute();
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

            // Spawning and weapons
            if (keyboard.WasKeyJustPressed(Keys.N))
            {
                _spawnNPCCallback?.Invoke();
            }

            if (keyboard.WasKeyJustPressed(Keys.Q))
            {
                _rotateWeaponLeftCallback?.Invoke();
            }
            if (keyboard.WasKeyJustPressed(Keys.E))
            {
                _rotateWeaponRightCallback?.Invoke();
            }

            // Interaction
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

            // Combat actions
            if (gamePadOne.WasButtonJustPressed(Buttons.RightShoulder))
                _projectileAttackCallback?.Invoke();
            if (gamePadOne.WasButtonJustPressed(Buttons.X))
                _meleeAttackCallback?.Invoke();

            // Weapon rotation
            if (gamePadOne.WasButtonJustPressed(Buttons.LeftShoulder))
                _rotateWeaponLeftCallback?.Invoke();
            if (gamePadOne.WasButtonJustPressed(Buttons.RightTrigger))
                _rotateWeaponRightCallback?.Invoke();

            // Interaction
            if (gamePadOne.WasButtonJustPressed(Buttons.Y))
                _interactionCallback?.Invoke();

            // Pause
            if (gamePadOne.WasButtonJustPressed(Buttons.Start))
            {
                if (_isPausedCallback?.Invoke() == true)
                    _resumeGameCallback?.Invoke();
                else
                    _pauseGameCallback?.Invoke();
            }
        }

        // Keep these methods for backward compatibility
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