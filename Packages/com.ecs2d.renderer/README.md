# ECS2D Rendering Documentation

Reusable ECS-native 2D sprite rendering for Unity with animation, culling, and GPU instancing.

## Installation

Git URL for Unity Package Manager:

```text
https://github.com/MaansenV/ECS2DTest.git?path=/Packages/com.ecs2d.renderer
```

Or add it to `manifest.json`:

```json
{
  "dependencies": {
    "com.ecs2d.renderer": "https://github.com/MaansenV/ECS2DTest.git?path=/Packages/com.ecs2d.renderer"
  }
}
```

## Requirements

- Unity `6000.3.10f1` or newer
- `com.unity.entities` `1.4.x`
- Burst, Collections, and Mathematics

## Main workflow

1. Create a `SpriteSheetDefinition`.
2. Assign its material, texture, bounds, and grid settings.
3. Add `SpriteDataAuthoring` to a prefab or GameObject.
4. Assign the `SpriteSheetDefinition` and choose the initial frame.
5. Optional: add `SpriteAnimationAuthoring` with a `SpriteAnimationSetDefinition`.
6. Optional: add `SpriteCullingSettingsAuthoring` in the scene.

## Main types

- `SpriteData`
- `SpriteSheetDefinition`
- `SpriteSheetRenderKey`
- `SpriteAnimationState`
- `SpriteAnimationSetReference`
- `SpriteCullState`

## Main systems

- `SpriteAnimationSystem`
- `SpriteAnimationChangeRequestSystem`
- `SpriteCullingSystem`
- `SpriteTransformSyncSystem`
- `SpriteSystem`

## Authoring components

- `SpriteDataAuthoring`
- `SpriteAnimationAuthoring`
- `SpriteCullingSettingsAuthoring`
- `EntitiesReferenceAuthoring`

## Included runtime assets

- `Third.mat`
- `Instan.shader`
- `Resources/SpriteSheets/CircleSheet.asset`
- `Resources/SpriteSheets/EmojiSheet.asset`

## Notes

- `SpriteAnimationAuthoring` expects a grid-based `SpriteSheetDefinition`.
- Animation clips are authored by `Row`, `StartColumn`, and `FrameCount`.
- Culling currently targets the main orthographic camera workflow.
- The sample repo also includes scenes and prefabs showing the package in use.
