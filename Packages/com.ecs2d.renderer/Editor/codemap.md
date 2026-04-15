# Packages/com.ecs2d.renderer/Editor/

Custom inspector and editor support for the renderer package's authoring components and scene-facing settings.

## Responsibility

Provides Unity Editor inspectors for `SpriteDataAuthoring`, `SpriteAnimationAuthoring`, `SpriteCullingSettingsAuthoring`, `ParticleEmitterAuthoring`, and `EntitiesReferenceAuthoring`, plus shared UI styling assets under `Editor/Styles`.

## Design

Thin `CustomEditor` layer over runtime authoring types. Inspectors keep validation and presentation editor-only, reuse common style resources, and avoid duplicating bake/runtime logic.

## Flow

Unity selects the matching inspector for an authoring component, the editor draws serialized fields and contextual controls using shared USS/style assets, then the authoring component bakes unchanged into the runtime ECS data path.

## Integration

Depends on `Packages/com.ecs2d.renderer/Runtime` authoring components and the editor asmdef. Feeds baked data into the runtime systems indirectly through the existing authoring/baking workflow; style assets are used only to standardize inspector presentation.
