# Hearthvale - AI Coding Agent Instructions

## Project Overview
Hearthvale is a 2D action RPG built with **MonoGame (.NET 8)** inspired by Dark Chronicle's combat mechanics. The game features procedurally generated dungeons, physics-based combat, and a component-driven architecture.

## Core Architecture

### Game Loop & Initialization
- Entry point: `Game1.cs` extends `Core` (from MonoGameLibrary) with post-processing shader support
- Bootstrap: `GameBootstrapper.InitializeAll()` registers managers (singleton pattern) and systems in dependency order
- Systems: Registered via `SystemManager.Register()` - order matters for dependencies (e.g., AssetSystem before AudioSystem)
- Scenes: Managed by `SceneManager.ChangeScene()` - wraps `Core.ChangeScene()`

### System Architecture Pattern
All game systems implement `IGameSystem` interface:
```csharp
public interface IGameSystem {
    void Initialize();  // Called once after registration
    void Update(GameTime gameTime);
    void Draw(GameTime gameTime) { } // Optional
}
```

Register systems in `GameBootstrapper` with explicit ordering. Example:
```csharp
SystemManager.Register(new LoggingSystem());
SystemManager.Register(AssetSystem);  // Required before AudioSystem
SystemManager.Register(new AudioSystem(AssetSystem));
SystemManager.Register(new GumUiSystem(core), participatesInDraw: true);
```

### Manager Pattern (Singletons)
Managers use lazy singleton initialization via `Initialize()` method:
- `DataManager.Initialize()` - loads all JSON/XML game data from `Content/data/`
- `ConfigurationManager.Initialize()` - loads/caches XML configurations
- `DungeonManager.Initialize(implementation)` - manages dungeon state and elements
- Access via `ManagerName.Instance` (throws if not initialized)

### Physics & Collision System
**Aether.Physics2D** integration with custom collision actors:
- `CollisionWorld` wraps Aether physics world (64 pixels = 1 meter)
- Collision actors: `PlayerCollisionActor`, `NpcCollisionActor`, `WallCollisionActor`, `ChestCollisionActor`
- Characters use `CharacterCollisionComponent.TryMove()` for physics-based movement with wall sliding
- Register entities with `CollisionWorldManager.RegisterPlayer()` / `RegisterNpc()`
- Wall colliders auto-generated from tilemap via `MapUtils.GetWallRectangles()`

**CRITICAL**: Call `character.CollisionComponent.SetCollisionWorld()` after registering with `CollisionWorldManager`

### Entity Component Pattern
Characters (Player, NPC, Enemy) use composition over inheritance:
- `CharacterCollisionComponent` - physics movement, knockback, wall sliding
- `CharacterHealthComponent` - health management
- `CharacterWeaponComponent` - equipped weapon
- `CharacterRenderComponent` - sprite rendering
- `CharacterStatsComponent` - stats from DataManager

NPCs add: `NpcCombatComponent`, `NpcAnimationComponent`, `NpcBuffComponent`

### Data Management
`DataManager` loads all game data on initialization from `Content/data/`:
- Character stats: `Characters/CharacterStats.json` (JSON dictionaries)
- Weapons: `Items/Weapons/WeaponStats.json`
- Enemies: `Characters/Enemies/enemies.xml` (XML - legacy, consider migrating to JSON)
- Access: `DataManager.Instance.GetCharacterStats("player")`, `GetWeaponStats("Dagger-Copper")`

**Hot-reload support**: `DataManager.Instance.ReloadDataCategory("items")` for development iteration

### Scene Structure
Scenes implement `Scene` base class (from MonoGameLibrary):
- Override `LoadContent()` for initialization
- Override `DrawWorld()` for camera-transformed rendering (entities, tilemap)
- Override `DrawUI()` for screen-space UI (health bars, HUD)
- **Never override `Draw()` directly** - use DrawWorld/DrawUI separation

Example scene lifecycle:
```csharp
public class GameScene : Scene, ICameraProvider {
    public override void LoadContent() {
        // 1. Load atlases/assets
        // 2. Initialize DungeonManager
        // 3. Create Player/NPCs
        // 4. Register with CollisionWorldManager
        // 5. Setup CameraManager
    }
    
    public override void DrawWorld(GameTime gameTime) {
        _tilemap.Draw(Core.SpriteBatch);
        _player.Draw(Core.SpriteBatch);
        // All camera-transformed rendering
    }
    
    public override void DrawUI(GameTime gameTime) {
        GumService.Default.Draw();
        // Screen-space UI only
    }
}
```

### Camera Management
`CameraManager` singleton wraps `Camera2D`:
- Initialize: `CameraManager.Initialize(new Camera2D(viewport) { Zoom = 3.0f })`
- Update: `CameraManager.Instance.Update(playerCenter, mapColumns, mapRows, tileWidth, gameTime, smoothing)`
- Use **stable 32x32 anchor** from player position to avoid animation jitter (not tight sprite bounds)
- View matrix: `CameraManager.Instance.GetViewMatrix()` for world rendering

### Dungeon System
Two implementations of `DungeonManager`:
1. `ProceduralDungeonManager` - generates dungeons with autotiling
2. `AutoLootDungeonManager` - adds automatic loot distribution

Dungeon elements (switches, levers, doors, traps, chests) register with singleton:
- Access: `DungeonManager.Instance.GetElement<DungeonSwitch>("switch_1")`
- Query: `DungeonManager.Instance.GetElements<DungeonLoot>()`
- Elements track position via `Column`, `Row` (tile coords) and `Bounds` (pixel rect)

