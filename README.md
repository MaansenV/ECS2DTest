# ECS2D Renderer

<p align="center">
  <img src=".github/ecs2d-banner.svg" alt="ECS2D Renderer Banner" width="100%" />
</p>

<p align="center">
  Reusable 2D sprite rendering for Unity ECS as a UPM package.
</p>

<p align="center">
  <img src="https://img.shields.io/badge/Unity-6000.3%2B-000000?style=for-the-badge&logo=unity" alt="Unity 6000.3+" />
  <img src="https://img.shields.io/badge/Entities-1.4.x-2D7DD2?style=for-the-badge" alt="Entities 1.4.x" />
  <img src="https://img.shields.io/badge/Package-UPM-00A67E?style=for-the-badge" alt="UPM Package" />
</p>

## Install

Add this package in Unity Package Manager:

```text
https://github.com/MaansenV/ECS2DTest.git?path=/Packages/com.ecs2d.renderer
```

Or add it directly to `manifest.json`:

```json
{
  "dependencies": {
    "com.ecs2d.renderer": "https://github.com/MaansenV/ECS2DTest.git?path=/Packages/com.ecs2d.renderer"
  }
}
```

## What you get

- `SpriteSystem`
- `SpriteData`
- `SpriteAnimationSystem`
- `SpriteAnimationState`
- `SpriteAnimationSetReference`
- `EntitiesReferences`
- `SpriteDataAuthoring`
- `SpriteAnimationClip`
- `SpriteAnimationSetDefinition`
- `SpriteAnimationAuthoring`
- `EntitiesReferenceAuthoring`
- Material, shader, and texture used by the renderer

Namespace:

```csharp
using ECS2D.Rendering;
```

## How to use

1. Install the package.
2. Create or use a prefab with `SpriteDataAuthoring`.
3. If you want animation, create a `SpriteAnimationSetDefinition` asset, add `SpriteAnimationAuthoring` to the same prefab, and choose the start clip by name.
4. Add `EntitiesReferenceAuthoring` to a scene object.
5. Assign your prefab in `bulletPrefab`.
6. Enter Play Mode and let `SpriteAnimationSystem` update the frame index before `SpriteSystem` renders the ECS sprites.
7. The repo already includes a `SwordMan` demo prefab wired to `Assets/Resources/SpriteAnimations/SwordManAnimations.asset`.

## Requirements

- Unity `6000.3.10f1` or newer
- `com.unity.entities` `1.4.x`

## Package Path

```text
Packages/com.ecs2d.renderer
```
