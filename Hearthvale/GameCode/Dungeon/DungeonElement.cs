using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public interface IDungeonElement
{
    string Id { get; }
    bool IsActive { get; }
    void Activate();
    void Deactivate();
    void Update(GameTime gameTime);
    void DrawDebug(SpriteBatch spriteBatch, Texture2D pixel);
}