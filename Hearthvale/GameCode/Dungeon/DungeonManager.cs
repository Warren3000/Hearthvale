using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;

public class DungeonManager
{
    private readonly List<IDungeonElement> _elements = new();

    public void AddElement(IDungeonElement element) => _elements.Add(element);

    public void Update(GameTime gameTime)
    {
        foreach (var element in _elements)
            element.Update(gameTime);
    }

    public T GetElement<T>(string id) where T : class, IDungeonElement
        => _elements.OfType<T>().FirstOrDefault(e => e.Id == id);

    // Persistence, event wiring, and state management can be added here
}