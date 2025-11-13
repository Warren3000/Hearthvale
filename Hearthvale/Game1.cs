using Hearthvale.GameCode.Bootstrap;
using Hearthvale.GameCode.Managers;
using Hearthvale.GameCode.Systems;
using Hearthvale.Scenes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using Hearthvale.GameCode.Rendering;
using MonoGameLibrary;

namespace Hearthvale;

public class Game1 : Core
{
    private RenderTarget2D _sceneTarget;
    private Effect _postEffect;
    private bool _postEnabled = true;
    private bool _prevF10Down;
    public Game1() : base("Next Day Deadlivery", 1920, 1080, false) { }

    protected override void Initialize()
    {
        base.Initialize();

        GameBootstrapper.InitializeAll(this);

        // Start scene (could later move to SceneSystem)
        SceneManager.ChangeScene(new TitleScene());
    }

    protected override void LoadContent()
    {
        base.LoadContent();

        var game = (Microsoft.Xna.Framework.Game)this;
        var pp = game.GraphicsDevice.PresentationParameters;
        _sceneTarget = new RenderTarget2D(game.GraphicsDevice, pp.BackBufferWidth, pp.BackBufferHeight, false, pp.BackBufferFormat, DepthFormat.None);
        try
        {
            _postEffect = game.Content.Load<Effect>("shaders/PostHorror");
        }
        catch
        {
            _postEffect = null; // Allow running without the effect if content not built yet
        }
    }

    protected override void Update(GameTime gameTime)
    {
        if (ExitOnEscape && Input.Keyboard.IsKeyDown(Keys.Escape))
            Exit();

        // Toggle post-processing on F10 (edge-triggered)
        bool f10Down = Input.Keyboard.IsKeyDown(Keys.F10);
        if (f10Down && !_prevF10Down)
            _postEnabled = !_postEnabled;
        _prevF10Down = f10Down;

        SceneManager.Update(gameTime);
        SystemManager.UpdateAll(gameTime);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        if (_sceneTarget != null && _postEffect != null && _postEnabled)
        {
            var game = (Microsoft.Xna.Framework.Game)this;
            // 1) Render scene to offscreen target
            game.GraphicsDevice.SetRenderTarget(_sceneTarget);
            game.GraphicsDevice.Clear(Color.Black);

            // draw the normal pipeline into the target using Core's rendering
            SceneManager.Draw(gameTime);
            SystemManager.DrawAll(gameTime);
            base.Draw(gameTime);

            // 2) Present with post-processing
            game.GraphicsDevice.SetRenderTarget(null);

            _postEffect.Parameters["Resolution"]?.SetValue(new Vector2(_sceneTarget.Width, _sceneTarget.Height));
            _postEffect.Parameters["DesaturateAmount"]?.SetValue(Theme.Current.Desaturate);
            _postEffect.Parameters["VignetteIntensity"]?.SetValue(Theme.Current.Vignette);
            _postEffect.Parameters["GrainIntensity"]?.SetValue(Theme.Current.Grain);

            SpriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.PointClamp, null, null, _postEffect);
            SpriteBatch.Draw(_sceneTarget, new Rectangle(0, 0, _sceneTarget.Width, _sceneTarget.Height), Color.White);
            SpriteBatch.End();
        }
        else
        {
            // Fallback to default rendering if effect or target not available
            SceneManager.Draw(gameTime);
            SystemManager.DrawAll(gameTime);
            base.Draw(gameTime);
        }
    }
}