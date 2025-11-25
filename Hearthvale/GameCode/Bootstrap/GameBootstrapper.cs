using Hearthvale.GameCode.Managers;
using Hearthvale.GameCode.Managers.Dungeon;
using Hearthvale.GameCode.Systems;
using MonoGameLibrary;

namespace Hearthvale.GameCode.Bootstrap;

/// <summary>
/// Central place for standing up managers + core systems to keep Game1 thin.
/// </summary>
public static class GameBootstrapper
{
    public static AssetManagerSystem AssetSystem { get; private set; }

    public static void InitializeAll(Core core)
    {
        // Initialize managers (singletons)
        ConfigurationManager.Initialize();
        DataManager.Initialize();
        DungeonManager.Initialize(new AutoLootDungeonManager(
            lootTableIds: new[] { "default" },
            roomLootChance: 0.6f,
            trapChance: 0.0f
        ));

        // Systems registration order matters for dependencies
        AssetSystem = new AssetManagerSystem(Core.Content);
        SystemManager.Register(new LoggingSystem());
        SystemManager.Register(AssetSystem);
        SystemManager.Register(new InputSystem());
        SystemManager.Register(new AudioSystem(AssetSystem));
        SystemManager.Register(new GumUiSystem(core), participatesInDraw: true);
        // SystemManager.Register(new SpriteAnalysisSystem(AssetSystem));

        SystemManager.InitializeAll();
    }
}
