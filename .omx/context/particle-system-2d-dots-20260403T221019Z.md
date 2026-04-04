Task statement
- User wants a basic, very performant 2D particle system in DOTS for the existing renderer.

Desired outcome
- Clarify a compact, execution-ready specification for a DOTS-native particle emitter/runtime that covers Shuriken-like basics: emission, lifetime, speed randomization, spawn shapes, scale/color over lifetime, and optional rotation randomization.

Stated solution
- Build a basic-scope ECS/DOTS particle system with:
- Burst Count
- Spawn Rate
- Max Particles
- Lifetime Min/Max
- Speed Min/Max
- Start Scale / End Scale
- Start Color / End Color
- Spawn Shapes: Point, Circle, Box
- Circle spawn modes: Edge, Area
- Direction modes: Outward, Random, Fixed
- Optional rotation: Start Rotation Min/Max, Rotation Speed Min/Max

Probable intent hypothesis
- User wants a lightweight, renderer-native particle path that keeps the current ECS2D renderer fast, avoids Shuriken overhead, and supports enough parameter variance to make effects feel natural.

Known facts/evidence
- Repo contains package `Packages/com.ecs2d.renderer`.
- Renderer uses `SpriteData` plus `SpriteSystem` indirect instanced drawing.
- `SpriteTransformSyncSystem` derives render state from `LocalToWorld`.
- Sample project has a `SpawnSystem` for grid spawning, but no existing particle/emitter runtime was found.

Constraints
- Deep-interview mode: clarify requirements only, no implementation in this phase.
- Brownfield repo; new system should likely align with existing ECS2D renderer data flow.
- User emphasized performance and compact Shuriken-like baseline scope.

Unknowns/open questions
- Whether particles must be implemented inside `com.ecs2d.renderer` package vs sample `Assets` first.
- Whether particles are rendered as regular sprites (`SpriteData`) or need a dedicated packed buffer/render path.
- Whether emitter configuration should be authoring/baker based, pure ECS runtime data, or both.
- Whether simulation space is local/world.
- Whether per-particle color/scale should be linearly interpolated only or support curves later.
- Whether particle effects must support looping, one-shot burst-only, or both.

Decision-boundary unknowns
- What OMX may decide alone about package placement, authoring approach, defaults, and first-pass limitations.

Likely codebase touchpoints
- `Packages/com.ecs2d.renderer/Runtime/SpriteData.cs`
- `Packages/com.ecs2d.renderer/Runtime/SpriteSystem.cs`
- `Packages/com.ecs2d.renderer/Runtime/SpriteTransformSyncSystem.cs`
- `Assets/Scripts/System/SpawnSystem.cs`
- `Assets/Scripts/System/SpawnSettings.cs`
