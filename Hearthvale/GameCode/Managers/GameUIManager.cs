using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;
using Hearthvale.GameCode.Entities;
using Hearthvale.GameCode.Entities.NPCs;
using Hearthvale.GameCode.Entities.Players;
using Hearthvale.GameCode.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using MonoGameGum;
using MonoGameGum.Forms.Controls;
using MonoGameGum.GueDeriving;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;
using System;
using System.Collections.Generic;

namespace Hearthvale.GameCode.Managers
{
    /// <summary>
    /// Manages all in-game UI, including dialog, pause, health bars, and debug info.
    /// </summary>
    public class GameUIManager
    {
        private readonly TextureAtlas _atlas;
        private readonly SpriteFont _font;
        private readonly SpriteFont _debugFont;
        private readonly Action OnResume;
        private readonly Action OnQuit;

        private Panel _pausePanel;
        private AnimatedButton _resumeButton;
        private Panel _dialogPanel;
        private TextRuntime _dialogText;
        private bool _isDialogOpen = false;
        private Texture2D _whitePixel;
        private Panel _weaponPanel;
        private TextRuntime _weaponLevelText;
        private ColoredRectangleRuntime _weaponXpBar;
        private Panel _equippedWeaponStatusPanel;
        private TextRuntime _equippedWeaponNameText;
        private SpriteRuntime _equippedWeaponSprite;
        private TextRuntime _weaponXpText;

        public bool IsDialogOpen => _isDialogOpen;
        public bool IsPausePanelVisible => _pausePanel?.IsVisible ?? false;
        public Texture2D WhitePixel => _whitePixel;

        /// <summary>
        /// Initializes a new instance of the <see cref="GameUIManager"/> class.
        /// </summary>
        public GameUIManager(TextureAtlas atlas, SpriteFont font, SpriteFont debugFont, Action onResume, Action onQuit)
        {
            _atlas = atlas;
            _font = font;
            _debugFont = debugFont;
            OnResume = onResume;
            OnQuit = onQuit;
            InitializeUI();
        }

        public void InitializeUI()
        {
            GumService.Default.Root.Children.Clear();
            CreateDialogPanel();
            CreatePausePanel();
            CreateModernWeaponUIPanel();
            _whitePixel = new Texture2D(Core.GraphicsDevice, 1, 1);
            _whitePixel.SetData(new[] { Color.White });
        }
        public void PauseGame()
        {
            ShowPausePanel();
        }

        public void ResumeGame(SoundEffect uiSoundEffect)
        {
            Core.Audio.PlaySoundEffect(uiSoundEffect);
            HidePausePanel();
        }

        public void QuitGame(SoundEffect uiSoundEffect, Action changeSceneCallback)
        {
            Core.Audio.PlaySoundEffect(uiSoundEffect);
            changeSceneCallback?.Invoke();
        }
        public void DrawPlayerHealthBar(SpriteBatch spriteBatch, Player player, Vector2 position, Vector2 size)
        {
            int health = player.CurrentHealth;
            int maxHealth = player.MaxHealth;
            float percent = (float)health / maxHealth;

            // Rounded background
            int radius = 6;
            DrawRoundedRect(spriteBatch, position, size, Color.FromNonPremultiplied(60, 20, 20, 220), radius);

            // Foreground (gradient green)
            var fgColor = Color.Lerp(Color.Red, Color.LimeGreen, percent);
            DrawRoundedRect(spriteBatch, position, new Vector2(size.X * percent, size.Y), fgColor, radius);

            // Border
            DrawRoundedRect(spriteBatch, position, size, Color.White * 0.2f, radius, outline: true);

            // Health text (smaller)
            var healthText = $"{health}/{maxHealth}";
            float healthFontScale = 0.6f; // 60% of normal font size
            var textSize = _font.MeasureString(healthText) * healthFontScale;
            var textPos = position + new Vector2(size.X / 2 - textSize.X / 2, size.Y / 2 - textSize.Y / 2);
            spriteBatch.DrawString(_font, healthText, textPos, Color.White, 0f, Vector2.Zero, healthFontScale, SpriteEffects.None, 0f);
        }
        // Helper for rounded rectangles (simple approximation)
        private void DrawRoundedRect(SpriteBatch spriteBatch, Vector2 pos, Vector2 size, Color color, int radius, bool outline = false)
        {
            // Center rectangle
            spriteBatch.Draw(_whitePixel, new Rectangle((int)pos.X + radius, (int)pos.Y, (int)size.X - 2 * radius, (int)size.Y), color);
            // Left and right rectangles
            spriteBatch.Draw(_whitePixel, new Rectangle((int)pos.X, (int)pos.Y + radius, radius, (int)size.Y - 2 * radius), color);
            spriteBatch.Draw(_whitePixel, new Rectangle((int)(pos.X + size.X - radius), (int)pos.Y + radius, radius, (int)size.Y - 2 * radius), color);
            // Four corner circles (approximate with small squares)
            spriteBatch.Draw(_whitePixel, new Rectangle((int)pos.X, (int)pos.Y, radius, radius), color);
            spriteBatch.Draw(_whitePixel, new Rectangle((int)(pos.X + size.X - radius), (int)pos.Y, radius, radius), color);
            spriteBatch.Draw(_whitePixel, new Rectangle((int)pos.X, (int)(pos.Y + size.Y - radius), radius, radius), color);
            spriteBatch.Draw(_whitePixel, new Rectangle((int)(pos.X + size.X - radius), (int)(pos.Y + size.Y - radius), radius, radius), color);

            // Optional outline
            if (outline)
            {
                var outlineColor = Color.White * 0.4f;
                DrawRect(spriteBatch, new Rectangle((int)pos.X, (int)pos.Y, (int)size.X, (int)size.Y), _whitePixel, outlineColor);
            }
        }
        public void DrawNpcHealthBar(SpriteBatch spriteBatch, NPC npc, Vector2 offset, Vector2 size)
        {
            int health = npc.Health;
            int maxHealth = npc.MaxHealth > 0 ? npc.MaxHealth : 1; // Use MaxHealth, not Health
            if (npc.IsDefeated) return; // Don't draw for defeated NPCs

            float percent = (float)health / maxHealth;
            Vector2 barPos = npc.Position + offset;

            // Background
            spriteBatch.Draw(_whitePixel, new Rectangle((int)barPos.X, (int)barPos.Y, (int)size.X, (int)size.Y), Color.DarkRed);
            // Foreground (green portion)
            spriteBatch.Draw(_whitePixel, new Rectangle((int)barPos.X, (int)barPos.Y, (int)(size.X * percent), (int)size.Y), Color.LimeGreen);
        }

