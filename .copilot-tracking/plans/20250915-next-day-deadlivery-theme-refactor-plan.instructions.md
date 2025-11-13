---
applyTo: '.copilot-tracking/changes/20250915-next-day-deadlivery-theme-refactor-changes.md'
---
<!-- markdownlint-disable-file -->
# Task Checklist: Next Day Deadlivery Theme Refactor

## Overview

Refactor Hearthvale into a survival horror delivery theme titled "Next Day Deadlivery" by updating visual pipeline, assets, UI, branding, and text while preserving core gameplay systems and tests.

## Objectives

- Introduce a post-processing pipeline (vignette, grain, desaturation) and centralized ThemeConfig to establish the new visual tone.
- Update branding, assets, UI, and narrative text to the new theme without breaking content pipeline or tests.

## Research Summary

### Project Files
- Hearthvale/Game1.cs - Central game loop where post-processing effect will be integrated
- Hearthvale/Content/Content.mgcb - Content pipeline definitions; add shader and new assets here
- Hearthvale/GameCode/Rendering/ - Location to add ThemeConfig
- Hearthvale/Content/atlas-configs/ - Atlas generator configurations to update for new sprite sets
- HearthvaleTest/*.cs - Tests to keep passing; update only string expectations where needed

### External References
- #file:../research/20250915-next-day-deadlivery-theme-refactor-research.md - Comprehensive research with code examples and specifications
- #githubRepo:"MonoGame/MonoGame SpriteBatch Effect example" - Applying Effects with SpriteBatch and RenderTarget patterns
- #fetch:https://docs.monogame.net/articles/graphics/effects.html - MonoGame Effects documentation for .fx usage

### Standards References
- #file:../../copilot/csharp.md - C# conventions used across the project
- #file:../../.github/instructions/task-implementation.instructions.md - Implementation execution standards for tasks

## Implementation Checklist

### [ ] Phase 1: Visual Pipeline and Theme Config

- [ ] Task 1.1: Add and compile PostHorror.fx; register in Content.mgcb
  - Details: .copilot-tracking/details/20250915-next-day-deadlivery-theme-refactor-details.md (Lines 12-25)

- [ ] Task 1.2: Integrate render target + post-processing effect in Game1.Draw and load in LoadContent
  - Details: .copilot-tracking/details/20250915-next-day-deadlivery-theme-refactor-details.md (Lines 28-39)

- [ ] Task 1.3: Create ThemeConfig and wire parameters/colors
  - Details: .copilot-tracking/details/20250915-next-day-deadlivery-theme-refactor-details.md (Lines 41-53)

### [ ] Phase 2: Branding and Asset Pathing

- [ ] Task 2.1: Update window title, assembly/root namespace display name, and icon to "Next Day Deadlivery"
  - Details: .copilot-tracking/details/20250915-next-day-deadlivery-theme-refactor-details.md (Lines 57-71)

- [ ] Task 2.2: Add/rename assets (images/audio/fonts), update MGCB and atlas-configs; run atlas generation
  - Details: .copilot-tracking/details/20250915-next-day-deadlivery-theme-refactor-details.md (Lines 73-89)

### [ ] Phase 3: UI Retheme and Narrative Text

- [ ] Task 3.1: Update UI colors/fonts to ThemeConfig-driven values
  - Details: .copilot-tracking/details/20250915-next-day-deadlivery-theme-refactor-details.md (Lines 93-104)

- [ ] Task 3.2: Update narrative strings and display names to delivery/horror tone
  - Details: .copilot-tracking/details/20250915-next-day-deadlivery-theme-refactor-details.md (Lines 106-118)

### [ ] Phase 4: Tests, Docs, and Validation

- [ ] Task 4.1: Update tests that assert text; keep behavioral tests unchanged
  - Details: .copilot-tracking/details/20250915-next-day-deadlivery-theme-refactor-details.md (Lines 122-133)

- [ ] Task 4.2: Update README and StoryScriptingGuide references; add a Theme Toggle section
  - Details: .copilot-tracking/details/20250915-next-day-deadlivery-theme-refactor-details.md (Lines 135-147)

## Dependencies

- MonoGame Content Pipeline (MGCB) working in the repo
- AtlasGenerator and AtlasGeneratorUI for sprite atlases

## Success Criteria

- Game builds and runs with post-processing effect applied and parameterized via ThemeConfig
- Updated assets load via MGCB; atlases generated successfully; no missing content at runtime
- UI uses new colors/fonts; text reflects the new theme
- All tests pass; only text assertion updates needed; no behavioral regressions
