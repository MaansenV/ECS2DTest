# ECS2D RENDERING PACKAGE

## OVERVIEW
Reusable ECS 2D sprite rendering package for Unity. This folder is the product core: runtime systems, authoring components, package docs, and EditMode tests.

## STRUCTURE
```text
Packages/com.ecs2d.renderer/
├── Runtime/      # runtime ECS systems, authoring, shaders, resources
└── Tests/EditMode/  # EditMode test assembly
```

## WHERE TO LOOK
| Task | Location | Notes |
|------|----------|-------|
| Render pipeline | `Runtime/SpriteSystem.cs` | final batching/draw path |
| Animation flow | `Runtime/SpriteAnimationSystem.cs` | clip/frame progression |
| Culling | `Runtime/SpriteCullingSystem.cs` | visibility decisions |
| Transform sync | `Runtime/SpriteTransformSyncSystem.cs` | ECS transform to sprite data |
| Runtime swapping | `Runtime/SpriteSheetRuntime.cs` | sheet changes must preserve render key |
| Package tests | `Tests/EditMode/*.cs` | bake/runtime behavior verification |

## CONVENTIONS
- `ECS2D.Rendering.asmdef` is the runtime boundary; keep runtime code inside it.
- `ECS2D.Rendering.Tests.asmdef` is Editor-only and references `TestAssemblies`.
- `AssemblyInfo.cs` exposes internals only to `ECS2D.Rendering.Tests`.
- `README.md` and `../DOCUMENTATION.md` describe the public API contract; keep code and docs aligned.
- Runtime assets live under `Runtime/Resources/SpriteSheets` and `Runtime/Resources`.

## ANTI-PATTERNS (THIS PACKAGE)
- Do not update `SpriteData.SpriteSheetId` without updating `SpriteSheetRenderKey`.
- Do not treat `SpriteAnimationAuthoring` as standalone; it requires `SpriteDataAuthoring` on the same GameObject.
- Do not let direct-matrix tests call `LocalToWorldSystem.Update(...)` if they are asserting the baked matrix itself.
- Do not change package behavior in tests to satisfy a stale assertion; update the test to current runtime semantics.

## UNIQUE STYLES
- Bake-time behavior is heavily reflection-tested; preserve current test harness shape unless fixing that harness.
- The package is UPM-first: package.json, asmdefs, runtime assets, and docs are part of the deliverable.
- Keep runtime APIs deterministic; many tests assert exact frame indices, sort order, and flip handling.

## COMMANDS
```powershell
Unity EditMode tests: run the `ECS2D.Rendering.Tests` assembly in Unity 6000.3.10f1
```

## NOTES
- `Runtime/Resources/SpriteSheets` is auto-discovered by `SpriteSheetDatabase`.
- `SpriteSheetRuntime.SetSheet(...)` is the safe runtime path for swapping sheets.