        private void CreatePausePanel()
        {
            _pausePanel = new Panel();
            _pausePanel.Anchor(Anchor.Center);
            _pausePanel.Visual.WidthUnits = DimensionUnitType.Absolute;
            _pausePanel.Visual.HeightUnits = DimensionUnitType.Absolute;
            _pausePanel.Visual.Height = 70;
            _pausePanel.Visual.Width = 264;
            _pausePanel.IsVisible = false;
            _pausePanel.AddToRoot();

            TextureRegion backgroundRegion = _atlas.GetRegion("panel-background");

            NineSliceRuntime background = new NineSliceRuntime();
            background.Dock(Dock.Fill);
            background.Texture = backgroundRegion.Texture;
            background.TextureAddress = TextureAddress.Custom;
            background.TextureHeight = backgroundRegion.Height;
            background.TextureLeft = backgroundRegion.SourceRectangle.Left;
            background.TextureTop = backgroundRegion.SourceRectangle.Top;
            background.TextureWidth = backgroundRegion.Width;
            _pausePanel.AddChild(background);

            var textInstance = new TextRuntime();
            textInstance.Text = "PAUSED";
            textInstance.CustomFontFile = @"fonts/04b_30.fnt";
            textInstance.UseCustomFont = true;
            textInstance.FontScale = 0.5f;
            textInstance.X = 10f;
            textInstance.Y = 10f;
            _pausePanel.AddChild(textInstance);

            _resumeButton = new AnimatedButton(_atlas);
            _resumeButton.Text = "RESUME";
            _resumeButton.Anchor(Anchor.BottomLeft);
            _resumeButton.Visual.X = 9f;
            _resumeButton.Visual.Y = -9f;
            _resumeButton.Visual.Width = 80;
            _resumeButton.Click += (s, e) => OnResume?.Invoke();
            _pausePanel.AddChild(_resumeButton);

            AnimatedButton quitButton = new AnimatedButton(_atlas);
            quitButton.Text = "QUIT";
            quitButton.Anchor(Anchor.BottomRight);
            quitButton.Visual.X = -9f;
            quitButton.Visual.Y = -9f;
            quitButton.Width = 80;
            quitButton.Click += (s, e) => OnQuit?.Invoke();
            _pausePanel.AddChild(quitButton);
        }
        // New unified, modern weapon UI panel
        private void CreateModernWeaponUIPanel()
        {
            var panel = new Panel();
            panel.Anchor(Anchor.BottomLeft);
            panel.Visual.WidthUnits = DimensionUnitType.Absolute;
            panel.Visual.Width = 160;
            panel.Visual.HeightUnits = DimensionUnitType.Absolute;
            panel.Visual.Height = 48; // Slightly taller for XP text
            panel.Visual.X = 10;
            panel.Visual.Y = -10;
            panel.IsVisible = true;
            panel.AddToRoot();

            var background = new NineSliceRuntime();
            var region = _atlas.GetRegion("panel-background");
            background.Dock(Dock.Fill);
            background.Texture = region.Texture;
            background.TextureAddress = TextureAddress.Custom;
            background.TextureHeight = region.Height;
            background.TextureLeft = region.SourceRectangle.Left;
            background.TextureTop = region.SourceRectangle.Top;
            background.TextureWidth = region.Width;
            panel.AddChild(background);

            _equippedWeaponSprite = new SpriteRuntime
            {
                X = 8,
                Y = 8,
                Width = 24,
                Height = 24,
                TextureAddress = TextureAddress.Custom
            };
            panel.AddChild(_equippedWeaponSprite);

            _equippedWeaponNameText = new TextRuntime
            {
                Text = "Weapon: None",
                FontScale = 0.22f,
                X = 38,
                Y = 8,
                Color = new Color(220, 220, 255)
            };
            panel.AddChild(_equippedWeaponNameText);

            _weaponLevelText = new TextRuntime
            {
                Text = "Lvl: --",
                FontScale = 0.18f,
                X = 38,
                Y = 22,
                Color = Color.Gold
            };
            panel.AddChild(_weaponLevelText);

            // XP Bar background
            var xpBarBackground = new ColoredRectangleRuntime
            {
                Width = 80,
                Height = 5,
                X = 38,
                Y = 32,
                Color = new Color(60, 60, 80)
            };
            panel.AddChild(xpBarBackground);

            // XP Bar foreground
            _weaponXpBar = new ColoredRectangleRuntime
            {
                Width = 0,
                Height = 5,
                X = 38,
                Y = 32,
                Color = Color.Gold
            };
            panel.AddChild(_weaponXpBar);

            // XP text
            _weaponXpText = new TextRuntime
            {
                Text = "XP: 0/0",
                FontScale = 0.16f,
                X = 38,
                Y = 38,
                Color = new Color(180, 180, 220)
            };
            panel.AddChild(_weaponXpText);

            _weaponPanel = panel;
            _equippedWeaponStatusPanel = panel;
        }
        public void UpdateWeaponUI(Weapon equippedWeapon)
        {
            if (equippedWeapon != null)
            {
                
                if (_weaponPanel != null && _weaponPanel.IsVisible)
                {
                    _weaponLevelText.Text = $"Lvl: {equippedWeapon.Level}";
                    float xpPercent = equippedWeapon.XpToNextLevel > 0 ? (float)equippedWeapon.XP / equippedWeapon.XpToNextLevel : 0;
                    _weaponXpBar.Width = 50 * xpPercent;
                    _weaponXpText.Text = $"XP: {equippedWeapon.XP}/{equippedWeapon.XpToNextLevel}";
                }

                if (_equippedWeaponStatusPanel != null && _equippedWeaponStatusPanel.IsVisible)
                {
                    _equippedWeaponNameText.Text = $"Weapon: {equippedWeapon.Name}";
                    if (equippedWeapon.Sprite?.Region?.Texture != null)
                    {
                        var region = equippedWeapon.Sprite.Region;
                        _equippedWeaponSprite.Texture = region.Texture;
                        _equippedWeaponSprite.TextureLeft = region.SourceRectangle.Left;
                        _equippedWeaponSprite.TextureTop = region.SourceRectangle.Top;
                        _equippedWeaponSprite.TextureWidth = region.SourceRectangle.Width;
                        _equippedWeaponSprite.TextureHeight = region.SourceRectangle.Height;
                    }
                    else
                    {
                       
                        _equippedWeaponSprite.Texture = null;
                    }
                }
            }
            else
            {
               
                if (_weaponPanel != null && _weaponPanel.IsVisible)
                {
                    _weaponLevelText.Text = "Lvl: --";
                    _weaponXpBar.Width = 0;
                    _weaponXpText.Text = "XP: 0/0";
                }
                if (_equippedWeaponStatusPanel != null && _equippedWeaponStatusPanel.IsVisible)
                {
                    _equippedWeaponNameText.Text = "Weapon: None";
                    _equippedWeaponSprite.Texture = null;
                }
            }
        }
        private void CreateDialogPanel()
        {
            _dialogPanel = new Panel();
            _dialogPanel.Anchor(Anchor.BottomRight);
            _dialogPanel.Visual.WidthUnits = DimensionUnitType.PercentageOfParent;
            _dialogPanel.Visual.Width = 80;
            _dialogPanel.Visual.HeightUnits = DimensionUnitType.Absolute;
            _dialogPanel.Visual.Height = 60;
            _dialogPanel.IsVisible = false;
            _dialogPanel.AddToRoot();

            _dialogText = new TextRuntime();
            _dialogText.Text = "";
            _dialogText.FontScale = 0.5f;
            _dialogText.X = 10f;
            _dialogText.Y = 10f;
            _dialogPanel.AddChild(_dialogText);
        }

