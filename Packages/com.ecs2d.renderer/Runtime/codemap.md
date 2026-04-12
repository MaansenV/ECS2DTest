# Packages/com.ecs2d.renderer/Runtime/

## Responsibility
Runtime ECS module for 2D sprite and particle rendering. It owns the baked data, per-frame simulation systems, and final render preparation for sprite sheets, animation, culling, and particle emission.

## Design
The module is organized around ECS systems plus authoring components that bake GameObject state into runtime components. Core types include `SpriteData`, `SpriteSheetDefinition`, `SpriteSheetRenderKey`, `SpriteAnimationState`, `SpriteCullState`, and particle components/curve LUT data. `SpriteSystem` is the final draw path; `SpriteTransformSyncSystem`, `SpriteAnimationSystem`, `SpriteAnimationChangeRequestSystem`, and `SpriteCullingSystem` prepare sprite state before rendering. Particle behavior is split into spawn, active simulation, and cleanup systems, with `ParticleSpawnUtility` building curve lookup data.

## Flow
Authoring starts with `SpriteDataAuthoring`, `SpriteAnimationAuthoring`, `SpriteCullingSettingsAuthoring`, `ParticleEmitterAuthoring`, and `EntitiesReferenceAuthoring`. Baking produces runtime sprite/particle components and shared sheet/animation definitions. Each frame, transform data is synced into ECS, animation requests advance clip state, culling marks visibility, and particle emission/simulation updates active particles. The render system then consumes the resolved sprite sheet key, sort data, and visibility state to batch and draw.

## Integration
Integrates with Unity.Entities for simulation, GameObject authoring for bake-time setup, and runtime resources under `Runtime/Resources` for sprite sheets. It also depends on package tests and external scene setup to validate deterministic sprite sorting, animation progression, and safe sheet swapping through `SpriteSheetRuntime`.
