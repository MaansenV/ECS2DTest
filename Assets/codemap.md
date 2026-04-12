# Assets/

## Responsibility
Project-level Unity content boundary: holds scene entry points, project-specific gameplay glue, runtime assets, and editor tooling that sit outside the reusable ECS renderer package.

## Design
Organized by function rather than feature stack: `Scripts/` contains repo-owned gameplay systems, authoring, and editor utilities; `Scenes/` and template scenes define playable/sample entry content; `Resources/`, `Prefabs/`, `Materials/`, and `Settings/` provide asset data and rendering configuration. Vendored packages such as `Graphy - Ultimate Stats Monitor/` are isolated as third-party boundaries and should not be treated as product code.

## Flow
Unity loads content from `Assets/` through scene startup and asset references. Scene objects and prefabs drive authoring-time setup into `Assets/Scripts/System/` runtime glue, which in turn binds project content to the ECS renderer package and shared render resources. Editor scripts in `Assets/Scripts/Editor/` support asset generation and authoring workflows without affecting runtime flow.

## Integration
Connects directly to the package in `Packages/com.ecs2d.renderer/` via scene content, authoring data, and runtime glue code. `Assets/Settings/` carries pipeline and scene-template configuration, while `Assets/Graphy - Ultimate Stats Monitor/` remains an external vendored dependency boundary. This folder is the bridge between Unity project state and reusable renderer modules.
