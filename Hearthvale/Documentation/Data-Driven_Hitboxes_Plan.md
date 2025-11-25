# Data-Driven Combat Hitboxes Plan

## Goals
- Prevent attack animation silhouettes from forcing character bodies through walls or other entities.
- Keep offensive hit detection authored per attack while stabilizing the blocking collider.
- Introduce minimal tooling changes so designers can gradually enrich profiles.

## Summary
We will extend the attack profile data to describe both offensive and defensive shapes. Characters adopt a defensive body collider per attack state, while offensive hit polygons continue to drive damage and feedback. The collision component manages live collider swaps, guaranteeing consistent blocking with animation-informed shapes.

## Implementation Steps

### 1. Extend Content Schema
- Add `defensiveBodyShape` to each attack entry in `Content/data/Characters/*/AttackProfiles.json` alongside the existing `shape` definition.
- Shape fields support `type`, `width`, `height`, `radius`, `forwardOffset`, and optional `verticalOffset`. Keep `shape` (offensive) unchanged for backwards compatibility.
- Version the JSON schema (e.g., `schemaVersion: 2`) to help loaders apply defaults when defensive data is missing.

### 2. Update Runtime Profile Models
- Modify the profile loader (`GameCode/Combat/AttackProfileLoader.cs` or equivalent) so DTOs expose both `OffensiveShape` and `DefensiveBodyShape`.
- Ensure `AttackTimingProfile` caches parsed shape structs to avoid per-frame allocations.
- Provide sensible defaults when `defensiveBodyShape` is omitted (reuse the actor’s base collider).

### 3. Bind Shapes During Equipment Setup
- When weapons or attacks initialize (`WeaponSwingProfileFactory`, character setup routines), persist the loaded `DefensiveBodyShape` with the owning combat component.
- Keep offensive shape logic unchanged so hit detection remains data-driven.

### 4. Swap Body Colliders Per Combat State
- In player and NPC combat controllers, trigger defensive collider updates when entering wind-up, active, and recovery states.
- Call a new API on `CharacterCollisionComponent` (e.g., `ApplyProfileCollider(DefensiveShape shape)`) that rebuilds the body collider.
- When an attack concludes, restore the cached default collider.

### 5. Enhance `CharacterCollisionComponent`
- Track the default collider definition and currently applied profile collider.
- Provide utilities to rebuild shapes (box/capsule/ellipse) from simple descriptors.
- Integrate with `CollisionWorldManager`: remove the old actor, construct the new shape, and re-register without dropping collision events.

### 6. Align Debug Visualizations
- Update `GameCode/Managers/DebugManager.cs` to render both offensive and defensive polygons when combat debug mode is active.
- Label shapes with color coding so QA can confirm author intent (e.g., offensive = red, defensive = cyan).

### 7. Tooling and Validation
- Update content validation scripts to verify new fields and enforce required dimensions per shape type.
- Add unit tests in `HearthvaleTest` validating profile deserialization, default fallbacks, and collider swap logic.
- Create automated gameplay scenarios (integration tests or scripted harness) to spawn characters near walls while attacking and assert no overlaps occur.

### 8. Rollout Strategy
- Ship with default defensive shapes mirroring current colliders so the pipeline change is inert initially.
- Gradually author bespoke defensive shapes for problematic attacks.
- Maintain a configuration flag to disable profile-based defensive colliders for rapid rollback if needed.

## Risks and Mitigations
- **Runtime churn during collider swaps**: Cache shapes and reuse collision actors when possible; profile early with high-frequency attacks.
- **Authoring complexity**: Provide sample templates and integrate validation warnings into build scripts.
- **Player feel regressions**: Test attacks near walls and entities to tune defensive collider sizes before full release.

## Success Criteria
- Characters no longer tunnel through walls or overlap allies when executing wide attacks.
- Designers can iterate on offensive shapes without touching code.
- Combat debug overlay shows consistent alignment between data and runtime colliders.