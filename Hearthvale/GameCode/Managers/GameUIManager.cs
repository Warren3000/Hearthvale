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
        private static GameUIManager _instance;
        public static GameUIManager Instance => _instance ?? throw new InvalidOperationException("GameUIManager not initialized. Call Initialize first.");

        private readonly TextureAtlas _atlas;
        private readonly SpriteFont _font;
        private readonly SpriteFont _debugFont;
        private readonly Action OnResume;
        private readonly Action OnQuit;

        private Gum.Forms.Controls.Panel _pausePanel;
        private AnimatedButton _resumeButton;
        private AnimatedButton _quitButton;
        private Gum.Forms.Controls.Panel _dialogPanel;
        private TextRuntime _dialogText;
        private bool _isDialogOpen = false;
        private Texture2D _whitePixel;
        private Gum.Forms.Controls.Panel _weaponPanel;
        private TextRuntime _weaponLevelText;
        private ColoredRectangleRuntime _weaponXpBar;
        private Gum.Forms.Controls.Panel _equippedWeaponStatusPanel;
        private TextRuntime _equippedWeaponNameText;
        private TextRuntime _weaponXpText;
        private SpriteRuntime _equippedWeaponSprite;
        public bool IsDialogOpen => _isDialogOpen;
        public bool IsPausePanelVisible => _pausePanel?.IsVisible ?? false;
        public Texture2D WhitePixel => _whitePixel;
        public AnimatedButton ResumeButton => _resumeButton;
        public AnimatedButton QuitButton => _quitButton;

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
        public static void Initialize(TextureAtlas atlas, SpriteFont font, SpriteFont debugFont, Action resumeGameCallback, Action quitGameCallback)
        {
            _instance = new GameUIManager(atlas, font, debugFont, resumeGameCallback, quitGameCallback);
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
            
            // Add safety check
            if (maxHealth <= 0) maxHealth = 100; // Default fallback
            
            float percent = maxHealth > 0 ? (float)health / maxHealth : 0f;

            // Health text with better scaling
            var healthText = $"{health}/{maxHealth}";
            float healthFontScale = 0.6f; // Reduced slightly to fit better
            var textSize = _font.MeasureString(healthText) * healthFontScale;
            
            // Calculate required width based on text size with padding
            float textPadding = 16f; // 8px padding on each side
            float requiredWidth = Math.Max(size.X, textSize.X + textPadding);
            
            // Make the health bar dynamically sized based on text requirements
            var adjustedSize = new Vector2(requiredWidth * 1.2f, size.Y * 1.4f); // 20% wider than required, 40% taller

            // Rounded background
            int radius = 8; // Increased radius for better appearance
            DrawRoundedRect(spriteBatch, position, adjustedSize, Color.FromNonPremultiplied(60, 20, 20, 220), radius);

            // Foreground (gradient green to red based on health)   
            var fgColor = Color.Lerp(Color.Red, Color.LimeGreen, percent);
            DrawRoundedRect(spriteBatch, position, new Vector2(adjustedSize.X * percent, adjustedSize.Y), fgColor, radius);

            // Border with better visibility
            DrawRoundedRect(spriteBatch, position, adjustedSize, Color.White * 0.4f, radius, outline: true);

            // Center the text in the adjusted health bar
            var textPos = position + new Vector2(adjustedSize.X / 2 - textSize.X / 2, adjustedSize.Y / 2 - textSize.Y / 2);

            // Add text shadow for better visibility
            var shadowPos = textPos + new Vector2(1, 1);
            spriteBatch.DrawString(_font, healthText, shadowPos, Color.Black * 0.5f, 0f, Vector2.Zero, healthFontScale, SpriteEffects.None, 0f);
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
            if (npc.IsDefeated) return; // Don't draw for defeated NPCs

            int health = npc.Health;
            int maxHealth = npc.MaxHealth;
            
            // Add safety check
            if (maxHealth <= 0) maxHealth = 1; // Prevent division by zero
            
            float percent = (float)health / maxHealth;
            Vector2 barPos = npc.Position + offset;

            // Background
            spriteBatch.Draw(_whitePixel, new Rectangle((int)barPos.X, (int)barPos.Y, (int)size.X, (int)size.Y), Color.DarkRed);
            // Foreground (green portion)
            spriteBatch.Draw(_whitePixel, new Rectangle((int)barPos.X, (int)barPos.Y, (int)(size.X * percent), (int)size.Y), Color.LimeGreen);
        }

        private void CreatePausePanel()
        {
            _pausePanel = new Gum.Forms.Controls.Panel();
            _pausePanel.Anchor(Anchor.Center);
            _pausePanel.Visual.WidthUnits = DimensionUnitType.Absolute;
            _pausePanel.Visual.HeightUnits = DimensionUnitType.Absolute;
            _pausePanel.Visual.Height = 70;
            _pausePanel.Visual.Width = 264;
            _pausePanel.IsVisible = false;
            _pausePanel.AddToRoot();

            TextureRegion backgroundRegion = _atlas.GetRegion("panel-background");

            NineSliceRuntime background = new NineSliceRuntime();
            background.Name = "PausePanelBackground";
            background.Dock(Dock.Fill);
            background.Texture = backgroundRegion.Texture;
            background.TextureAddress = TextureAddress.Custom;
            background.TextureHeight = backgroundRegion.Height;
            background.TextureLeft = backgroundRegion.SourceRectangle.Left;
            background.TextureTop = backgroundRegion.SourceRectangle.Top;
            background.TextureWidth = backgroundRegion.Width;
            _pausePanel.AddChild(background);

            var textInstance = new TextRuntime();
            textInstance.Tag = "PauseText";
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

            _quitButton = new AnimatedButton(_atlas);
            _quitButton.Text = "QUIT";
            _quitButton.Anchor(Anchor.BottomRight);
            _quitButton.Visual.X = -9f;
            _quitButton.Visual.Y = -9f;
            _quitButton.Width = 80;
            _quitButton.Click += (s, e) => OnQuit?.Invoke();
            _pausePanel.AddChild(_quitButton);
        }
        private void CreateModernWeaponUIPanel()
        {
            var panel = new Gum.Forms.Controls.Panel();
            panel.Name = "WeaponPanel";
            panel.Anchor(Anchor.BottomLeft);
            panel.Visual.WidthUnits = DimensionUnitType.Absolute;
            panel.Visual.Width = 350; // Increased from 280
            panel.Visual.HeightUnits = DimensionUnitType.Absolute;
            panel.Visual.Height = 110; // Increased from 90
            panel.Visual.X = 20; // More margin from edge
            panel.Visual.Y = -20; // More margin from bottom
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
                X = 20, // More margin
                Y = 20,
                Width = 48, // Increased from 40
                Height = 48, // Increased from 40
                TextureAddress = TextureAddress.Custom
            };
            panel.AddChild(_equippedWeaponSprite);

            _equippedWeaponNameText = new TextRuntime
            {
                Tag = "EquippedWeaponNameText",
                Text = "Weapon: None",
                FontScale = 0.45f, // Increased from 0.35f
                X = 80, // Adjusted for larger sprite
                Y = 15,
                Color = new Color(220, 220, 255),
                UseCustomFont = true,
                CustomFontFile = @"fonts/04b_30.fnt"
            };
            panel.AddChild(_equippedWeaponNameText);

            _weaponLevelText = new TextRuntime
            {
                Tag = "WeaponLevelText",
                Text = "Lvl: --",
                FontScale = 0.35f, // Increased from 0.28f
                X = 80, // Adjusted for larger sprite
                Y = 38, // Adjusted spacing
                Color = Color.Gold,
                UseCustomFont = true,
                CustomFontFile = @"fonts/04b_30.fnt"
            };
            panel.AddChild(_weaponLevelText);

            var xpBarBackground = new ColoredRectangleRuntime
            {
                Tag = "XpBarBackground",
                Width = 180, // Increased from 150
                Height = 10, // Increased from 8
                X = 80, // Adjusted for larger sprite
                Y = 60, // Adjusted spacing
                Color = new Color(60, 60, 80)
            };
            panel.AddChild(xpBarBackground);

            _weaponXpBar = new ColoredRectangleRuntime
            {
                Tag = "WeaponXpBar",
                Width = 0,
                Height = 10, // Increased from 8
                X = 80, // Adjusted for larger sprite
                Y = 60, // Adjusted spacing
                Color = Color.Gold
            };
            panel.AddChild(_weaponXpBar);

            _weaponXpText = new TextRuntime
            {
                Tag = "WeaponXpText",
                Text = "XP: 0/0",
                FontScale = 0.30f, // Increased from 0.25f
                X = 80, // Adjusted for larger sprite
                Y = 75, // Adjusted spacing for larger panel
                Color = new Color(180, 180, 220),
                UseCustomFont = true,
                CustomFontFile = @"fonts/04b_30.fnt"
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
                    _weaponXpBar.Width = 180 * xpPercent; // Updated from 150 to match new bar width
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
            _dialogPanel = new Gum.Forms.Controls.Panel();
            _dialogPanel.Anchor(Anchor.BottomRight);
            _dialogPanel.Visual.WidthUnits = DimensionUnitType.PercentageOfParent;
            _dialogPanel.Visual.Width = 80;
            _dialogPanel.Visual.HeightUnits = DimensionUnitType.Absolute;
            _dialogPanel.Visual.Height = 60;
            _dialogPanel.IsVisible = false;
            _dialogPanel.AddToRoot();

            _dialogText = new TextRuntime();
            _dialogText.Tag = "DialogText";
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
            // Only show debug info if debug drawing is enabled
            if (!DebugManager.Instance.DebugDrawEnabled || !DebugManager.Instance.ShowUIOverlay)
                return;

            // Position debug info to avoid overlapping with other UI
            float margin = 20f;
            var debugLines = new string[]
            {
                $"Player: ({heroPosition.X:F0}, {heroPosition.Y:F0})",
                $"Camera: ({cameraPosition.X:F0}, {cameraPosition.Y:F0})", 
                $"FPS: {1f / (float)gameTime.ElapsedGameTime.TotalSeconds:F0}"
            };

            // Calculate total height needed
            float totalHeight = debugLines.Length * _debugFont.LineSpacing * 0.8f; // Smaller scale
            
            // Position in bottom-right, accounting for weapon panel
            Vector2 startPosition = new Vector2(
                viewport.Width - 200, // 200px from right edge
                viewport.Height - totalHeight - margin - 120 // Above weapon panel
            );

            // Draw semi-transparent background
            var bgSize = new Vector2(180, totalHeight + 8);
            var bgRect = new Rectangle((int)(startPosition.X - 4), (int)(startPosition.Y - 4), (int)bgSize.X, (int)bgSize.Y);
            spriteBatch.Draw(_whitePixel, bgRect, Color.Black * 0.4f);

            // Draw debug text
            Vector2 position = startPosition;
            foreach (string line in debugLines)
            {
                // Text shadow
                spriteBatch.DrawString(_debugFont, line, position + Vector2.One, Color.Black * 0.7f, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
                // Main text
                spriteBatch.DrawString(_debugFont, line, position, Color.Yellow, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
                position.Y += _debugFont.LineSpacing * 0.8f;
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
            // Only draw if debug manager allows it
            if (!DebugManager.Instance.DebugDrawEnabled || !DebugManager.Instance.ShowDungeonElements)
                return;

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