        public void ShowDialog(string text)
        {
            _dialogText.Text = text;
            _dialogPanel.IsVisible = true;
            _isDialogOpen = true;
        }

        public void HideDialog()
        {
            _dialogPanel.IsVisible = false;
            _isDialogOpen = false;
        }

        public void ShowPausePanel()
        {
            _pausePanel.IsVisible = true;
            _resumeButton.IsFocused = true;
        }

        public void HidePausePanel()
        {
            _pausePanel.IsVisible = false;
        }

        public void DrawCollisionBoxes(SpriteBatch spriteBatch, Player player, IEnumerable<NPC> npcs)
        {
            // Draw player bounds (green)
            DrawRect(spriteBatch, player.Bounds, _whitePixel, Color.LimeGreen * 0.5f);

            // Draw NPC bounds (red)
            foreach (var npc in npcs)
            {
                DrawRect(spriteBatch, npc.Bounds, _whitePixel, Color.Red * 0.5f);
            }
        }

        private void DrawRect(SpriteBatch spriteBatch, Rectangle rect, Texture2D texture, Color color)
        {
            // Top
            spriteBatch.Draw(texture, new Rectangle(rect.X, rect.Y, rect.Width, 1), color);
            // Left
            spriteBatch.Draw(texture, new Rectangle(rect.X, rect.Y, 1, rect.Height), color);
            // Right
            spriteBatch.Draw(texture, new Rectangle(rect.Right - 1, rect.Y, 1, rect.Height), color);
            // Bottom
            spriteBatch.Draw(texture, new Rectangle(rect.X, rect.Bottom - 1, rect.Width, 1), color);
        }

