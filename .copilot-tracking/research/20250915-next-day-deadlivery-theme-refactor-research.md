<!-- markdownlint-disable-file -->
# Research: Next Day Deadlivery Theme Refactor (Survival Horror Delivery)

Date: 20250915

This document compiles verified findings, project structure analysis, concrete code examples, and implementation guidance to refactor the Hearthvale game into a survival horror themed project titled "Next Day Deadlivery" with inspiration from Fallout and Death Stranding while preserving core gameplay systems.

## 1) Tool Usage Documentation with Verified Findings

- MonoGame (current engine)
  - Project uses MonoGame, with `Game1.cs` as the root game class in `Hearthvale/`.
  - Content pipeline is managed via `.mgcb` at `Hearthvale/Content/Content.mgcb`.
  - Tiled integration via `TiledSharp.dll` and `MonoGame.Extended.Content.Pipeline.dll` present in `Hearthvale/ContentPipeline/`.
- Atlas generation tools
  - Custom `AtlasGenerator` and `AtlasGeneratorUI` projects exist for spritesheet/atlas management.
  - Build target `build/AtlasGeneration.targets` suggests MSBuild integration for atlases.
- Test suite
  - `HearthvaleTest/` contains unit tests (e.g., `PlayerTests.cs`, `CombatManagerTests.cs`), indicating behavior coverage to keep stable during refactor.
- Asset layout
  - Assets live under `Hearthvale/Content/` with subfolders: `audio/`, `fonts/`, `images/`, `Tilesets/`, etc.
  - Tiled project files at `Hearthvale/Content/Hearthvale.tiled-project` and workspace level `Tiled/` directory.

Verified implications for refactor:
- The theme refactor must preserve content pipeline references in `.mgcb` and update asset paths consistently.
- The atlas generation configs in `Hearthvale/Content/atlas-configs/` will need updates if sprite naming/pathing changes.
- Post-processing (vignette, grain, desaturation) can be implemented with MonoGame Effects (.fx) compiled via MGCB and applied in `Game1.Draw` pipeline.

## 2) Project Structure Analysis and Patterns

### Solution Structure (Hearthvale.sln)
- `Hearthvale/` (main game): WindowsDX MonoGame project with gameplay, scenes, rendering, systems
- `MonoGameLibrary/` (shared library): DesktopGL MonoGame library with engine utilities
- `HearthvaleTest/` (unit tests): xUnit test project for validating gameplay behaviors
- `AtlasGenerator/` & `AtlasGeneratorUI/`: Console and WinForms tools for sprite atlas preprocessing

