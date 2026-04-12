# Assets/Scripts/System/

## Responsibility

Authoring-to-ECS bridge for the sample spawn workflow. Converts MonoBehaviour configuration into ECS singleton data and drives batched entity instantiation at runtime.

## Design

`SpawnAuthoring` bakes scene-level inspector fields into `SpawnSettings` plus `SpawnPrefabReferences`. `SpawnSettings` stores scalar spawn parameters; `SpawnPrefabReferences` stores baked prefab entity handles. `SpawnSystem` is an `ISystem` that reads the singleton and uses `EntityManager.Instantiate` to spawn entities in grid batches.

## Flow

1. Authoring component exposes enable flag, prefab references, grid size, spacing, and per-frame budget.
2. Baker resolves prefab entities and writes singleton ECS components on the authoring entity.
3. `SpawnSystem` waits for `SpawnSettings`, then checks `Enabled` and prefab validity.
4. Each update spawns up to `SpawnPerFrame` entities, updates `LocalTransform`/`LocalToWorld` when present, and patches `SpriteData` for the main sprite prefab path.
5. State is tracked across frames with spawn indices and completion flags until both grids are fully emitted.

## Integration

Relies on `Unity.Entities`, `Unity.Transforms`, and `ECS2D.Rendering.SpriteData`. The spawned prefabs must already carry the runtime components expected by the renderer; the system only seeds transform, sprite data, and emitter placement.
