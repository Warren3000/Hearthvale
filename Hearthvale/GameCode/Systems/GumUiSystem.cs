using Gum.Forms.Controls;
using Microsoft.Xna.Framework.Input;
using MonoGameGum;
using MonoGameLibrary;

namespace Hearthvale.GameCode.Systems;

/// <summary>
/// Initializes Gum UI and keeps canvas sizing consistent.
/// </summary>
public sealed class GumUiSystem : IGameSystem
{
    private readonly Core _core;

    public GumUiSystem(Core core)
    {
        _core = core;
    }

    public void Initialize()
    {
        GumService.Default.Initialize(_core);
        GumService.Default.ContentLoader.XnaContentManager = Core.Content;
        FrameworkElement.KeyboardsForUiControl.Add(GumService.Default.Keyboard);
        FrameworkElement.GamePadsForUiControl.AddRange(GumService.Default.Gamepads);
        FrameworkElement.TabReverseKeyCombos.Add(new KeyCombo { PushedKey = Keys.Up });
        FrameworkElement.TabKeyCombos.Add(new KeyCombo { PushedKey = Keys.Down });
        GumService.Default.CanvasWidth  = Core.GraphicsDevice.PresentationParameters.BackBufferWidth;
        GumService.Default.CanvasHeight = Core.GraphicsDevice.PresentationParameters.BackBufferHeight;
        GumService.Default.Renderer.Camera.Zoom = 1.0f;
    }

    public void Update(Microsoft.Xna.Framework.GameTime gameTime) { }
}