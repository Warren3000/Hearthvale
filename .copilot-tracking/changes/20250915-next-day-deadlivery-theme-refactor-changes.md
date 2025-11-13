<!-- markdownlint-disable-file -->
# Changes: Next Day Deadlivery Theme Refactor

Date: 2025-09-15

This file tracks incremental changes for implementing the plan in .copilot-tracking/plans/20250915-next-day-deadlivery-theme-refactor-plan.instructions.md.

## Phase 1: Visual Pipeline and Theme Config

Status: Started

### Task 1.1: Add and compile PostHorror.fx; register in Content.mgcb

Planned actions:
  - Create `Hearthvale/Content/shaders/PostHorror.fx` using the shader from research (vignette, grain, desaturation).
  - Add MGCB entries to `Hearthvale/Content/Content.mgcb` under a new `#begin shaders/PostHorror.fx` block with EffectImporter/EffectProcessor.
  - Ensure Content folder structure includes `shaders/`.

Verification:
  - Build content; confirm `Content.Load<Effect>("shaders/PostHorror")` will resolve.
  - No warnings/errors in MGCB output.

Notes:
  - MGCB currently includes multiple images and fonts; adding a shader block is non-breaking.
  - Keep sampler filter as PointClamp to match pixel art.

Status: Done
Changes:
  - Added [Hearthvale/Content/shaders/PostHorror.fx]
  - Appended MGCB entry in [Hearthvale/Content/Content.mgcb]
Validation:
  - MGCB file updated; build to verify effect compilation in next run

### Task 1.2: Integrate render target + post-processing effect in Game1

Planned actions:
  - Add fields in `Game1` for `RenderTarget2D _sceneTarget; Effect _postEffect;`.
  - In `LoadContent`, allocate `_sceneTarget` based on backbuffer size and load `_postEffect` from `shaders/PostHorror`.
  - In `Draw`, wrap base drawing with render target:
    1) `GraphicsDevice.SetRenderTarget(_sceneTarget)`; clear; call `base.Draw(gameTime)` to render world+UI into the target (Core handles SpriteBatch passes).
    2) Reset to backbuffer; set effect params (Resolution, DesaturateAmount, VignetteIntensity, GrainIntensity).
    3) `SpriteBatch.Begin(..., effect: _postEffect);` draw `_sceneTarget` full-screen; `End()`.

Verification:
  - Visual effect clearly visible; toggling parameters changes presentation.
  - No double-draw artifacts; no state leakage (ensure SetRenderTarget(null) before final pass).

Notes:
  - Core.cs already performs two SpriteBatch passes (world with camera, then UI). Wrapping via Game1.Draw before/after `base.Draw` avoids altering Core library code.
  - Use `SamplerState.PointClamp` for crisp presentation.

Status: Done
Changes:
  - Updated [Hearthvale/Game1.cs] to render to offscreen target and present with effect
  - Loads effect in `LoadContent`; safe fallback when content not built
Validation:
  - Requires run to visually confirm effect

### Task 1.3: Create ThemeConfig and wire parameters/colors

Planned actions:
  - Add `Hearthvale/GameCode/Rendering/ThemeConfig.cs` with properties (Desaturate, Vignette, Grain, FogColor, UIBg, UIAccent, FontPrimary) from research.
  - Provide access via a simple static `Theme` class or register instance in `Game.Services` for consumption by UI and draw code.
  - Read ThemeConfig values when setting effect parameters in Game1.

Verification:
  - Changing ThemeConfig values affects runtime visuals without code changes.
  - UI can access ThemeConfig colors in later phases.

Notes:
  - Keep initial defaults subtle to preserve readability; allow runtime tweak later via debug console if available.

Status: Done
Changes:
  - Added [Hearthvale/GameCode/Rendering/ThemeConfig.cs] with defaults and Theme.Current accessor
  - Game1 uses ThemeConfig for effect parameters
Validation:
  - On run, tuning ThemeConfig should change effect intensity

## Running Notes

Phase 1 complete; Phase 2 partially complete (branding only).

### Solution-Wide Context Added
- Expanded research document with full solution structure analysis
- Documented MonoGameLibrary architecture (Core, Graphics, Scenes, Input, Audio)
- Documented HearthvaleTest suite (11 test files, xUnit-based, mocking patterns)
- Added cross-project compatibility validation steps
- Identified static property shadowing in Core class and documented workaround

Next steps: Continue Phase 2 (assets/atlases) or Phase 3 (UI/narrative).

## Phase 2: Branding and Asset Pathing

Status: In Progress

### Task 2.1 (Partial): Branding updates

- Window title changed to "Next Day Deadlivery" in [Hearthvale/Game1.cs]
- Added runtime toggle (F10) for post-processing to aid testing and screenshots
- Added [Hearthvale/ThemeNotes.md] with quick guidance
- Fixed Core.GraphicsDevice static shadowing issues by casting to base Game

Pending:
- AssemblyName/RootNamespace staging
- Icon replacement

### Solution Context Integration (Completed)

- Analyzed MonoGameLibrary shared library structure
- Analyzed HearthvaleTest unit test coverage and patterns
- Updated research document with solution-wide architecture details
- Documented cross-project validation requirements

