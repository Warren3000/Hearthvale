using Microsoft.Xna.Framework;

public class DungeonDoor : IDungeonElement
{
    public string Id { get; }
    public bool IsLocked { get; private set; }
    public bool IsActive => !IsLocked;

    public void Activate() => IsLocked = false;
    public void Deactivate() => IsLocked = true;

    public void Update(GameTime gameTime) { /* Door animation/logic */ }
}