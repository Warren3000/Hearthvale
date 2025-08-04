using Gum.Managers;
using Hearthvale.GameCode.Managers;
using Hearthvale.GameCode.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameGum;
using MonoGameGum.Forms.Controls;
using MonoGameGum.GueDeriving;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;
using MonoGameLibrary.Scenes;
using System;

namespace Hearthvale.Scenes;

public class TitleScene : Scene
{
    private const string HEARTHVALE_TEXT = "Hearthvale";
    private const string Subtitle = "";
    private const string PRESS_ENTER_TEXT = "Press Enter To Start";

    // The font to use to render normal text.
    private SpriteFont _font;

    // The font used to render the title text.
    private SpriteFont _font5x;

    private SpriteFont _debugFont;

    // The position to draw the dungeon text at.
    private Vector2 _dungeonTextPos;

    // The origin to set for the dungeon text.
    private Vector2 _dungeonTextOrigin;

    // The position to draw the slime text at.
    private Vector2 _slimeTextPos;

    // The origin to set for the slime text.
    private Vector2 _slimeTextOrigin;

    // The position to draw the press enter text at.
    private Vector2 _pressEnterPos;

    // The origin to set for the press enter text when drawing it.
    private Vector2 _pressEnterOrigin;

    // The texture used for the background pattern.
    private Texture2D _backgroundPattern;

    // The destination rectangle for the background pattern to fill.
    private Rectangle _backgroundDestination;

    // The offset to apply when drawing the background pattern so it appears to
    // be scrolling.
    private Vector2 _backgroundOffset;

    // The speed that the background pattern scrolls.
    private float _scrollSpeed = 50.0f;

    // The options button used to open the options menu.
    private AnimatedButton _optionsButton;

    // The back button used to exit the options menu back to the title menu.
    private AnimatedButton _optionsBackButton;

    // Reference to the texture atlas that we can pass to UI elements when they
    // are created.
    private TextureAtlas _atlas;

    private SoundEffect _uiSoundEffect;
    private Gum.Forms.Controls.Panel _titleScreenButtonsPanel;
    private Gum.Forms.Controls.Panel _optionsPanel;

    private DebugManager _debugManager;

    private AnimatedButton _startButton;

    public override void Initialize()
    {
        // LoadContent is called during base.Initialize().
        base.Initialize();

        // While on the title screen, we can enable exit on escape so the player
        // can close the game by pressing the escape key.
        Core.ExitOnEscape = true;

        // Set the position and origin for the Dungeon text.
        Vector2 size = _font5x.MeasureString(HEARTHVALE_TEXT);
        _dungeonTextPos = new Vector2(640, 300);
        _dungeonTextOrigin = size * 0.5f;

        // Set the position and origin for the Slime text.
        //size = _font5x.MeasureString(SLIME_TEXT);
        //_slimeTextPos = new Vector2(757, 207);
        //_slimeTextOrigin = size * 0.5f;

        // Set the position and origin for the press enter text.
        size = _font.MeasureString(PRESS_ENTER_TEXT);
        _pressEnterPos = new Vector2(640, 620);
        _pressEnterOrigin = size * 0.5f;

        // Initialize the offset of the background pattern at zero.
        _backgroundOffset = Vector2.Zero;

        // Set the background pattern destination rectangle to fill the entire
        // screen background.
        _backgroundDestination = Core.GraphicsDevice.PresentationParameters.Bounds;

        InitializeUI();
    }

    public override void LoadContent()
    {
        // Load the font for the standard text.
        _font = Core.Content.Load<SpriteFont>("fonts/04B_30");

        // Load the font for the title text.
        _font5x = Content.Load<SpriteFont>("fonts/04B_30_5x");

        //Load debug font for debug manager.
        _debugFont = Content.Load<SpriteFont>("fonts/DebugFont");

        // Load the background pattern texture.
        _backgroundPattern = Content.Load<Texture2D>("images/background-pattern");

        // Load the sound effect to play when ui actions occur.
        _uiSoundEffect = Core.Content.Load<SoundEffect>("audio/ui");

        // Load the texture atlas from the xml configuration file.
        _atlas = TextureAtlas.FromFile(Core.Content, "images/atlas-definition.xml");

        // Create a 1x1 white pixel texture for the grid
        Texture2D whitePixel = new Texture2D(Core.GraphicsDevice, 1, 1);
        whitePixel.SetData(new[] { Color.White });

        _debugManager = new DebugManager(whitePixel);

#if DEBUG
        Core.Audio.SongVolume = 0f;
#endif
    }

    public override void Update(GameTime gameTime)
    {
        // Only handle Enter key if the title screen buttons panel is visible
        if (_titleScreenButtonsPanel.IsVisible && Core.Input.Keyboard.WasKeyJustPressed(Keys.Enter))
        {
            if (_startButton.IsFocused)
            {
                HandleStartClicked(_startButton, EventArgs.Empty);
            }
            else if (_optionsButton.IsFocused)
            {
                HandleOptionsClicked(_optionsButton, EventArgs.Empty);
            }
        }
        // If options panel is visible, let Gum handle Enter for focused control (sliders/back button)
        // GumService.Default.Update will route Enter to the focused control

        // Toggle UI debug grid with F9
        if (Core.Input.Keyboard.WasKeyJustPressed(Keys.F9))
        {
            _debugManager.ShowUIDebugGrid = !_debugManager.ShowUIDebugGrid;
        }

        GumService.Default.Update(gameTime);
    }