        public void DrawDebugInfo(SpriteBatch spriteBatch, GameTime gameTime, Vector2 heroPosition, Vector2 cameraPosition, Viewport viewport)
        {
            // Position debug info at the bottom right
            float margin = 16f;
            var debugLines = new string[]
            {
                $"Player Position: {heroPosition}",
                $"Camera Position: {cameraPosition}",
                $"Viewport Position: {viewport.X} ,{viewport.Y}",
                $"FPS: {1f / (float)gameTime.ElapsedGameTime.TotalSeconds:0.0}"
            };

            // Calculate total height
            float totalHeight = debugLines.Length * _debugFont.LineSpacing;
            Vector2 position = new Vector2(
                viewport.Width - 220, // 220px from right, adjust as needed
                viewport.Height - totalHeight - margin
            );

            foreach (string line in debugLines)
            {
                spriteBatch.DrawString(_debugFont, line, position, Color.Yellow);
                position.Y += _debugFont.LineSpacing;
            }
        }

        public void DrawWallCollisionBoxes(SpriteBatch spriteBatch, Tilemap tilemap, int wallTileId)
        {
            for (int row = 0; row < tilemap.Rows; row++)
            {
                for (int col = 0; col < tilemap.Columns; col++)
                {
                    if (tilemap.GetTileId(col, row) == wallTileId)
                    {
                        var x = (int)(col * tilemap.TileWidth);
                        var y = (int)(row * tilemap.TileHeight);
                        var rect = new Rectangle(x, y, (int)tilemap.TileWidth, (int)tilemap.TileHeight);
                        DrawRect(spriteBatch, rect, _whitePixel, Color.Red * 0.5f);
                    }
                }
            }
        }
        public void DrawDungeonElementCollisionBoxes(SpriteBatch spriteBatch, IEnumerable<IDungeonElement> elements, Matrix viewMatrix)
        {
            foreach (var element in elements)
            {
                var boundsProperty = element.GetType().GetProperty("Bounds");
                if (boundsProperty != null)
                {
                    var bounds = (Rectangle)boundsProperty.GetValue(element);

                    // Transform the top-left corner of the bounds to screen space
                    var topLeft = Vector2.Transform(new Vector2(bounds.X, bounds.Y), viewMatrix);
                    var bottomRight = Vector2.Transform(new Vector2(bounds.Right, bounds.Bottom), viewMatrix);

                    // Calculate the transformed rectangle
                    var screenRect = new Rectangle(
                        (int)topLeft.X,
                        (int)topLeft.Y,
                        (int)(bottomRight.X - topLeft.X),
                        (int)(bottomRight.Y - topLeft.Y)
                    );

                    DrawRect(spriteBatch, screenRect, _whitePixel, Color.Red * 0.5f);
                }
            }
        }
    }
}