using Hearthvale.GameCode.Bootstrap;
using Hearthvale.GameCode.Managers;
using Hearthvale.GameCode.Systems;
using Hearthvale.Scenes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary;

namespace Hearthvale;

public class Game1 : Core
{
    public Game1() : base("Hearthvale", 1920, 1080, false) { }

    protected override void Initialize()
    {
        base.Initialize();

        GameBootstrapper.InitializeAll(this);

        // Start scene (could later move to SceneSystem)
        SceneManager.ChangeScene(new TitleScene());
    }

    protected override void Update(GameTime gameTime)
    {
        if (ExitOnEscape && Input.Keyboard.IsKeyDown(Keys.Escape))
            Exit();

        SceneManager.Update(gameTime);
        SystemManager.UpdateAll(gameTime);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        SceneManager.Draw(gameTime);
        SystemManager.DrawAll(gameTime);
        base.Draw(gameTime);
    }
}