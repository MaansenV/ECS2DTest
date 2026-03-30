# ECS2D Test

<p align="center">
  <img src=".github/ecs2d-banner.svg" alt="ECS2D Rendering Banner" width="100%" />
</p>

<p align="center">
  A reusable Unity ECS sprite-rendering core for 2D projects, extracted into a clean UPM package.
</p>

<p align="center">
  <img src="https://img.shields.io/badge/Unity-6000.3%2B-000000?style=for-the-badge&logo=unity" alt="Unity 6000.3+" />
  <img src="https://img.shields.io/badge/Entities-1.4.x-2D7DD2?style=for-the-badge" alt="Entities 1.4.x" />
  <img src="https://img.shields.io/badge/Package-UPM-00A67E?style=for-the-badge" alt="UPM Package" />
</p>

## What this project is
This repo contains a Unity ECS rendering demo and the reusable rendering package behind it.

The package lives in `Packages/com.ecs2d.renderer` and contains:

- `ECS2D.Rendering.SpriteSystem`
- `ECS2D.Rendering.SpriteData`
- `ECS2D.Rendering.EntitiesReferences`
- `ECS2D.Rendering.SpriteDataAuthoring`
- `ECS2D.Rendering.EntitiesReferenceAuthoring`

## Why this version looks better
- The reusable core is separated from the demo code.
- The repo page now has a visual header instead of plain text.
- The important setup details are grouped into short, scannable sections.

## Quick Start
1. Open the project in Unity `6000.3.10f1` or newer.
2. Keep the package folder `Packages/com.ecs2d.renderer` in the repo or install it as a local package.
3. Add `SpriteDataAuthoring` to entities that should be rendered.
4. Add `EntitiesReferenceAuthoring` to a scene object and assign the bullet prefab.
5. Make sure the material `Third` is available so `SpriteSystem` can load it through `Resources`.

## Included files
- Runtime ECS code
- Authoring components
- The custom shader used by the renderer
- The material and texture assets required by the renderer

## Demo-only code
The following pieces stay outside the package on purpose:

- `Systems.SpawnSystem`
- `EntitiCounter`

## Package path
If you want to reuse the renderer in another Unity project, point that project at:

`Packages/com.ecs2d.renderer`

## Notes
- This project is based on Unity ECS with indirect instanced rendering.
- The package is meant to be copied, embedded, or referenced from Git without manually hunting through source files.
