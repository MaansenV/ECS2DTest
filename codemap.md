# Repository Atlas: ECS2DTest

## Project Responsibility
Unity 6000.3 ECS 2D rendering repository centered on the reusable `Packages/com.ecs2d.renderer` UPM package, with sample scenes, project-specific ECS glue, Unity project configuration, and scripted release automation.

## System Entry Points
- `Packages/com.ecs2d.renderer/Runtime/SpriteSystem.cs`: final sprite render path for the package runtime.
- `Packages/com.ecs2d.renderer/Runtime/SpriteAnimationSystem.cs`: frame advancement and clip progression.
- `Packages/com.ecs2d.renderer/Runtime/ParticleEmissionSystem.cs`: particle spawn/emission entry for emitter simulation.
- `Assets/Scripts/System/SpawnAuthoring.cs`: sample-scene authoring bridge into ECS spawn configuration.
- `Assets/Scripts/System/SpawnSystem.cs`: project-level runtime spawning/orchestration glue.
- `Packages/com.ecs2d.renderer/package.json`: package metadata and version source of truth.
- `Packages/manifest.json`: Unity dependency manifest and local package resolution surface.
- `ProjectSettings/EditorBuildSettings.asset`: enabled scene list and build entry configuration.
- `tools/release-package.ps1`: scripted package release pipeline.

## Directory Map
| Directory | Responsibility Summary | Detailed Map |
|-----------|------------------------|--------------|
| `Assets/` | Unity project content boundary for scenes, repo-owned gameplay glue, runtime assets, and vendored sample dependencies. | [View Map](Assets/codemap.md) |
| `Assets/Scripts/` | Thin project-specific bridge layer that adapts scene/editor workflows into ECS runtime behavior. | [View Map](Assets/Scripts/codemap.md) |
| `Assets/Scripts/Authoring/` | MonoBehaviour/UI-facing authoring adapters that push configuration into ECS-side systems. | [View Map](Assets/Scripts/Authoring/codemap.md) |
| `Assets/Scripts/Editor/` | Editor-only utility and preset-generation tooling behind the `Assets.Scripts.Editor` assembly boundary. | [View Map](Assets/Scripts/Editor/codemap.md) |
| `Assets/Scripts/System/` | Sample spawn pipeline that bakes scene settings and instantiates ECS entities at runtime. | [View Map](Assets/Scripts/System/codemap.md) |
| `Packages/` | Unity package-management boundary containing dependency manifests and the local renderer package. | [View Map](Packages/codemap.md) |
| `Packages/com.ecs2d.renderer/` | Reusable ECS2D renderer package with runtime systems, editor tooling, tests, and package metadata. | [View Map](Packages/com.ecs2d.renderer/codemap.md) |
| `Packages/com.ecs2d.renderer/Runtime/` | Core ECS sprite/particle runtime: baked data, simulation systems, culling, animation, and render prep. | [View Map](Packages/com.ecs2d.renderer/Runtime/codemap.md) |
| `Packages/com.ecs2d.renderer/Editor/` | Custom inspector/editor support for package authoring components. | [View Map](Packages/com.ecs2d.renderer/Editor/codemap.md) |
| `Packages/com.ecs2d.renderer/Editor/Styles/` | Shared UI Toolkit style assets for editor presentation consistency. | [View Map](Packages/com.ecs2d.renderer/Editor/Styles/codemap.md) |
| `ProjectSettings/` | Unity project configuration surface for build, rendering, Entities, physics, input, and package behavior. | [View Map](ProjectSettings/codemap.md) |
| `tools/` | Release automation scripts for packaging, versioning, tagging, and publication. | [View Map](tools/codemap.md) |

## Architecture Notes
- Product code is UPM-first: reusable runtime/editor behavior lives under `Packages/com.ecs2d.renderer`, while `Assets/` contains project glue and sample content.
- Data flow is primarily GameObject authoring -> baking -> ECS runtime systems -> sprite/particle rendering.
- Package/runtime determinism matters: several systems and tests rely on stable animation, sorting, culling, and sheet-swap semantics.
- Release flow is scripted rather than ad hoc; versioning and publishing run through `tools/release-package.ps1`.

## Navigation Guidance
- Start in `Packages/com.ecs2d.renderer/Runtime/` for renderer behavior changes.
- Start in `Assets/Scripts/` for scene-specific spawn/setup logic.
- Start in `ProjectSettings/` for build scene, URP, Entities, input, or package-resolution changes.
- Start in `tools/` for package release workflow changes.
