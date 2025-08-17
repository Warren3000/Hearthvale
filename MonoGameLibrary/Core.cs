using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary.Audio;
using MonoGameLibrary.Input;
using MonoGameLibrary.Scenes;
using System;

namespace MonoGameLibrary;

public class Core : Game
{
    internal static Core s_instance;

    /// <summary>
    /// Optional logging delegates to avoid engine/game circular dependencies.
    /// Assign these in your game (e.g., Game1.LoadContent) to route logs to your logger.
    /// </summary>
    public static Action<string> CameraLog { get; set; }
    public static Action<string> SceneLog { get; set; }

    public static Core Instance => s_instance;
    public static Scene CurrentScene => s_activeScene;

    private static Scene s_activeScene;
    private static Scene s_nextScene;

    public static GraphicsDeviceManager Graphics { get; private set; }
    public static new GraphicsDevice GraphicsDevice { get; private set; }
    public static SpriteBatch SpriteBatch { get; private set; }
    public static new ContentManager Content { get; private set; }
    public static InputManager Input { get; private set; }
    public static bool ExitOnEscape { get; set; }
    public static AudioController Audio { get; private set; }

    public Core(string title, int width, int height, bool fullScreen)
    {
        if (s_instance != null)
        {
            throw new InvalidOperationException($"Only a single Core instance can be created");
        }

        s_instance = this;

        Graphics = new GraphicsDeviceManager(this);
        Graphics.PreferredBackBufferWidth = width;
        Graphics.PreferredBackBufferHeight = height;
        Graphics.IsFullScreen = fullScreen;
        Graphics.ApplyChanges();

        Window.Title = title;
        Content = base.Content;
        Content.RootDirectory = "Content";
        IsMouseVisible = true;

        this.Exiting += (sender, args) => Exit();
    }

    protected override void Initialize()
    {
        base.Initialize();
        GraphicsDevice = base.GraphicsDevice;
        SpriteBatch = new SpriteBatch(GraphicsDevice);
        Input = new InputManager();
        Audio = new AudioController();
    }

    protected override void UnloadContent()
    {
        Audio.Dispose();
        base.UnloadContent();
    }

    protected override void Update(GameTime gameTime)
    {
        Input.Update(gameTime);
        Audio.Update();

        if (ExitOnEscape && Input.Keyboard.IsKeyDown(Keys.Escape))
        {
            Exit();
        }

        if (s_nextScene != null)
        {
            TransitionScene();
        }

        s_activeScene?.Update(gameTime);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        Matrix cameraMatrix = Matrix.Identity;

        if (CurrentScene is ICameraProvider cameraProvider)
        {
            cameraMatrix = cameraProvider.GetViewMatrix();
            CameraLog?.Invoke($"🎥 Using camera matrix from scene: Translation=({cameraMatrix.M41}, {cameraMatrix.M42}), Scale=({cameraMatrix.M11}, {cameraMatrix.M22})");
        }
        else
        {
            SceneLog?.Invoke($"⚠️ Scene {CurrentScene?.GetType().Name} is not ICameraProvider, using identity matrix");
        }

        // World space drawing with camera transform
        SpriteBatch.Begin(transformMatrix: cameraMatrix, samplerState: SamplerState.PointClamp);
        s_activeScene?.DrawWorld(gameTime);
        SpriteBatch.End();

        // Screen space UI drawing (no transform)
        SpriteBatch.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, samplerState: SamplerState.PointClamp);
        s_activeScene?.DrawUI(gameTime);
        SpriteBatch.End();

        base.Draw(gameTime);
    }

    public static void ChangeScene(Scene next)
    {
        if (s_activeScene != next)
        {
            s_nextScene = next;
        }
    }

    private static void TransitionScene()
    {
        if (s_activeScene != null)
        {
            s_activeScene.Dispose();
        }

        GC.Collect();

        s_activeScene = s_nextScene;
        s_nextScene = null;

        if (s_activeScene != null)
        {
            s_activeScene.Initialize();
        }
    }
}

