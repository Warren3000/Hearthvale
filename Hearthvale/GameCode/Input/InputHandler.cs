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
        private float _movementSpeed;
        private readonly Action PauseGameCallback;
        private readonly Action<Vector2> MoveHeroCallback;
        private readonly Action SpawnNPCCallback;
        private readonly Action QuitCallback;
        private readonly Action FireProjectileCallback; // <-- Added

        public InputHandler(
            Camera2D camera,
            float movementSpeed,
            Action pauseGameCallback,
            Action<Vector2> moveHeroCallback,
            Action spawnNpcCallback,
            Action quitCallback,
            Action fireProjectileCallback // <-- Added
        )
        {
            _camera = camera;
            _movementSpeed = movementSpeed;
            PauseGameCallback = pauseGameCallback;
            MoveHeroCallback = moveHeroCallback;
            SpawnNPCCallback = spawnNpcCallback;
            QuitCallback = quitCallback;
            FireProjectileCallback = fireProjectileCallback; // <-- Added
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

            float speed = _movementSpeed / 2;
            if (keyboard.IsKeyDown(Keys.Space))
            {
                speed *= 1.5f;
            }

            Vector2 movement = Vector2.Zero;
            if (keyboard.IsKeyDown(Keys.W) || keyboard.IsKeyDown(Keys.Up))
                movement.Y -= speed;
            if (keyboard.IsKeyDown(Keys.S) || keyboard.IsKeyDown(Keys.Down))
                movement.Y += speed;
            if (keyboard.IsKeyDown(Keys.A) || keyboard.IsKeyDown(Keys.Left))
                movement.X -= speed;
            if (keyboard.IsKeyDown(Keys.D) || keyboard.IsKeyDown(Keys.Right))
                movement.X += speed;

            if (movement != Vector2.Zero)
                MoveHeroCallback?.Invoke(movement);

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
                FireProjectileCallback?.Invoke();
            }
        }

        private void HandleGamePad(GamePadInfo gamePadOne)
        {
            if (gamePadOne.WasButtonJustPressed(Buttons.Start))
            {
                PauseGameCallback?.Invoke();
            }

            float speed = _movementSpeed;
            if (gamePadOne.IsButtonDown(Buttons.A))
            {
                speed *= 1.5f;
                GamePad.SetVibration(PlayerIndex.One, 1f, 1f);
            }
            else
            {
                GamePad.SetVibration(PlayerIndex.One, 0f, 0f);
            }

            Vector2 movement = Vector2.Zero;

            if (gamePadOne.LeftThumbStick != Vector2.Zero)
            {
                movement = new Vector2(gamePadOne.LeftThumbStick.X * speed, -gamePadOne.LeftThumbStick.Y * speed);
            }
            else
            {
                if (gamePadOne.IsButtonDown(Buttons.DPadUp))
                    movement.Y -= speed;
                if (gamePadOne.IsButtonDown(Buttons.DPadDown))
                    movement.Y += speed;
                if (gamePadOne.IsButtonDown(Buttons.DPadLeft))
                    movement.X -= speed;
                if (gamePadOne.IsButtonDown(Buttons.DPadRight))
                    movement.X += speed;
            }

            if (movement != Vector2.Zero)
            {
                MoveHeroCallback?.Invoke(movement);
            }

            // Optionally, add gamepad support for firing projectiles here
            // if (gamePadOne.WasButtonJustPressed(Buttons.RightShoulder))
            //     FireProjectileCallback?.Invoke();
        }
    }
}