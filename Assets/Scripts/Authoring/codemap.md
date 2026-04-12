# Assets/Scripts/Authoring/

## Responsibility
Scene-facing authoring glue that exposes runtime toggles and inputs in the Unity UI, then translates them into ECS-ready configuration changes for package systems.

## Design
Small MonoBehaviour-driven adapters keep presentation concerns in GameObject/UI space while writing only the minimal state needed by ECS-side authoring or runtime bridges.

## Flow
User interaction on scene objects or UI controls updates local authoring state, which is then propagated to the ECS pipeline through baked components, shared references, or runtime command paths.

## Integration
Connects Unity UI and scene objects to ECS package systems, especially renderer settings such as culling and other configuration flags consumed by bake/runtime systems in `Packages/com.ecs2d.renderer`.
