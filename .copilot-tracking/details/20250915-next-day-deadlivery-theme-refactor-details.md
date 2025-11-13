<!-- markdownlint-disable-file -->
# Task Details: Next Day Deadlivery Theme Refactor

## Research Reference

**Source Research**: #file:../research/20250915-next-day-deadlivery-theme-refactor-research.md

## Phase 1: Visual Pipeline and Theme Config

### Task 1.1: Add and compile PostHorror.fx; register in Content.mgcb

Create a new HLSL effect `Hearthvale/Content/shaders/PostHorror.fx` implementing vignette, film grain, and desaturation. Add it to `Content.mgcb` with EffectImporter/EffectProcessor entries so it builds with the pipeline.

- Files:
  - Hearthvale/Content/shaders/PostHorror.fx - New post-processing shader
  - Hearthvale/Content/Content.mgcb - Add build entries for the shader
- Success:
  - MGCB builds without errors; `Content.Load<Effect>("shaders/PostHorror")` succeeds at runtime
  - Shader parameters can be adjusted (Desaturate, Vignette, Grain)
- Research References:
  - #file:../research/20250915-next-day-deadlivery-theme-refactor-research.md (Lines 63-126) - Shader code and MGCB snippet
  - #fetch:https://docs.monogame.net/articles/graphics/effects.html - Effect pipeline usage
- Dependencies:
  - MGCB configured; Content project references valid

### Task 1.2: Integrate render target + post-processing effect in Game1

Wrap the existing draw pipeline to render to a `RenderTarget2D`, then draw to backbuffer using `SpriteBatch` with the `PostHorror` effect. Load the effect and create the render target in `LoadContent`.

- Files:
  - Hearthvale/Game1.cs - Add fields, load effect, allocate render target, wrap Draw
- Success:
  - Visual effect is visible in-game; toggling parameters changes the presentation
  - No regression to world/UI drawing order
- Research References:
  - #file:../research/20250915-next-day-deadlivery-theme-refactor-research.md (Lines 128-169) - C# integration example and notes
  - #fetch:https://docs.monogame.net/api/Microsoft.Xna.Framework.Graphics.RenderTarget2D.html - RenderTarget usage
- Dependencies:
  - Task 1.1 completion

### Task 1.3: Create ThemeConfig and wire parameters/colors

Add a `ThemeConfig` class to centralize effect parameters and UI colors; expose via a service or static for minimal intrusion. Use in Game1 and UI rendering to drive consistent theme.

- Files:
  - Hearthvale/GameCode/Rendering/ThemeConfig.cs - Theme values (colors, effect params, font name)
- Success:
  - Game reads parameters from ThemeConfig to configure the effect
  - UI uses ThemeConfig colors and fonts
- Research References:
  - #file:../research/20250915-next-day-deadlivery-theme-refactor-research.md (Lines 170-193) - ThemeConfig code and usage notes
- Dependencies:
  - Task 1.2 completion (to apply parameters during draw)

## Phase 2: Branding and Asset Pathing

### Task 2.1: Update window title, display names, and icon to "Next Day Deadlivery"

Update the window title string in `Game1` or `Program` initialization. Update application icon references if used (e.g., `Hearthvale/Icon.ico`). Optionally stage namespace change by first updating `AssemblyName` to `NextDayDeadlivery` while maintaining code namespaces.

- Files:
  - Hearthvale/Game1.cs or Hearthvale/Program.cs - Set window title
  - Hearthvale/Hearthvale.csproj - AssemblyName/RootNamespace (staged)
  - Hearthvale/Icon.ico - Replace with new icon
- Success:
  - Title bar shows "Next Day Deadlivery"
  - Build outputs use new assembly name if staged
- Research References:
  - #file:../research/20250915-next-day-deadlivery-theme-refactor-research.md (Lines 227-233) - Branding/naming guidance
- Dependencies:
  - Phase 1 completion

### Task 2.2: Add/rename assets and atlases; update MGCB and atlas-configs

