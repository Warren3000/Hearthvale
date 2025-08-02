using Microsoft.Xna.Framework;

public interface IDungeonElement
{
    string Id { get; }
    bool IsActive { get; }
    void Activate();
    void Deactivate();
    void Update(GameTime gameTime);
}