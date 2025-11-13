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
        private readonly Action _pauseGameCallback;
        private readonly Action _resumeGameCallback;
        private readonly Func<bool> _isPausedCallback;
        private readonly Action _closeDialogCallback;
        private readonly Func<bool> _isDialogOpenCallback;

        // Add this field to store the movement vector
        private Vector2 _currentMovementVector = Vector2.Zero;

        // Tracks which keys/buttons have already been processed while still held down
        private readonly HashSet<Keys> _processedKeys = new HashSet<Keys>();
        private readonly HashSet<Buttons> _processedButtons = new HashSet<Buttons>();

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

        // One-shot key detection using a per-key processed set
        public bool ProcessKeyPress(Keys key, KeyboardInfo keyboard)
        {
            if (keyboard.IsKeyDown(key) && !_processedKeys.Contains(key))
            {
                _processedKeys.Add(key);
                return true;
            }
            return false;
        }

        // One-shot button detection using a per-button processed set
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

            // IMPORTANT: prune processed sets only when inputs are released,
            // not by clearing every frame
            PruneProcessedKeys(keyboard);
            PruneProcessedButtons(gamePadOne);

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
        /// Remove keys from the processed set when they are released, allowing future presses to trigger again.
        /// </summary>
        private void PruneProcessedKeys(KeyboardInfo keyboard)
        {
            if (_processedKeys.Count == 0) return;

            var toRemove = new List<Keys>();
            foreach (var key in _processedKeys)
            {
                if (keyboard.IsKeyUp(key))
                    toRemove.Add(key);
            }
            for (int i = 0; i < toRemove.Count; i++)
                _processedKeys.Remove(toRemove[i]);
        }

        /// <summary>
        /// Remove buttons from the processed set when they are released, allowing future presses to trigger again.
        /// </summary>
        private void PruneProcessedButtons(GamePadInfo gamepad)
        {
            if (_processedButtons.Count == 0) return;

            var toRemove = new List<Buttons>();
            foreach (var button in _processedButtons)
            {
                if (gamepad.IsButtonUp(button))
                    toRemove.Add(button);
            }
            for (int i = 0; i < toRemove.Count; i++)
                _processedButtons.Remove(toRemove[i]);
        }

        /// <summary>
        /// Handles UI and debug input. Returns true if input was consumed.
        /// </summary>
        private bool HandleUIAndDebugInput(KeyboardInfo keyboard)
        {
            // Pause / Resume (Escape)
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

            // Debug Mode Controls
            if (ProcessKeyPress(Keys.F1, keyboard))
            {
                DebugManager.Instance.ToggleDebugMode();
                return false;
            }

            // Toggle UIDebugGrid
            if (ProcessKeyPress(Keys.F2, keyboard))
            {
                DebugManager.Instance.ShowUIDebugGrid = !DebugManager.Instance.ShowUIDebugGrid;
                return false;
            }

            // Toggle Physics Debug + Dungeon Collision overlay
            if (ProcessKeyPress(Keys.F3, keyboard))
            {
                DebugManager.Instance.TogglePhysicsDebug();
                DebugManager.Instance.ToggleDungeonCollisionDebug(); // NEW: show chest/trap collision shapes
                return false;
            }

            // Toggle Combat Debug
            if (ProcessKeyPress(Keys.F4, keyboard))
            {
                DebugManager.Instance.ToggleCombatDebug();
                return false;
            }

            // Toggle Tileset Viewer
            if (ProcessKeyPress(Keys.F5, keyboard))
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

            // Toggle Collision Bounds Debug
            if (ProcessKeyPress(Keys.F7, keyboard))
            {
                DebugManager.Instance.ShowCollisionBounds = !DebugManager.Instance.ShowCollisionBounds;
                System.Diagnostics.Debug.WriteLine($"F7 Pressed: ShowCollisionBounds = {DebugManager.Instance.ShowCollisionBounds}");
                return false;
            }

            // Advanced Debug Controls (Ctrl + key combinations)
            if (keyboard.IsKeyDown(Keys.LeftControl))
            {
                // Toggle Rendering Debug (Ctrl + R)
                if (ProcessKeyPress(Keys.R, keyboard))
                {
                    DebugManager.Instance.ToggleRenderingDebug();
                    return false;
                }

                // Toggle Sprite Alignment Debug (Ctrl + A)
                if (ProcessKeyPress(Keys.A, keyboard))
                {
                    DebugManager.Instance.ToggleSpriteAlignmentDebug();
                    return false;
                }

                // Toggle Detailed Physics (Ctrl + P)
                if (ProcessKeyPress(Keys.P, keyboard))
                {
                    DebugManager.Instance.ShowDetailedPhysics = !DebugManager.Instance.ShowDetailedPhysics;
                    return false;
                }

                // Toggle Dungeon Elements (Ctrl + D)
                if (ProcessKeyPress(Keys.D, keyboard))
                {
                    DebugManager.Instance.ShowDungeonElements = !DebugManager.Instance.ShowDungeonElements;
                    return false;
                }
            }

            return false; // Input not consumed
        }

        public static bool IsKeyPressed(Keys key)
        {
            return Keyboard.GetState().IsKeyDown(key);
        }

        private void HandleKeyboard(KeyboardInfo keyboard)
        {
            // Player Movement - Allow combined directions for diagonals
            Vector2 moveVector = Vector2.Zero;

            if (keyboard.IsKeyDown(Keys.W)) moveVector.Y -= 1;
            if (keyboard.IsKeyDown(Keys.S)) moveVector.Y += 1;
            if (keyboard.IsKeyDown(Keys.A)) moveVector.X -= 1;
            if (keyboard.IsKeyDown(Keys.D)) moveVector.X += 1;

            // Normalize if moving diagonally to prevent faster diagonal movement
            if (moveVector.LengthSquared() > 1.0f)
                moveVector.Normalize();

            // Scale by movement speed
            _currentMovementVector = moveVector * _movementSpeed;

            if (moveVector != Vector2.Zero)
            {
                _movePlayerCallback?.Invoke(_currentMovementVector);
            }

            // Camera shake (K)
            if (ProcessKeyPress(Keys.K, keyboard))
            {
                CameraManager.Instance.Camera2D?.Shake(0.5f, 8f);
            }

            // Combat actions - use the processed set
            if (ProcessKeyPress(Keys.F, keyboard))
            {
                System.Diagnostics.Debug.WriteLine("F KEY PROCESSED - MELEE ATTACK");
                _meleeAttackCallback?.Invoke();
            }

            if (ProcessKeyPress(Keys.Space, keyboard))
            {
                System.Diagnostics.Debug.WriteLine("SPACE KEY PROCESSED - PROJECTILE ATTACK");
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
            // Player Movement - Use analog stick input directly
            Vector2 stickInput = gamePadOne.LeftThumbStick;

            // Only process if the stick is being moved significantly
            if (stickInput.LengthSquared() > 0.2f)
            {
                // Normalize and scale by movement speed
                if (stickInput.LengthSquared() > 1.0f)
                    stickInput.Normalize();

                _currentMovementVector = stickInput * _movementSpeed;
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

            // Pause (already edge-detected via GamePadInfo)
            if (gamePadOne.WasButtonJustPressed(Buttons.Start))
            {
                if (_isPausedCallback?.Invoke() == true)
                    _resumeGameCallback?.Invoke();
                else
                    _pauseGameCallback?.Invoke();
            }
        }
    }
}