Introduce new images/audio/fonts matching the new theme. Update `atlas-configs` to include new spritesheets for the courier and props. Re-run AtlasGenerator to produce atlases and ensure MGCB references the generated textures/metadata.

- Files:
  - Hearthvale/Content/images/** - New/renamed art assets
  - Hearthvale/Content/audio/** - Ambient and stinger audio
  - Hearthvale/Content/fonts/** - Updated/added SpriteFonts
  - Hearthvale/Content/atlas-configs/*.json - Update to include new assets
  - Hearthvale/Content/Content.mgcb - Ensure new assets are built
- Success:
  - Atlases are generated; game loads updated textures without missing content
  - Asset paths in code match MGCB logical names
- Research References:
  - #file:../research/20250915-next-day-deadlivery-theme-refactor-research.md (Lines 194-206) - Asset strategy and atlas notes
- Dependencies:
  - Task 2.1 (branding assets may include icon)

## Phase 3: UI Retheme and Narrative Text

### Task 3.1: Update UI colors/fonts to ThemeConfig-driven values

Refactor UI drawing to consume `ThemeConfig` colors and primary font. Replace fantasy-themed textures with grungy panels if used; ensure high contrast readability.

- Files:
  - Hearthvale/GameCode/UI/**/*.cs - Apply ThemeConfig values where colors/fonts are defined
- Success:
  - UI visuals reflect the new theme; font assets load; no readability regression
- Research References:
  - #file:../research/20250915-next-day-deadlivery-theme-refactor-research.md (Lines 207-213) - UI and font specifications
- Dependencies:
  - Phase 2 completion (fonts/assets available)

### Task 3.2: Update narrative strings and display names to delivery/horror tone

Update in-game text (quest titles, item names, tooltips) to the new theme. Prefer non-breaking changes by keeping internal IDs the same and changing only player-facing strings.

- Files:
  - Hearthvale/GameCode/Data/** - String tables or data-driven text
  - Hearthvale/GameCode/** - Any hard-coded UI/narrative strings
- Success:
  - Text consistently uses the new lexicon (Courier, Outpost, Credits, etc.)
- Research References:
  - #file:../research/20250915-next-day-deadlivery-theme-refactor-research.md (Lines 255-263) - Glossary mapping
- Dependencies:
  - Task 3.1 completion

## Phase 4: Tests, Docs, and Validation

### Task 4.1: Update tests that assert text; keep behavioral tests unchanged

Search tests for string assertions tied to old theme and update expectations. Do not alter behavioral logic; ensure gameplay tests still pass.

- Files:
  - HearthvaleTest/**/*.cs - Update only where string expectations are asserted
- Success:
  - All tests pass; only expected text changes were updated
- Research References:
  - #file:../research/20250915-next-day-deadlivery-theme-refactor-research.md (Lines 239-241, 250-253) - Tests guidance and validation steps
- Dependencies:
  - Phase 3 completion

### Task 4.2: Update README and StoryScriptingGuide references; add a Theme Toggle section

Refresh documentation to reflect the new theme. Optionally document a runtime toggle or config flag to disable post-processing for performance.

- Files:
  - Hearthvale/README.md (if present) and Hearthvale/StoryScriptingGuide.md - Update terminology and screenshots
  - Hearthvale/CODE_GUIDELINES.md - Add note on ThemeConfig usage (optional)
- Success:
  - Docs reference "Next Day Deadlivery" and explain ThemeConfig and post-processing
- Research References:
  - #file:../research/20250915-next-day-deadlivery-theme-refactor-research.md (Lines 225-236, 245-253) - Implementation steps and validation
- Dependencies:
  - Task 4.1 completion

## Dependencies

- MonoGame Content Pipeline (MGCB)

## Success Criteria

- Post-processing shader compiled and applied via render target pipeline
- ThemeConfig drives visual parameters and UI colors/fonts
- Assets and atlases updated and loading; no content errors
- Tests pass; documentation updated for the new theme
