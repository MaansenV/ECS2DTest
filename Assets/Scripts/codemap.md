# Assets/Scripts/

## Responsibility
Gameplay glue outside the reusable ECS2D package. This folder contains project-specific bridge code that adapts scene content, editor workflows, and spawn/setup logic into ECS runtime behavior.

## Design
- `Authoring/`: MonoBehaviour-facing components and baking helpers that capture scene configuration.
- `Editor/`: custom inspectors, validation, and editor-time utilities for authoring workflows.
- `System/`: ECS systems and spawn orchestration that consume baked data and drive runtime setup.

The code here should remain thin and project-specific; core rendering and simulation belong in `Packages/com.ecs2d.renderer`.

## Flow
1. Designers place GameObjects and configure authoring components in the scene.
2. Editor tooling validates and serializes those settings into ECS-ready data during baking.
3. Spawn systems read the baked data, create or initialize entities, and connect them to the runtime package systems.
4. ECS systems then execute rendering, animation, culling, and related behavior using the shared package infrastructure.

## Integration
- Bridges GameObject scene content to ECS entities and components.
- Coordinates with package runtime systems through baked references, spawn payloads, and initialization state.
- Keeps editor-facing authoring separate from runtime execution so project logic stays lightweight and maintainable.
