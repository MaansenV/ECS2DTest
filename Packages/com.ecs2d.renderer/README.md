# ECS2D Rendering

Reusable ECS sprite rendering core extracted from the sample project.

## Requirements
- Unity 6000.3 or newer
- Entities 1.4.x
- Burst, Collections, and Mathematics from the package manifest

## Install
- If this repository is used as a local package source, add `Packages/com.ecs2d.renderer` directly.
- If published to git, use Unity's Git URL package install with the repository URL and package path.

## Included runtime pieces
- `ECS2D.Rendering.SpriteSystem`
- `ECS2D.Rendering.SpriteData`
- `ECS2D.Rendering.EntitiesReferences`
- `ECS2D.Rendering.SpriteDataAuthoring`
- `ECS2D.Rendering.EntitiesReferenceAuthoring`
- `Third.mat`, `emojione-2.png`, and `Instan.shader`

## Usage
1. Add `SpriteDataAuthoring` to the entities you want to render.
2. Add `EntitiesReferenceAuthoring` to a scene object and assign the bullet prefab.
3. Make sure `Third.mat` is available in `Resources` so `SpriteSystem` can load it.
