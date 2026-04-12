# Packages/com.ecs2d.renderer/

## Responsibility

Reusable Unity UPM package for ECS2D sprite rendering. Owns the runtime rendering/animation/culling pipeline, authoring components, Editor tooling, tests, and package metadata (`package.json`, asmdefs, docs).

## Design

Split into `Runtime/` for ECS systems, components, authoring, and runtime assets, and `Editor/` for custom inspectors and editor-only UX. The package is assembly-bounded (`ECS2D.Rendering.asmdef`, `ECS2D.Rendering.Tests.asmdef`) and keeps runtime APIs deterministic for bake/runtime verification.

## Flow

Authoring components bake sprite and animation data into ECS entities; runtime systems ingest transforms, advance animation, cull visibility, and batch draw sprites. Editor code supports configuration and inspection, while EditMode tests validate baking and runtime behavior.

## Integration

Consumed by scenes and project glue through the package API, with runtime assets under `Runtime/Resources`. It integrates with Unity Entities, the Editor, and package-level documentation/metadata to deliver a reusable rendering module.
