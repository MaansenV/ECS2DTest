# ProjectSettings/

## Responsibility
Unity editor/runtime configuration for the project: build targets, package manager, rendering, Entities, physics, input, quality, and editor behavior.

## Design
Asset-backed settings stored as versioned YAML/JSON in `ProjectSettings/`. Key repo-relevant surfaces include `EditorBuildSettings.asset` (enabled scenes), `ProjectSettings.asset` (global player/editor defaults), `EntitiesClientSettings.asset`, `URPProjectSettings.asset`, `GraphicsSettings.asset`, `Physics2DSettings.asset`, `InputManager.asset`, and `PackageManagerSettings.asset`.

## Flow
Unity loads these files at editor startup and when project settings change; they shape compilation, package resolution, scene inclusion, rendering pipeline selection, physics/input defaults, and Entities runtime/editor integration.

## Integration
Directly controls what ships and how the repo runs in-editor: build scene list, URP/rendering setup, 2D physics behavior, legacy input configuration, Burst/Entities client settings, and package registry/source resolution for UPM packages.
