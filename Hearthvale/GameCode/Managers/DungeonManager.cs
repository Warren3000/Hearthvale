using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using MonoGame.Extended.Tiled;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

/// <summary>
/// Represents a dungeon element that can trigger an activation event.
/// </summary>
public interface IActivatorElement : IDungeonElement
{
    /// <summary>
    /// Occurs when the element is activated.
    /// </summary>
    event Action OnActivated;
}

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

    /// <summary>
    /// Wires an activator dungeon element to a target dungeon element.
    /// When the activator is triggered, the target's Activate method is called.
    /// </summary>
    /// <param name="activatorId">The ID of the element that triggers the action (e.g., a switch or pressure plate).</param>
    /// <param name="targetId">The ID of the element that responds to the action (e.g., a door or trap).</param>
    public void WireUp(string activatorId, string targetId)
    {
        var activator = GetElement<IActivatorElement>(activatorId);
        var target = GetElement<IDungeonElement>(targetId);

        if (activator != null && target != null)
        {
            activator.OnActivated += target.Activate;
        }
    }

    /// <summary>
    /// Loads a dungeon from an XML file, creating the tilemap and all dungeon elements.
    /// </summary>
    /// <param name="content">The content manager.</param>
    /// <param name="filename">The path to the level XML file.</param>
    /// <returns>The loaded tilemap.</returns>
    public TiledMap LoadDungeonFromFile(ContentManager content, string filename)
    {
        // Ensure the path is relative to the Content directory
        string contentPath = filename.StartsWith("Content/")
            ? filename
            : $"Content/{filename}";

        var doc = XDocument.Load(TitleContainer.OpenStream(contentPath));
        var mapElement = doc.Root.Element("Map");
        var tilemapPath = mapElement?.Attribute("tilemap")?.Value;

        if (string.IsNullOrEmpty(tilemapPath))
        {
            throw new Exception("Level file must specify a 'tilemap' attribute in the 'Map' element.");
        }

        var tilemap = content.Load<TiledMap>(tilemapPath);

        // Load elements like switches, doors, etc.
        foreach (var element in doc.Root.Element("Elements").Elements())
        {
            var id = element.Attribute("id").Value;
            var type = element.Name.ToString();

            IDungeonElement newElement = type switch
            {
                "DungeonSwitch" => new DungeonSwitch(id,
                    int.Parse(element.Attribute("col").Value),
                    int.Parse(element.Attribute("row").Value),
                    int.Parse(element.Attribute("inactiveTileId").Value),
                    int.Parse(element.Attribute("activeTileId").Value)),
                "DungeonDoor" => new DungeonDoor(id,
                    int.Parse(element.Attribute("col").Value),
                    int.Parse(element.Attribute("row").Value),
                    int.Parse(element.Attribute("lockedTileId").Value),
                    int.Parse(element.Attribute("unlockedTileId").Value)),
                "DungeonTrap" => new DungeonTrap(id,
                    Enum.Parse<TrapType>(element.Attribute("trapType").Value)),
                "DungeonPuzzle" => new DungeonPuzzle(id,
                    Enum.Parse<PuzzleType>(element.Attribute("puzzleType").Value)),
                "DungeonEncounter" => new DungeonEncounter(id),
                "DungeonLoot" => new DungeonLoot(id,
                    element.Attribute("lootTableId").Value),
                _ => null
            };

            if (newElement != null)
            {
                AddElement(newElement);
            }
        }

        // Wire up elements
        foreach (var wiring in doc.Root.Element("Wiring").Elements("Wire"))
        {
            var activatorId = wiring.Attribute("activator").Value;
            var targetId = wiring.Attribute("target").Value;
            WireUp(activatorId, targetId);
        }

        return tilemap;
    }
}