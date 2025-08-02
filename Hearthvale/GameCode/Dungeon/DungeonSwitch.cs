using Microsoft.Xna.Framework;
using System;

public class DungeonSwitch : IDungeonElement
{
    public string Id { get; }
    public bool IsActive { get; private set; }
    public event Action OnActivated;

    public void Activate()
    {
        IsActive = true;
        OnActivated?.Invoke();
    }

    public void Deactivate() => IsActive = false;

    public void Update(GameTime gameTime) { /* Switch logic */ }
}