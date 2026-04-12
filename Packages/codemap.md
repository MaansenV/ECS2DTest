# Packages/

Unity package boundary for the project: `Packages/manifest.json` declares package dependencies and local UPM sources, while `Packages/packages-lock.json` pins resolved versions for deterministic restores.

## Responsibility

Own dependency resolution for the Unity project and host the local reusable package `Packages/com.ecs2d.renderer`.

## Design

Uses Unity Package Manager layout: project-level manifest/lockfile at the root, plus a self-contained package folder with its own runtime/editor/test assemblies and package metadata.

## Flow

Editor/Unity resolves `manifest.json` -> locks versions in `packages-lock.json` -> loads `com.ecs2d.renderer` as a local UPM package into the project assembly graph.

## Integration

Integrates with the rest of the project through UPM references; `com.ecs2d.renderer` is consumed by scenes, scripts, and tests without being flattened into `Assets/`.