    public override void Draw(GameTime gameTime)
    {
        // Intentionally left blank; use DrawUI instead.
    }

    public override void DrawUI(GameTime gameTime)
    {
        GumService.Default.Draw();

        // Draw the UI debug grid if enabled
        if (_debugManager?.ShowUIDebugGrid == true)
        {
            _debugManager.DrawUIDebugGrid(Core.SpriteBatch, Core.GraphicsDevice.Viewport, 40, 40, Color.Black * 0.25f, _debugFont);
        }
    }
    private void InitializeUI()
    {
        GumService.Default.Root.Children.Clear();

        // Title Text (centered, large, gold/orange, with shadow)
        var titleText = new TextRuntime
        {
            Text = HEARTHVALE_TEXT,
            FontScale = 2.2f,
            X = Core.GraphicsDevice.PresentationParameters.BackBufferWidth / 2f,
            Y = 260f,
            UseCustomFont = true,
            CustomFontFile = @"fonts/04b_30.fnt",
            Color = new Color(255, 180, 60) // Gold/orange
        };
        titleText.Anchor(Gum.Wireframe.Anchor.Center);
        GumService.Default.Root.AddChild(titleText);

        // --- Create a panel to hold the buttons ---
        _titleScreenButtonsPanel = new Gum.Forms.Controls.Panel();
        _titleScreenButtonsPanel.Visual.Width = 180f;
        _titleScreenButtonsPanel.Visual.Height = 90f;
        _titleScreenButtonsPanel.Visual.X = Core.GraphicsDevice.PresentationParameters.BackBufferWidth / 2f - 90f;
        _titleScreenButtonsPanel.Visual.Y = 740;
        GumService.Default.Root.AddChild(_titleScreenButtonsPanel);

        // Start Button
        _startButton = new AnimatedButton(_atlas);
        _startButton.Text = "START";
        _startButton.Width = 180f;
        _startButton.Height = 38f;
        _startButton.Click += HandleStartClicked;
        _titleScreenButtonsPanel.AddChild(_startButton);

        // Options Button
        _optionsButton = new AnimatedButton(_atlas);
        _optionsButton.Text = "OPTIONS";
        _optionsButton.Width = 180f;
        _optionsButton.Height = 38f;
        _optionsButton.Y = 55f;
        _optionsButton.Click += HandleOptionsClicked;
        _titleScreenButtonsPanel.AddChild(_optionsButton);

        // "Press Enter" text below buttons
        var pressEnterText = new TextRuntime
        {
            Text = PRESS_ENTER_TEXT,
            FontScale = 0.5f,
            X = Core.GraphicsDevice.PresentationParameters.BackBufferWidth / 2f,
            Y = 440f,
            Color = new Color(255, 255, 255, 180)
        };
        pressEnterText.Anchor(Gum.Wireframe.Anchor.Center);
        GumService.Default.Root.AddChild(pressEnterText);

        // Set initial focus to the start button
        _startButton.IsFocused = true;

        CreateOptionsPanel();
    }

    protected override void Dispose(bool disposing)
    {
        if (IsDisposed) return;

        if (disposing)
        {
            // IMPORTANT: Unsubscribe from events to prevent them from
            // firing in other scenes.
            if (_startButton != null) _startButton.Click -= HandleStartClicked;
            if (_optionsButton != null) _optionsButton.Click -= HandleOptionsClicked;
            if (_optionsBackButton != null) _optionsBackButton.Click -= HandleOptionsButtonBack;


            // Remove all UI elements from the global Gum root
            // to prevent them from capturing input in other scenes.
            GumService.Default.Root.Children.Clear();
        }

        base.Dispose(disposing);
    }

