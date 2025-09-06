using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using MonoGame.Extended.Tiled;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Hearthvale.GameCode.Managers
{
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

    /// <summary>
    /// Manages dungeon elements and interactions, supporting both singleton and inheritance patterns.
    /// </summary>
    public class DungeonManager
    {
        private static DungeonManager _instance;
        public static DungeonManager Instance => _instance ?? throw new InvalidOperationException("DungeonManager not initialized");

        protected readonly List<IDungeonElement> _elements = new();
        protected readonly Dictionary<string, HashSet<string>> _elementTags = new();

        /// <summary>
        /// Initializes the singleton instance with the base DungeonManager.
        /// </summary>
        public static void Initialize()
        {
            _instance ??= new DungeonManager();
        }

        /// <summary>
        /// Initializes the singleton instance with a custom DungeonManager implementation.
        /// </summary>
        /// <param name="customManager">Custom dungeon manager instance (e.g., ProceduralDungeonManager)</param>
        public static void Initialize(DungeonManager customManager)
        {
            _instance = customManager ?? throw new ArgumentNullException(nameof(customManager));
        }

        public static void Shutdown()
        {
            _instance = null;
        }

        /// <summary>
        /// Protected constructor to allow inheritance while maintaining singleton pattern.
        /// </summary>
        protected DungeonManager() { }

        public void AddElement(IDungeonElement element) => _elements.Add(element);

        public virtual void Update(GameTime gameTime)
        {
            foreach (var element in _elements)
                element.Update(gameTime);
        }

        public T GetElement<T>(string id) where T : class, IDungeonElement
            => _elements.OfType<T>().FirstOrDefault(e => e.Id == id);

        /// <summary>
        /// Gets all elements with the specified tag.
        /// </summary>
        /// <param name="tag">The tag to search for.</param>
        /// <returns>Collection of elements with the specified tag.</returns>
        public IEnumerable<IDungeonElement> GetElementsByTag(string tag)
        {
            var elementIds = _elementTags.Where(kvp => kvp.Value.Contains(tag)).Select(kvp => kvp.Key);
            return _elements.Where(e => elementIds.Contains(e.Id));
        }

        /// <summary>
        /// Adds a tag to an element.
        /// </summary>
        /// <param name="elementId">The element ID.</param>
        /// <param name="tag">The tag to add.</param>
        public void AddElementTag(string elementId, string tag)
        {
            if (!_elementTags.ContainsKey(elementId))
                _elementTags[elementId] = new HashSet<string>();

            _elementTags[elementId].Add(tag);
        }

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
        /// Wires an activator to all elements with the specified tag.
        /// </summary>
        /// <param name="activatorId">The ID of the activator element.</param>
        /// <param name="targetTag">The tag of target elements.</param>
        public void WireUpByTag(string activatorId, string targetTag)
        {
            var activator = GetElement<IActivatorElement>(activatorId);
            if (activator == null) return;

            var targets = GetElementsByTag(targetTag);
            foreach (var target in targets)
            {
                activator.OnActivated += target.Activate;
            }
        }

        public IEnumerable<IDungeonElement> GetAllElements()
        {
            return _elements;
        }

        /// <summary>
        /// Loads a dungeon from an XML file, creating the tilemap and all dungeon elements.
        /// </summary>
        /// <param name="content">The content manager.</param>
        /// <param name="filename">The path to the level XML file.</param>
        /// <returns>The loaded tilemap.</returns>
        public virtual TiledMap LoadDungeonFromFile(ContentManager content, string filename)
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
            var elementsElement = doc.Root.Element("Elements");
            if (elementsElement != null)
            {
                foreach (var element in elementsElement.Elements())
                {
                    var id = element.Attribute("id")?.Value;
                    if (string.IsNullOrEmpty(id)) continue;

                    var type = element.Name.ToString();

                    IDungeonElement newElement = type switch
                    {
                        "DungeonSwitch" => CreateDungeonSwitch(element),
                        "DungeonDoor" => CreateDungeonDoor(element),
                        "DungeonLever" => CreateDungeonLever(element),
                        "DungeonPressurePlate" => CreateDungeonPressurePlate(element),
                        "DungeonTrap" => CreateDungeonTrap(element),
                        "DungeonPuzzle" => new DungeonPuzzle(id,
                            Enum.Parse<PuzzleType>(element.Attribute("puzzleType")?.Value ?? "BlockPushing")),
                        "DungeonEncounter" => new DungeonEncounter(id),
                        "DungeonLoot" => CreateDungeonLoot(element),
                        _ => null
                    };

                    if (newElement != null)
                    {
                        AddElement(newElement);

                        // Load tags
                        var tagsAttr = element.Attribute("tags")?.Value;
                        if (!string.IsNullOrEmpty(tagsAttr))
                        {
                            var tags = tagsAttr.Split(',', StringSplitOptions.RemoveEmptyEntries);
                            foreach (var tag in tags)
                            {
                                AddElementTag(id, tag.Trim());
                            }
                        }
                    }
                }
            }

            // Wire up elements
            var wiringElement = doc.Root.Element("Wiring");
            if (wiringElement != null)
            {
                foreach (var wiring in wiringElement.Elements("Wire"))
                {
                    var activatorId = wiring.Attribute("activator")?.Value;
                    var targetId = wiring.Attribute("target")?.Value;
                    var targetTag = wiring.Attribute("targetTag")?.Value;

                    if (!string.IsNullOrEmpty(activatorId))
                    {
                        if (!string.IsNullOrEmpty(targetId))
                        {
                            WireUp(activatorId, targetId);
                        }
                        else if (!string.IsNullOrEmpty(targetTag))
                        {
                            WireUpByTag(activatorId, targetTag);
                        }
                    }
                }
            }

            return tilemap;
        }

        protected virtual IDungeonElement CreateDungeonSwitch(XElement element)
        {
            var id = element.Attribute("id").Value;
            var col = int.Parse(element.Attribute("col")?.Value ?? "0");
            var row = int.Parse(element.Attribute("row")?.Value ?? "0");
            var inactiveTileId = int.Parse(element.Attribute("inactiveTileId")?.Value ?? "0");
            var activeTileId = int.Parse(element.Attribute("activeTileId")?.Value ?? "0");
            var switchType = Enum.Parse<SwitchType>(element.Attribute("switchType")?.Value ?? "Toggle");
            var duration = float.Parse(element.Attribute("duration")?.Value ?? "0");

            return (IDungeonElement)new DungeonSwitch(id, col, row, inactiveTileId, activeTileId, switchType, duration);
        }

        protected virtual DungeonDoor CreateDungeonDoor(XElement element)
        {
            var id = element.Attribute("id").Value;
            var col = int.Parse(element.Attribute("col").Value);
            var row = int.Parse(element.Attribute("row").Value);
            var lockedTileId = int.Parse(element.Attribute("lockedTileId").Value);
            var unlockedTileId = int.Parse(element.Attribute("unlockedTileId").Value);
            var doorType = Enum.Parse<DoorType>(element.Attribute("doorType")?.Value ?? "Normal");
            var keyRequired = element.Attribute("keyRequired")?.Value;

            return new DungeonDoor(id, col, row, lockedTileId, unlockedTileId, doorType, keyRequired);
        }

        protected virtual IDungeonElement CreateDungeonLever(XElement element)
        {
            var id = element.Attribute("id").Value;
            var col = int.Parse(element.Attribute("col")?.Value ?? "0");
            var row = int.Parse(element.Attribute("row")?.Value ?? "0");
            var inactiveTileId = int.Parse(element.Attribute("inactiveTileId")?.Value ?? "0");
            var activeTileId = int.Parse(element.Attribute("activeTileId")?.Value ?? "0");
            var leverType = Enum.Parse<LeverType>(element.Attribute("leverType")?.Value ?? "Reusable");

            return (IDungeonElement)new DungeonLever(id, col, row, inactiveTileId, activeTileId, leverType);
        }

        protected virtual IDungeonElement CreateDungeonPressurePlate(XElement element)
        {
            var id = element.Attribute("id").Value;
            var col = int.Parse(element.Attribute("col")?.Value ?? "0");
            var row = int.Parse(element.Attribute("row")?.Value ?? "0");
            var inactiveTileId = int.Parse(element.Attribute("inactiveTileId")?.Value ?? "0");
            var activeTileId = int.Parse(element.Attribute("activeTileId")?.Value ?? "0");
            var plateType = Enum.Parse<PressurePlateType>(element.Attribute("plateType")?.Value ?? "Momentary");
            var weightRequired = float.Parse(element.Attribute("weightRequired")?.Value ?? "1");

            return (IDungeonElement)new DungeonPressurePlate(id, col, row, inactiveTileId, activeTileId, plateType, weightRequired);
        }

        protected virtual DungeonTrap CreateDungeonTrap(XElement element)
        {
            var id = element.Attribute("id").Value;
            var trapType = Enum.Parse<TrapType>(element.Attribute("trapType").Value);
            var damage = float.Parse(element.Attribute("damage")?.Value ?? "10");
            var cooldown = float.Parse(element.Attribute("cooldown")?.Value ?? "2");
            var col = int.Parse(element.Attribute("col")?.Value ?? "0");
            var row = int.Parse(element.Attribute("row")?.Value ?? "0");

            return new DungeonTrap(id, trapType, damage, cooldown, col, row);
        }

        protected virtual DungeonLoot CreateDungeonLoot(XElement element)
        {
            var id = element.Attribute("id").Value;
            var lootTableId = element.Attribute("lootTableId")?.Value ?? "default";
            var col = int.Parse(element.Attribute("col")?.Value ?? "0");
            var row = int.Parse(element.Attribute("row")?.Value ?? "0");
            var isTrapped = bool.Parse(element.Attribute("isTrapped")?.Value ?? "false");
            var trapId = element.Attribute("trapId")?.Value;

            return new DungeonLoot(id, lootTableId, col, row, isTrapped, trapId);
        }

        /// <summary>
        /// Clears all elements and tags. Used for scene transitions.
        /// </summary>
        public virtual void Clear()
        {
            _elements.Clear();
            _elementTags.Clear();
        }

        /// <summary>
        /// Gets all elements of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of dungeon element.</typeparam>
        /// <returns>Collection of dungeon elements of the specified type.</returns>
        public IEnumerable<T> GetElements<T>() where T : class, IDungeonElement
            => GetAllElements().OfType<T>();
    }
}