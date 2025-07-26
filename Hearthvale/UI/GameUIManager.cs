using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameGum;
using MonoGameGum.Forms.Controls;
using MonoGameGum.GueDeriving;
using MonoGameLibrary.Graphics;
using System;

namespace Hearthvale.UI
{
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

        public bool IsDialogOpen => _isDialogOpen;
        public bool IsPausePanelVisible => _pausePanel?.IsVisible ?? false;

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

        public void DrawDebugInfo(SpriteBatch spriteBatch, GameTime gameTime, Vector2 heroPosition, Vector2 cameraPosition, Viewport viewport)
        {
            Vector2 position = new Vector2(20, 40);
            float fps = 1f / (float)gameTime.ElapsedGameTime.TotalSeconds;

            string[] debugLines = new string[]
            {
                $"Player Position: {heroPosition}",
                $"Camera Position: {cameraPosition}",
                $"Viewport Position: {viewport.X} ,{viewport.Y}",
                $"FPS: {fps:0.0}"
            };

            foreach (string line in debugLines)
            {
                spriteBatch.DrawString(_debugFont, line, position, Color.Yellow);
                position.Y += _debugFont.LineSpacing;
            }
        }
    }
}