    private void CreateOptionsPanel()
    {
        _optionsPanel = new Gum.Forms.Controls.Panel();
        _optionsPanel.Dock(Gum.Wireframe.Dock.Fill);
        _optionsPanel.IsVisible = false;
        _optionsPanel.AddToRoot();

        TextRuntime optionsText = new TextRuntime();
        optionsText.X = 10;
        optionsText.Y = 10;
        optionsText.Text = "OPTIONS";
        optionsText.UseCustomFont = true;
        optionsText.FontScale = 0.5f;
        optionsText.CustomFontFile = @"fonts/04b_30.fnt";
        _optionsPanel.AddChild(optionsText);

        OptionsSlider musicSlider = new OptionsSlider(_atlas);
        musicSlider.Name = "MusicSlider";
        musicSlider.Text = "MUSIC";
        musicSlider.Anchor(Gum.Wireframe.Anchor.Top);
        musicSlider.Visual.Y = 30f;
        musicSlider.Minimum = 0;
        musicSlider.Maximum = 1;
        musicSlider.Value = Core.Audio.SongVolume;
        musicSlider.SmallChange = .1;
        musicSlider.LargeChange = .2;
        musicSlider.ValueChanged += HandleMusicSliderValueChanged;
        musicSlider.ValueChangeCompleted += HandleMusicSliderValueChangeCompleted;
        _optionsPanel.AddChild(musicSlider);

        OptionsSlider sfxSlider = new OptionsSlider(_atlas);
        sfxSlider.Name = "SfxSlider";
        sfxSlider.Text = "SFX";
        sfxSlider.Anchor(Gum.Wireframe.Anchor.Top);
        sfxSlider.Visual.Y = 93;
        sfxSlider.Minimum = 0;
        sfxSlider.Maximum = 1;
        sfxSlider.Value = Core.Audio.SoundEffectVolume;
        sfxSlider.SmallChange = .1;
        sfxSlider.LargeChange = .2;
        sfxSlider.ValueChanged += HandleSfxSliderChanged;
        sfxSlider.ValueChangeCompleted += HandleSfxSliderChangeCompleted;
        _optionsPanel.AddChild(sfxSlider);

        _optionsBackButton = new AnimatedButton(_atlas);
        _optionsBackButton.Text = "BACK";
        _optionsBackButton.Anchor(Gum.Wireframe.Anchor.BottomRight);
        _optionsBackButton.X = -28f;
        _optionsBackButton.Y = -10f;
        _optionsBackButton.Click += HandleOptionsButtonBack;
        _optionsPanel.AddChild(_optionsBackButton);
    }

    private void HandleSfxSliderChanged(object sender, EventArgs args)
    {
        // Intentionally not playing the UI sound effect here so that it is not
        // constantly triggered as the user adjusts the slider's thumb on the
        // track.

        // Get a reference to the sender as a Slider.
        var slider = (Gum.Forms.Controls.Slider)sender;

        // Set the global sound effect volume to the value of the slider.;
        Core.Audio.SoundEffectVolume = (float)slider.Value;
    }

    private void HandleSfxSliderChangeCompleted(object sender, EventArgs e)
    {
        // Play the UI Sound effect so the player can hear the difference in audio.
        Core.Audio.PlaySoundEffect(_uiSoundEffect);
    }

    private void HandleMusicSliderValueChanged(object sender, EventArgs args)
    {
        // Intentionally not playing the UI sound effect here so that it is not
        // constantly triggered as the user adjusts the slider's thumb on the
        // track.

        // Get a reference to the sender as a Slider.
        var slider = (Gum.Forms.Controls.Slider)sender;

        // Set the global song volume to the value of the slider.
        Core.Audio.SongVolume = (float)slider.Value;
    }

    private void HandleMusicSliderValueChangeCompleted(object sender, EventArgs args)
    {
        // A UI interaction occurred, play the sound effect
        Core.Audio.PlaySoundEffect(_uiSoundEffect);
    }

    private void HandleOptionsButtonBack(object sender, EventArgs e)
    {
        // A UI interaction occurred, play the sound effect
        Core.Audio.PlaySoundEffect(_uiSoundEffect);

        // Set the title panel to be visible.
        _titleScreenButtonsPanel.IsVisible = true;

        // Set the options panel to be invisible.
        _optionsPanel.IsVisible = false;

        // Give the options button on the title panel focus since we are coming
        // back from the options screen.
        _optionsButton.IsFocused = true;
    }

    private void HandleStartClicked(object sender, EventArgs e)
    {
        // Play UI sound effect
        Core.Audio.PlaySoundEffect(_uiSoundEffect);

        // Transition to the main game scene
        Core.ChangeScene(new GameScene());
    }

    private void HandleOptionsClicked(object sender, EventArgs e)
    {
        // Play UI sound effect
        Core.Audio.PlaySoundEffect(_uiSoundEffect);

        // Show the options panel and hide the title buttons panel
        if (_optionsPanel != null)
            _optionsPanel.IsVisible = true;
        if (_titleScreenButtonsPanel != null)
            _titleScreenButtonsPanel.IsVisible = false;
    }

    private NineSliceRuntime CreateButtonNineSlice(float width, float height)
    {
        // Get the region from your atlas (ensure "button-background" exists in your atlas XML)
        TextureRegion region = _atlas.GetRegion("unfocused-button");

        var nineSlice = new NineSliceRuntime();
        nineSlice.Texture = region.Texture;
        nineSlice.TextureAddress = TextureAddress.Custom;
        nineSlice.TextureLeft = region.SourceRectangle.Left;
        nineSlice.TextureTop = region.SourceRectangle.Top;
        nineSlice.TextureWidth = region.Width;
        nineSlice.TextureHeight = region.Height;
        nineSlice.Width = width;
        nineSlice.Height = height;
        nineSlice.Dock(Gum.Wireframe.Dock.Fill);

        // Set slice margins (adjust these to match your image's corner thickness)
        //nineSlice.LeftMargin = 8;
        //nineSlice.TopMargin = 8;
        //nineSlice.RightMargin = 8;
        //nineSlice.BottomMargin = 8;

        return nineSlice;
    }
}