**Integration**: Register dungeon chests as collision actors:
```csharp
var chests = DungeonManager.Instance.GetElements<DungeonLoot>();
_npcManager.CollisionManager.RegisterChests(chests);
```

### UI System (Gum)
- Gum UI system via `GumUiSystem` and `GumService.Default`
- Initialize Gum in system registration with `participatesInDraw: true`
- UI elements added to `GumService.Default.Root.Children`
- **Scene cleanup**: Call `GumService.Default.Root.Children.Clear()` in scene `Dispose()` to prevent input capture leaks

### Input Handling
`InputHandler` singleton initialized via `InputManagerInitializer.InitializeForGameScene()`:
- Registers callbacks for movement, attacks, weapon rotation, interactions
- Handles debug toggles (F1-F10 keys)
- Movement callback should delegate to `Player.Move()` with collision parameters

### Combat System
`CombatManager` singleton handles projectile/melee combat:
- Initialize with player, NPC manager, effects manager
- Manages projectile lifecycle, collision detection
- Weapon stats loaded from `DataManager.Instance.GetWeaponStats()`
- `WeaponManager` handles weapon equipping: `weaponManager.EquipWeapon(player, weapon)`

### Rendering & Theme System
Post-processing shader: `shaders/PostHorror.fx` (desaturate, vignette, grain)
- Toggle with F10 key at runtime
- Theme config: `ThemeConfig.cs` - adjust `Desaturate`, `Vignette`, `Grain` values
- Render pipeline: Scene → RenderTarget2D → Post-process → Screen

### Debug Tools
Built-in debug overlays toggled with F1-F7:
- F1: Debug UI (FPS, camera position)
- F2: Grid overlay
- F3: AI debug info
- F4: Weapon hitboxes
- F5: Tileset viewer
- F6: Tile coordinates
- F7: Collision bounds

Enable debug logging: `Log.EnabledAreas |= LogArea.Combat | LogArea.Player`

## Code Conventions

### Style (from CODE_GUIDELINES.md)
- PascalCase for classes/methods, camelCase for variables
- Singular folder names: `Entity/`, `Manager/` (not plural)
- Explicit access modifiers always
- 4-space indentation, braces on new lines
- Use XML doc comments for public APIs: `/// <summary>`

### Patterns
- **Composition over inheritance**: Use component pattern for entities
- **Dependency injection**: Inject managers/services via constructors
- **Event-driven**: Subscribe/unsubscribe events explicitly
- **No magic numbers**: Define constants or load from data files

### Data-Driven Design
- Store content in JSON/XML under `Content/data/`
- Parse at runtime via `DataManager`
- Separate game logic from content for iteration speed

## Common Tasks

### Adding a New Game System
1. Implement `IGameSystem` interface
2. Register in `GameBootstrapper.InitializeAll()` in dependency order
3. Add `participatesInDraw: true` if system needs Draw() call

### Creating a New Character Type
1. Extend `Character` base class
2. Add specialized components (inherit from base components)
3. Register with `CollisionWorldManager` via `RegisterPlayer()` or `RegisterNpc()`
4. Call `character.CollisionComponent.SetCollisionWorld(collisionWorld)`

### Adding New Data
1. Create JSON file in appropriate `Content/data/` subfolder
2. Add loading logic to `DataManager` (follow existing patterns)
3. Add getter method: `public T GetXData(string id)`
4. Access via `DataManager.Instance.GetXData("id")`

### Creating a New Dungeon Element
1. Extend `DungeonElement` base class
2. Implement interaction logic (Activate, Update, Draw)
3. Register with `DungeonManager.Instance.RegisterElement(element)`
4. For physical objects (chests), create collision actor and register with `CollisionWorldManager`

## Critical Gotchas

1. **System Order**: Register systems in `GameBootstrapper` respecting dependencies (AssetSystem before others using assets)
2. **Collision Setup**: Always call `SetCollisionWorld()` on character collision components after registration
3. **Scene Cleanup**: Clear `GumService.Default.Root.Children` in scene `Dispose()` to prevent input issues
4. **Camera Jitter**: Use fixed 32x32 anchor from player position, not animated sprite bounds
5. **NaN Validation**: Player movement validates for NaN coordinates (due to physics edge cases) - follow this pattern for new movers
6. **Draw Order**: DrawWorld() for camera-transformed, DrawUI() for screen-space - never mix
7. **Physics Scale**: Aether physics uses 64 pixels = 1 meter conversion (handled by `CollisionWorld`)
8. **Weapon Bounds**: Only use character body bounds for movement collision, never weapon sprite bounds

## Reference Files
- Architecture: `GameBootstrapper.cs`, `SystemManager.cs`
- Physics: `CollisionWorld.cs`, `CharacterCollisionComponent.cs`
- Data: `DataManager.cs`, `ConfigurationManager.cs`
- Example scene: `GameScene.cs`
- Combat: `CombatManager.cs`, `WeaponManager.cs`
- Story systems guide: `StoryScriptingGuide.md` (dialogue, quests, JSON formats)

## Build & Run
```powershell
dotnet build Hearthvale.sln
dotnet run --project Hearthvale.csproj
```

MonoGame Content Pipeline builds assets from `Content/Content.mgcb` automatically.
