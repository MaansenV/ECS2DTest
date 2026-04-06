# ECS2D Renderer

<p align="center">
  <img src=".github/ecs2d-banner.svg" alt="ECS2D Renderer Banner" width="100%" />
</p>

<p align="center">
  Unity ECS 2D sprite rendering with animation, culling, and UPM-based reuse.
</p>

<p align="center">
  <img src="https://img.shields.io/badge/Unity-6000.3%2B-000000?style=for-the-badge&logo=unity" alt="Unity 6000.3+" />
  <img src="https://img.shields.io/badge/Entities-1.4.x-2D7DD2?style=for-the-badge" alt="Entities 1.4.x" />
  <img src="https://img.shields.io/badge/Package-UPM-00A67E?style=for-the-badge" alt="UPM Package" />
</p>

## Install

Unity Package Manager Git URL:

```text
https://github.com/MaansenV/ECS2DTest.git?path=/Packages/com.ecs2d.renderer
```

Or in `manifest.json`:

```json
{
  "dependencies": {
    "com.ecs2d.renderer": "https://github.com/MaansenV/ECS2DTest.git?path=/Packages/com.ecs2d.renderer"
  }
}
```

## What it does

- Renders ECS sprites through `SpriteSystem`
- Supports grid-based sprite-sheet animation through `SpriteAnimationSystem`
- Supports orthographic frustum culling through `SpriteCullingSystem`
- Provides a DOTS-based particle system with curve-driven speed and scale via LUTs
- Ships as a reusable package in `Packages/com.ecs2d.renderer`

## Core assets and types

**Sprite rendering and animation:**
- `SpriteSheetDefinition`
- `SpriteDataAuthoring`
- `SpriteAnimationSetDefinition`
- `SpriteAnimationAuthoring`
- `SpriteCullingSettingsAuthoring`
- `EntitiesReferenceAuthoring`

**Particle system:**
- `ParticleEmitterAuthoring`
- `ParticleCurveTypes` (curve LUT support via `CurveBlobLUT` and `ParticleCurveMode`)
- `ParticleActiveSimulationSystem`, `ParticleEmissionSystem`, `ParticleEmitterCleanupSystem`

**Editor tools ( ECS2D.Rendering.Editor):**
- Custom inspectors for all authoring components
- Shared USS stylesheet for consistent inspector styling

Namespace:

```csharp
using ECS2D.Rendering;
```

## Documentation

For a detailed guide with short code snippets on how everything is used, check out the **[DOCUMENTATION.md](DOCUMENTATION.md)**.

Release automation for agents and maintainers lives in **[docs/release-workflow.md](docs/release-workflow.md)**.

## Release

To release a new version of `com.ecs2d.renderer`:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\release-package.ps1 -Version <VERSION> -Push
```

This validates the version, bumps the package version, creates a commit with an annotated tag, and pushes to GitHub.

## Quick usage

1. Install the package.
2. Create a `SpriteSheetDefinition` for your material and texture.
3. Add `SpriteDataAuthoring` to a prefab or GameObject and assign that sheet.
4. If you want animation, create a `SpriteAnimationSetDefinition` and add `SpriteAnimationAuthoring`.
5. If you want runtime culling control, add `SpriteCullingSettingsAuthoring` in the scene.
6. Enter Play Mode and let the ECS systems animate, cull, and render your sprites.

## Included examples

- `Assets/Scenes/SampleScene.unity`
- `Assets/Scenes/SpawnBenchmark.unity`
- `Assets/Prefabs/SwordMan/SwordMan.prefab`

## Requirements

- Unity `6000.3.10f1` or newer
- `com.unity.entities` `1.4.x`

## More details

Detailed package documentation lives in:

`Packages/com.ecs2d.renderer/README.md`