### Hearthvale Project Architecture
- **GameCode/Bootstrap/**: `GameBootstrapper` orchestrates manager/system initialization
- **GameCode/Managers/**: Organized by domain
  - `Combat/`: CombatManager, WeaponManager, CombatEffectsManager
  - `NPC/`: NpcManager
  - `Camera/`: CameraManager, MapManager
  - `Dungeon/`: DungeonManager, ProceduralDungeonManager, TilesetManager
  - `UI/`: GameUIManager, DialogManager
  - `Core/`: ScoreManager, DataManager, ConfigurationManager, SingletonManager
  - `Collision/`: CollisionWorldManager
- **GameCode/Systems/**: IGameSystem implementations managed by SystemManager
  - AssetManagerSystem, AudioSystem, InputSystem, GumUiSystem, LoggingSystem, SpriteAnalysisSystem
- **GameCode/Entities/**: Player, NPCs, Projectile, Character (base)
- **GameCode/Scenes/**: Scene implementations (GameScene, TitleScene)
- **GameCode/Rendering/**: ThemeConfig (newly added), rendering helpers
- **GameCode/UI/**: UI components and managers
- **GameCode/Tools/**: AtlasConfigManager for atlas integration
- **GameCode/Data/**, **GameCode/Serialization/**, **GameCode/Collision/**, **GameCode/Dungeon/**: supporting subsystems

### MonoGameLibrary Project (Shared Engine)
- **Core.cs**: Base Game class with static/instance GraphicsDevice, SpriteBatch, Content, Input, Audio
  - Provides Scene system with DrawWorld/DrawUI separation and camera transform support
  - Static property shadowing requires cast to base Game when accessing instance properties
- **Graphics/**: 
  - `TextureAtlas`: XML-based atlas loading with region/animation support
  - `Animation`: Frame-based animation with delay
  - `Sprite`, `AnimatedSprite`: drawable sprite primitives
  - `TextureRegion`: sub-texture definition
  - `Tilemap`, `Tileset`: tile-based map rendering
  - `SpriteBatchExtensions`: helper extensions
- **Scenes/**: 
  - `Scene` (abstract): base with Initialize, LoadContent, Update, Draw, DrawWorld, DrawUI
  - `ICameraProvider`: interface for scenes providing camera matrix
- **Input/**: `InputManager`, `KeyboardInfo`, `MouseInfo`, `GamePadInfo`
- **Audio/**: `AudioController` for sound/music management

### HearthvaleTest Project (Unit Tests)
- **Test Coverage Areas**:
  - `CombatManagerTests`: attack cooldowns, projectile registration, damage mechanics, NPC collision
  - `PlayerTests`: (empty file)
  - `PlayerAnimationTests`: animation state transitions (Theory-based)
  - `NPCTests`, `NpcSeparationTests`, `NpcIntegrationTests`: NPC behaviors, separation logic, integration
  - `CharacterTests` (EntityTests): base Character class behaviors
  - `WeaponTests`: weapon mechanics
  - `ChestCollisionTests`: chest interaction
  - `CameraManagerTests`: camera positioning and tracking
- **Test Patterns**:
  - Uses xUnit with [Fact] and [Theory] attributes
  - Creates mock GraphicsDevice, SpriteBatch, Texture2D, TextureAtlas for isolated tests
  - Tests validate gameplay behaviors without visual rendering
  - Dummy SoundEffects created with minimal audio data for audio-dependent managers

### Key Patterns and Dependencies
- **Content Pipeline**: XML-based atlas definitions loaded via `TextureAtlas.FromFile`
- **Manager Initialization**: Centralized in `GameBootstrapper.InitializeAll`, singletons via static Initialize methods
- **System Pattern**: IGameSystem interface with Update/Draw, registered in SystemManager, initialized in order
- **Scene Architecture**: Core handles two-pass rendering (world with camera, UI without), scenes override DrawWorld/DrawUI
- **Namespace Stability**: Tests and game code reference `Hearthvale.GameCode.*` and `MonoGameLibrary.*` namespaces; theme refactor must preserve public API
- **Static vs Instance**: Core exposes static members shadowing Game base class; must use base Game cast or Core.* qualification
- **Asset Loading**: Content.Load with relative paths; atlas configs point to image paths; MGCB builds all assets

### Refactor Implications
- **Preserve Test Suite**: All tests in HearthvaleTest must pass; only update text assertions, never behavioral logic
- **MonoGameLibrary Stability**: Shared library used by multiple projects; avoid breaking changes to public API
- **Manager/System Coordination**: Theme changes must integrate cleanly with existing manager initialization flow
- **Atlas Tooling**: New assets require atlas config JSON updates and AtlasGenerator execution
- **Namespace Consistency**: Keep `Hearthvale.*` namespaces or update systematically across solution including tests

## 3) External Source Research with Concrete Implementation Examples

Authoritative documentation and examples (key excerpts, see links):
- MonoGame Effects (HLSL) and applying `Effect` in SpriteBatch
  - Official Docs: https://docs.monogame.net/articles/graphics/effects.html
  - MGCB Content Pipeline: https://docs.monogame.net/articles/tools/mgcb_editor.html
- Tiled and MonoGame.Extended Content Pipeline
  - MonoGame.Extended Tiles and Content: https://monogameextended.net/docs/features/tiled-maps/
- General SpriteBatch and render targets
  - RenderTarget2D usage: https://docs.monogame.net/api/Microsoft.Xna.Framework.Graphics.RenderTarget2D.html

Relevant patterns confirmed by sources:
- Compile .fx with MGCB, load via `Content.Load<Effect>("path")`, set parameters, and use with `SpriteBatch.Begin(effect: effect)`.
- Use `RenderTarget2D` for full-screen post-processing pipeline.
- Tiled maps content are built via `MonoGame.Extended.Content.Pipeline` and loaded using `Content.Load<TiledMap>(...)`.

## 4) Concrete Code Examples and Specifications

### 4.1 Post-Processing Effect: Vignette, Grain, Desaturation (HLSL)

File: `Hearthvale/Content/shaders/PostHorror.fx` (to add via MGCB)

```
// PostHorror.fx - simple vignette + grain + desaturation
// Technique Name: PostHorror

texture SceneTexture;
sampler SceneSampler = sampler_state
{
    Texture = <SceneTexture>;
    MinFilter = Point; MagFilter = Point; MipFilter = Point;
    AddressU = Clamp; AddressV = Clamp;
};

float DesaturateAmount = 0.35;   // 0..1
float VignetteIntensity = 0.45;  // 0..1
float GrainIntensity = 0.05;     // 0..1
float2 Resolution = float2(1280, 720);

float rand(float2 co) {
    return frac(sin(dot(co, float2(12.9898,78.233))) * 43758.5453);
}

float4 PS(float2 texCoord : TEXCOORD0) : COLOR0
{
    float4 color = tex2D(SceneSampler, texCoord);

    // Desaturate
    float gray = dot(color.rgb, float3(0.299, 0.587, 0.114));
    color.rgb = lerp(color.rgb, gray.xxx, DesaturateAmount);

    // Vignette
    float2 pos = texCoord - 0.5;
    float dist = dot(pos, pos) * 2.0; // 0 center .. ~1 corners
    float vignette = saturate(1.0 - dist * VignetteIntensity);
    color.rgb *= vignette;

    // Grain
    float noise = rand(texCoord * Resolution + frac(Resolution.xy));
    color.rgb = saturate(color.rgb + (noise - 0.5) * GrainIntensity);

    return color;
}

technique PostHorror
{
    pass P0
    {
        PixelShader = compile ps_3_0 PS();
    }
}
```

MGCB entry example (Content.mgcb):

```
#begin shaders/PostHorror.fx
/importer:EffectImporter
/processor:EffectProcessor
/build:shaders/PostHorror.fx
#end
```

### 4.2 C# Integration of Post-Processing Pipeline

Add to `Game1`:

```
// Fields
RenderTarget2D _sceneTarget;
Effect _postEffect;

protected override void LoadContent()
{
    // ... existing content loads ...
    var pp = GraphicsDevice.PresentationParameters;
    _sceneTarget = new RenderTarget2D(GraphicsDevice, pp.BackBufferWidth, pp.BackBufferHeight, false, pp.BackBufferFormat, DepthFormat.None);
    _postEffect = Content.Load<Effect>("shaders/PostHorror");
}

protected override void Draw(GameTime gameTime)
{
    // 1) Render scene to offscreen target
    GraphicsDevice.SetRenderTarget(_sceneTarget);
    GraphicsDevice.Clear(Color.Black);
    // draw world/UI with existing SpriteBatch as usual
    // ... existing draw calls ...

    // 2) Present with post-processing
    GraphicsDevice.SetRenderTarget(null);
    _postEffect.Parameters["Resolution"]?.SetValue(new Vector2(_sceneTarget.Width, _sceneTarget.Height));
    _postEffect.Parameters["DesaturateAmount"]?.SetValue(0.35f);
    _postEffect.Parameters["VignetteIntensity"]?.SetValue(0.45f);
    _postEffect.Parameters["GrainIntensity"]?.SetValue(0.05f);

    _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.PointClamp, null, null, _postEffect);
    _spriteBatch.Draw(_sceneTarget, new Rectangle(0, 0, _sceneTarget.Width, _sceneTarget.Height), Color.White);
    _spriteBatch.End();
}
```

Notes:
- Use `SpriteSortMode.Immediate` for effects applied during SpriteBatch.
- If the project already uses multiple render targets or camera matrices, integrate the pre-pass as a wrapper around the existing draw pipeline.

### 4.3 Central Theme Configuration and Colors

Create a central configuration for theme parameters so gameplay systems remain unchanged while visuals/audio shift.

```
// File: Hearthvale/GameCode/Rendering/ThemeConfig.cs
namespace Hearthvale.GameCode.Rendering
{
    public class ThemeConfig
    {
        public float Desaturate { get; set; } = 0.35f;
        public float Vignette { get; set; } = 0.45f;
        public float Grain { get; set; } = 0.05f;

        public Microsoft.Xna.Framework.Color FogColor { get; set; } = new Microsoft.Xna.Framework.Color(18, 22, 22);
        public Microsoft.Xna.Framework.Color UIBg { get; set; } = new Microsoft.Xna.Framework.Color(12, 14, 16, 220);
        public Microsoft.Xna.Framework.Color UIAccent { get; set; } = new Microsoft.Xna.Framework.Color(180, 180, 120);
        public string FontPrimary { get; set; } = "fonts/retro_small";
    }
}
```

Use dependency access via `Game.Services` or a simple static singleton to reduce invasive changes.

### 4.4 Asset Renaming Strategy and Atlas Generation

Guidelines:
- Keep folder structure but introduce new top-level theme folders where needed:
  - `Content/images/characters/` -> rename sprites to courier/scavenger naming.
  - `Content/images/props/` -> add post-apocalyptic props (barrels, crates, signs).
  - `Content/audio/ambience/` -> wind, distant thunder, Geiger clicks.
- Update `atlas-configs/*.json` to point to new assets; re-run AtlasGenerator.

Batching with AtlasGenerator (example concept):
- Define `atlas-configs/courier.json` to include `images/characters/courier/*` into a spritesheet.
- Ensure MGCB builds the generated atlas texture and JSON/region metadata.

### 4.5 UI Retheme and Fonts

Specifications:
- Replace fantasy UI panels with grungy panels; darker panels with high-contrast pale accent.
- Font: consider a pixel/bitmap font with utilitarian feel; keep legibility.
- Ensure `SpriteFont` assets are updated in MGCB and used by UI draw code.

### 4.6 Audio Direction

Guidelines:
- Reduce melodic music; emphasize ambient soundscapes.
- Add low-volume wind rumble loop; occasional stingers on threat detection.
- Simulate radio/comms beeps for delivery updates.

Implementation tips:
- Use `SoundEffectInstance.IsLooped = true` for ambience.
- For "muffled indoors" effect, use alternate samples or EQ’d assets; MonoGame lacks built-in DSP filters.

## 5) Implementation Guidance (Step-by-step)

1. Branding and Naming
   - Project title: update window title, splash text, icons to "Next Day Deadlivery".
   - Namespace: progressively rename from `Hearthvale` to `NextDayDeadlivery` (or keep root namespace and set `AssemblyName`/`RootNamespace` to minimize churn initially). Prefer staged rename.
2. Visual Pipeline
   - Add `PostHorror.fx`, register in MGCB, load in `Game1`, wrap draw with render target + effect.
   - Add `ThemeConfig` to centralize effect parameters and UI colors.
3. Assets and Atlases
   - Introduce new images/audio; update `atlas-configs` and run AtlasGenerator; update code asset paths.
4. UI and Text
   - Update UI colors and font paths; update narrative strings (quests, item names) to delivery/horror tone.
5. Gameplay Nouns
   - Rename class display names and enums (only where used in text) e.g., Player -> Courier (display), Currency -> Credits, Potions -> Supplies.
6. Tests and Docs
   - Update tests asserting names/strings; ensure behaviors unchanged.
   - Update README/StoryScriptingGuide with new theme references.

## 6) Risk, Edge Cases, and Validation

### Risks
- Breaking content pipeline by moving assets without updating `.mgcb` entries and atlas configs
- Effect not applied correctly with existing camera transforms; ensure post-effect only on final pass after Core.Draw
- Performance on low-end hardware with extra render target; F10 toggle added for comparisons
- Static property shadowing in Core class causing NullReference or compile errors; must cast to base Game or use Core.* qualification
- MonoGameLibrary API changes breaking shared library contract or HearthvaleTest mocks
- Test failures from string assertion changes (NPC names, item names, UI text) that weren't updated
- Manager/System initialization order dependencies broken by new theme systems

### Edge Cases
- GraphicsDevice/Content access in LoadContent timing: base Game properties valid after Initialize, static Core members depend on Core.Initialize
- Atlas XML parsing failures if atlas-configs reference non-existent images or malformed metadata
- Scene transitions during post-effect rendering: ensure render target reset on scene change
- Multiple SpriteBatch Begin/End calls in Core.Draw: post-effect wraps entire pipeline, including world+UI passes
- Test mocks using dummy Texture2D/SpriteFont: ensure theme code gracefully handles minimal test fixtures

### Validation Steps
1. **Content Pipeline**: Build MGCB successfully with shader; verify no missing asset errors at runtime
2. **Visual Verification**: Run game with F10 toggle; confirm effect applies and can be disabled
3. **Code Audit**: Audit all `Content.Load` calls for updated paths; check atlas-configs match MGCB entries
4. **Unit Tests**: Run full HearthvaleTest suite; update only text assertions (NPC/item names), preserve all behavioral tests
5. **Solution Build**: Build entire Hearthvale.sln (all configurations); verify MonoGameLibrary, Hearthvale, HearthvaleTest, AtlasGenerator projects compile
6. **Manager Integration**: Verify GameBootstrapper.InitializeAll runs without exceptions; check singleton initialization order
7. **Performance**: Profile with/without post-effect; measure frame time impact of render target pass
8. **Cross-Project Compatibility**: Verify AtlasGenerator tools still function with updated atlas-configs

### Test-Specific Validation
- **CombatManagerTests**: Ensure dummy textures/atlases work with theme code; no effect on combat logic
- **CameraManagerTests**: Camera transforms unaffected by post-processing (applied after camera pass)
- **NPC/Player Tests**: Character display names may change (Player->Courier); update assertions only
- **WeaponTests**: Weapon asset paths may change; ensure test mocks remain valid
- All test GraphicsDevice mocks use GraphicsProfile.Reach; compatible with ps_4_0_level_9_1 shader

## 7) Glossary: Old -> New (Thematic)

- Player -> Courier
- NPC -> Scavenger / Survivor
- Town -> Outpost / Hub
- Gold -> Credits / Chits
- Potion -> Medkit / Rations
- Dungeon -> Dead Zone / Quarantine Zone
- Chest -> Cache / Drop Crate

## 8) References (URLs)

- MonoGame Effects: https://docs.monogame.net/articles/graphics/effects.html
- MGCB Editor: https://docs.monogame.net/articles/tools/mgcb_editor.html
- RenderTarget2D API: https://docs.monogame.net/api/Microsoft.Xna.Framework.Graphics.RenderTarget2D.html
- MonoGame.Extended Tiled Maps: https://monogameextended.net/docs/features/tiled-maps/
