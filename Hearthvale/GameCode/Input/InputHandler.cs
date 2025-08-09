using Hearthvale.GameCode.Entities.Players;
using Hearthvale.GameCode.Managers;
using Hearthvale.GameCode.UI;
using Hearthvale.GameCode.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary;
using MonoGameLibrary.Input;
using System;
using System.Collections.Generic;

namespace Hearthvale.GameCode.Input
{
    public class InputHandler
    {
        private static InputHandler _instance;
        public static InputHandler Instance => _instance ?? throw new InvalidOperationException("InputHandler not initialized. Call Initialize first.");

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

        // Add this field to track which keys were processed already
        private HashSet<Keys> _processedKeys = new HashSet<Keys>();

        // Add a similar method for gamepad buttons
        private HashSet<Buttons> _processedButtons = new HashSet<Buttons>();

        public InputHandler(
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
                movementSpeed, movePlayerCallback, spawnNpcCallback,
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

        // New method for one-shot key detection
        public bool ProcessKeyPress(Keys key, KeyboardInfo keyboard)
        {
            if (keyboard.IsKeyDown(key) && !_processedKeys.Contains(key))
            {
                _processedKeys.Add(key);
                return true;
            }
            return false;
        }

        // New method for one-shot button detection
        public bool ProcessButtonPress(Buttons button, GamePadInfo gamepad)
        {
            if (gamepad.IsButtonDown(button) && !_processedButtons.Contains(button))
            {
                _processedButtons.Add(button);
                return true;
            }
            return false;
        }

        public void Update(GameTime gameTime)
        {
            KeyboardInfo keyboard = Core.Input.Keyboard;
            GamePadInfo gamePadOne = Core.Input.GamePads[(int)PlayerIndex.One];

            // Reset processed inputs at start of each frame
            _processedKeys.Clear();
            _processedButtons.Clear();
            
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
            if (ProcessKeyPress(Keys.Escape, keyboard))
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
            if (_isDialogOpenCallback?.Invoke() == true && ProcessKeyPress(Keys.Enter, keyboard))
            {
                _closeDialogCallback?.Invoke();
                return true; // Consume input, don't process further
            }

            // Toggle UIDebugGrid
            if (ProcessKeyPress(Keys.F2, keyboard))
            {
                DebugManager.Instance.ShowUIDebugGrid = !DebugManager.Instance.ShowUIDebugGrid;
                return false;
            }

            // Toggle Collision Boxes
            if (ProcessKeyPress(Keys.F3, keyboard))
            {
                DebugManager.Instance.ShowCollisionBoxes = !DebugManager.Instance.ShowCollisionBoxes;
                _toggleDebugModeCallback?.Invoke();
                return false;
            }

            // Toggle Tileset Viewer
            if (ProcessKeyPress(Keys.F4, keyboard))
            {
                DebugManager.Instance.ShowTilesetViewer = !DebugManager.Instance.ShowTilesetViewer;
                return false;
            }

            // Toggle Tile Coordinates Overlay
            if (ProcessKeyPress(Keys.F6, keyboard))
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
            
            // Player Movement - Cardinal directions only
            CardinalDirection? moveDirection = null;
            
            // Priority order: Last key pressed wins
            if (keyboard.IsKeyDown(Keys.W)) moveDirection = CardinalDirection.North;
            if (keyboard.IsKeyDown(Keys.S)) moveDirection = CardinalDirection.South;
            if (keyboard.IsKeyDown(Keys.A)) moveDirection = CardinalDirection.West;
            if (keyboard.IsKeyDown(Keys.D)) moveDirection = CardinalDirection.East;

            // Update the movement vector field based on cardinal direction
            _currentMovementVector = moveDirection.HasValue 
                ? moveDirection.Value.ToVector() * _movementSpeed 
                : Vector2.Zero;

            if (moveDirection.HasValue)
            {
                _movePlayerCallback?.Invoke(_currentMovementVector);
            }
            
            // Camera shake (K)
            if (ProcessKeyPress(Keys.K, keyboard))
            {
                CameraManager.Instance.Camera2D?.Shake(0.5f, 8f);
            }

            // Combat actions - use the new method
            if (ProcessKeyPress(Keys.Space, keyboard))
            {
                System.Diagnostics.Debug.WriteLine("SPACE KEY PROCESSED - MELEE ATTACK");
                _meleeAttackCallback?.Invoke();
            }
            
            if (ProcessKeyPress(Keys.F, keyboard))
            {
                System.Diagnostics.Debug.WriteLine("F KEY PROCESSED - PROJECTILE ATTACK");
                _projectileAttackCallback?.Invoke();
            }

            // Audio controls
            if (ProcessKeyPress(Keys.M, keyboard))
            {
                Core.Audio.ToggleMute();
            }

            if (ProcessKeyPress(Keys.OemPlus, keyboard))
            {
                Core.Audio.SongVolume += 0.1f;
                Core.Audio.SoundEffectVolume += 0.1f;
            }

            if (ProcessKeyPress(Keys.OemMinus, keyboard))
            {
                Core.Audio.SongVolume -= 0.1f;
                Core.Audio.SoundEffectVolume -= 0.1f;
            }

            // Spawning and weapons
            if (ProcessKeyPress(Keys.N, keyboard))
            {
                _spawnNPCCallback?.Invoke();
            }

            if (ProcessKeyPress(Keys.Q, keyboard))
            {
                _rotateWeaponLeftCallback?.Invoke();
            }
            
            if (ProcessKeyPress(Keys.E, keyboard))
            {
                _rotateWeaponRightCallback?.Invoke();
            }

            // Interaction
            if (ProcessKeyPress(Keys.I, keyboard))
            {
                _interactionCallback?.Invoke();
            }
        }

        private void HandleGamePad(GamePadInfo gamePadOne)
        {
            // Player Movement - Convert analog to cardinal directions
            Vector2 stickInput = gamePadOne.LeftThumbStick;
            
            // Only process if the stick is being moved significantly
            if (stickInput.LengthSquared() > 0.2f)
            {
                // Convert to cardinal direction
                CardinalDirection cardinalDirection = stickInput.ToCardinalDirection();
                
                // Use the unit vector for that direction
                _currentMovementVector = cardinalDirection.ToVector() * _movementSpeed;
                _movePlayerCallback?.Invoke(_currentMovementVector);
            }
            else
            {
                _currentMovementVector = Vector2.Zero;
            }
            
            // Combat actions
            if (ProcessButtonPress(Buttons.RightShoulder, gamePadOne))
                _projectileAttackCallback?.Invoke();
            if (ProcessButtonPress(Buttons.X, gamePadOne))
                _meleeAttackCallback?.Invoke();

            // Weapon rotation
            if (ProcessButtonPress(Buttons.LeftShoulder, gamePadOne))
                _rotateWeaponLeftCallback?.Invoke();
            if (ProcessButtonPress(Buttons.RightTrigger, gamePadOne))
                _rotateWeaponRightCallback?.Invoke();

            // Interaction
            if (ProcessButtonPress(Buttons.Y, gamePadOne))
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
            return ProcessKeyPress(Keys.Escape, keyboard);
        }

        public bool WasDebugGridTogglePressed()
        {
            var keyboard = Core.Input.Keyboard;
            return ProcessKeyPress(Keys.F9, keyboard);
        }

        public bool WasDialogAdvancePressed()
        {
            var keyboard = Core.Input.Keyboard;
            return ProcessKeyPress(Keys.Enter, keyboard);
        }
    }
}