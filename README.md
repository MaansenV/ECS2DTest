# Unity ECS Rendering Project

The reusable rendering core now lives in `Packages/com.ecs2d.renderer`.

## What it contains
- `ECS2D.Rendering.SpriteSystem`
- `ECS2D.Rendering.SpriteData`
- `ECS2D.Rendering.EntitiesReferences`
- `ECS2D.Rendering.SpriteDataAuthoring`
- `ECS2D.Rendering.EntitiesReferenceAuthoring`

## Notes
- The sample project still contains host/demo code such as `Systems.SpawnSystem` and `EntitiCounter`.
- The package is intended to be reused from other Unity ECS projects via a local package path or Git URL.
