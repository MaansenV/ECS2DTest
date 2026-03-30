# ECS2D Renderer Documentation

Detailed usage guide for the ECS2D Rendering package. All types live in the `ECS2D.Rendering` namespace.

---

## Table of Contents

- [1. Prerequisites (Materials & Shaders)](#1-prerequisites-materials--shaders)
- [2. Sprite Sheet Definition](#2-sprite-sheet-definition)
- [3. Sprite Data Authoring](#3-sprite-data-authoring)
- [4. Animation](#4-animation)
  - [4a. Animation Set Definition (ScriptableObject)](#4a-animation-set-definition)
  - [4b. Animation Clips](#4b-animation-clips)
  - [4c. Animation Authoring](#4c-animation-authoring)
  - [4d. Changing Animations at Runtime](#4d-changing-animations-at-runtime)
  - [4e. Pausing & Resuming Animations at Runtime](#4e-pausing--resuming-animations-at-runtime)
- [5. Changing Sprite Sheets at Runtime](#5-changing-sprite-sheets-at-runtime)
- [6. Sprite Sheet Database](#6-sprite-sheet-database)
- [7. Frustum Culling](#7-frustum-culling)
  - [7a. Authoring](#7a-culling-authoring)
  - [7b. Enabling/Disabling Individual Sprites at Runtime](#7b-enablingdisabling-individual-sprites-at-runtime)
  - [7c. Global Culling Override](#7c-global-culling-override)
- [8. Entities References](#8-entities-references)
- [9. ECS Components Reference](#9-ecs-components-reference)
- [10. Systems Overview](#10-systems-overview)

---

## 1. Prerequisites (Materials & Shaders)

The renderer uses `Graphics.DrawMeshInstancedIndirect` for GPU instancing. This requires shaders that support instancing via `StructuredBuffer`s. Shader Model 4.5+ is required (`#pragma target 4.5`).

**Included shader:**  
`Instanced/SpriteRendererIndexedUv` — a 2D sprite shader that reads per-instance data (position, rotation, scale, color, UV frame) from compute buffers. It supports alpha clipping and alpha blending.

**Included material:**  
A pre-made material using this shader is available at `Packages/com.ecs2d.renderer/Runtime/Resources/Third.mat`.

**Creating your own material:**
1. Create a new material.
2. Assign the shader `Instanced/SpriteRendererIndexedUv`.
3. Assign your sprite sheet texture to `_MainTex`.
4. Set `_Cutoff` (alpha threshold, default `0.5`) as needed.
5. Assign this material as `BaseMaterial` in your `SpriteSheetDefinition`.

**Placing Sprite Sheets for auto-loading:**  
The `SpriteSheetDatabase` loads all `SpriteSheetDefinition` assets from `Resources/SpriteSheets/`. Place your sheets in `Assets/Resources/SpriteSheets/` to have them auto-discovered.

---

## 2. Sprite Sheet Definition

`SpriteSheetDefinition` is a `ScriptableObject` that defines the texture, material, and frame layout for a sprite sheet.

**How to create:**  
`Create > ECS2D > Rendering > Sprite Sheet Definition`

**Properties:**

| Property | Type | Default | Description |
|---|---|---|---|
| `SheetId` | `int` | `1` | Unique identifier for this sprite sheet. Used by the rendering system to group sprites. Must be unique across all sheets. |
| `BaseMaterial` | `Material` | — | Material applied when rendering sprites from this sheet. |
| `Texture` | `Texture2D` | — | Source texture containing the sprite grid. |
| `WorldBounds` | `Bounds` | `(0,0,0)` size `(1000,1000,1000)` | Bounding box in world space, used for GPU instancing setup. |
| `InitialCapacity` | `int` | `256` | Initial number of sprite instances this sheet can render. Controls the initial allocation size of the compute buffers used for GPU instancing. |
| `CapacityStep` | `int` | `256` | When the instance count exceeds the current capacity, the buffer grows by this amount. |
| `AutoGenerateGridFrames` | `bool` | `true` | If enabled, automatically generates UV frame rectangles from the grid layout. |
| `Columns` | `int` | `4` | Number of columns in the sprite grid. |
| `Rows` | `int` | `4` | Number of rows in the sprite grid. |
| `FrameCount` | *(read-only)* | — | Total number of frames (`Columns * Rows` when auto-generating). |
| `Frames` | `Vector4[]` | — | Array of UV rectangles. Each `Vector4` is `(width, height, offsetX, offsetY)` in normalized UV space. |

**How `InitialCapacity` and `CapacityStep` affect performance:**  
The `SpriteSystem` uses `Graphics.DrawMeshInstancedIndirect` with compute buffers. Each sprite sheet gets its own set of buffers (for translation/rotation, scale, color, frame index). Setting `InitialCapacity` to match your expected sprite count avoids runtime buffer reallocations. If more sprites are spawned than the current capacity allows, the buffer grows by `CapacityStep`.

**Grid frame generation:**  
When `AutoGenerateGridFrames` is enabled, frames are generated left-to-right, top-to-bottom. Frame index 0 is the top-left cell. The texture is divided evenly into `Columns` * `Rows` cells.

---

## 3. Sprite Data Authoring

Attach `SpriteDataAuthoring` to a GameObject or Prefab to author a 2D sprite entity.

**Properties:**

| Field | Type | Default | Description |
|---|---|---|---|
| `SpriteSheet` | `SpriteSheetDefinition` | `null` | Reference to the sprite sheet asset. **Required.** |
| `SpriteFrameIndex` | `int` | `0` | Index of the frame to display initially. Clamped to valid range during baking. |
| `BaseScale` | `float` | `1f` | Scale multiplier. The final baked scale is `BaseScale * transform.lossyScale.x`. **Only the X-axis scale is used** (the renderer expects uniform scaling). Non-uniform scaling will log a warning. |
| `Color` | `float4` | `(1,1,1,1)` | RGBA tint color. |
| `RotationOffsetDegrees` | `float` | `0` | Additional rotation offset in degrees. Final baked rotation (in radians) = `transform.eulerAngles.z + RotationOffsetDegrees`. |

**Example — setup via code:**

```csharp
using ECS2D.Rendering;
using UnityEngine;

public class SpriteSetupExample : MonoBehaviour
{
    public SpriteDataAuthoring spriteAuthoring;
    public SpriteSheetDefinition mySheet;

    void Start()
    {
        spriteAuthoring.SpriteSheet = mySheet;
        spriteAuthoring.SpriteFrameIndex = 0;
        spriteAuthoring.BaseScale = 1.5f;
        spriteAuthoring.Color = new float4(1f, 0.5f, 0.5f, 1f); // red tint
    }
}
```

---

## 4. Animation

Animation in ECS2D has three layers: defining the animation clips in a ScriptableObject, wiring it up with an authoring component, and changing animations at runtime via an ECS command pattern.

### 4a. Animation Set Definition

`SpriteAnimationSetDefinition` is a `ScriptableObject` that links a sprite sheet to a list of animation clips.

**How to create:**  
`Create > ECS2D > Rendering > Sprite Animation Set Definition`

**Properties:**

| Property | Type | Description |
|---|---|---|
| `SpriteSheet` | `SpriteSheetDefinition` | The sprite sheet this animation set targets. |
| `Clips` | `List<SpriteAnimationClip>` | List of animation clips available in this set. |

### 4b. Animation Clips

Each `SpriteAnimationClip` defines a sequence of frames from a grid-based sprite sheet.

| Field | Type | Description |
|---|---|---|
| `Name` | `string` | Name of the clip (e.g., `"Idle"`, `"Run"`, `"Attack"`). Used to look up clips by name. |
| `Row` | `int` | Grid row where the animation starts. |
| `StartColumn` | `int` | Grid column where the animation starts. |
| `FrameCount` | `int` | Number of frames in the sequence. |
| `FrameRate` | `float` | Playback speed in frames per second. |
| `Loop` | `bool` | If true, the animation repeats from the beginning after the last frame. |
| `PingPong` | `bool` | If true, plays forward then backward (e.g., 0→1→2→1→0) before looping. |

**How frames are resolved:**  
The actual sprite sheet frame index for a given clip frame is calculated as:  
`frameIndex = (Row * Columns) + StartColumn + clipFrameIndex`

**Example — defining clips in the Inspector:**

Suppose your sprite sheet has 4 columns and 4 rows. You define:
- **Idle** (Row: 0, StartColumn: 0, FrameCount: 4, FrameRate: 8, Loop: true)
- **Run** (Row: 1, StartColumn: 0, FrameCount: 4, FrameRate: 12, Loop: true)
- **Attack** (Row: 2, StartColumn: 0, FrameCount: 4, FrameRate: 10, Loop: false)

Clip index 0 = "Idle", clip index 1 = "Run", clip index 2 = "Attack".

### 4c. Animation Authoring

Attach `SpriteAnimationAuthoring` alongside `SpriteDataAuthoring` to control animation playback on the entity.

**Properties:**

| Field | Type | Default | Description |
|---|---|---|---|
| `AnimationSet` | `SpriteAnimationSetDefinition` | `null` | Reference to the animation set asset. |
| `StartAnimation` | `string` | `""` | Name of the clip to play on start. Must match a `Name` in the animation set's clips. |
| `PlaybackSpeed` | `float` | `1f` | Multiplier applied to the clip's base `FrameRate`. `1.0` = normal, `0.5` = half speed. |
| `PlayOnStart` | `bool` | `true` | If true, the animation starts playing immediately when the entity is initialized. |

**Example:**

```csharp
using ECS2D.Rendering;
using UnityEngine;

public class AnimationSetupExample : MonoBehaviour
{
    public SpriteAnimationAuthoring animAuthoring;
    public SpriteAnimationSetDefinition animSet;

    void Start()
    {
        animAuthoring.AnimationSet = animSet;
        animAuthoring.StartAnimation = "Idle";
        animAuthoring.PlaybackSpeed = 1.0f;
        animAuthoring.PlayOnStart = true;
    }
}
```

### 4d. Changing Animations at Runtime

To change the playing animation on an entity at runtime, you add a `SpriteAnimationChangeRequest` component to the entity. The `SpriteAnimationChangeRequestSystem` processes it automatically and removes it afterwards (command pattern).

**`SpriteAnimationChangeRequest` fields:**

| Field | Type | Description |
|---|---|---|
| `ClipIndex` | `int` | Index of the target clip in the animation set (0-based). |
| `StartTime` | `float` | Time offset to start from (in seconds). |
| `Restart` | `byte` | `1` = start from `StartTime`. `0` = start from time 0. |

**Example — changing animation from a system:**

```csharp
using ECS2D.Rendering;
using Unity.Entities;
using Unity.Burst;

[BurstCompile]
public partial struct ChangeAnimationSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

        // Change animation to clip index 1 (e.g., "Run") on all player entities
        foreach (var (_, entity) in SystemAPI.Query<RefRO<PlayerTag>>().WithEntityAccess())
        {
            ecb.AddComponent(entity, new SpriteAnimationChangeRequest
            {
                ClipIndex = 1,
                StartTime = 0f,
                Restart = 0
            });
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
```

**How clip indices map to clips:**  
The `ClipIndex` corresponds to the position of the clip in the `SpriteAnimationSetDefinition.Clips` list. The first clip is index 0, the second is index 1, etc. If you need to find a clip index by name at runtime, the system internally resolves the `StartAnimation` string from `SpriteAnimationAuthoring` to its index during baking.

### 4e. Pausing & Resuming Animations at Runtime

To pause or resume an animation at runtime, write directly to the `SpriteAnimationState` component. The `Playing` field controls playback: `1` = playing, `0` = paused.

**Example — pause/resume from a system:**

```csharp
using ECS2D.Rendering;
using Unity.Collections;
using Unity.Burst;
using Unity.Entities;

[BurstCompile]
public partial struct PauseAnimationSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        // Pause all entities with a specific tag
        foreach (var (animState, entity) in
            SystemAPI.Query<RefRW<SpriteAnimationState>>()
                .WithAll<DeadTag>()
                .WithEntityAccess())
        {
            var s = animState.ValueRW;
            s.Playing = 0; // pause
            animState.ValueRW = s;
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
```

**Example — toggle pause on a single entity:**

```csharp
void TogglePause(EntityManager em, Entity entity)
{
    var state = em.GetComponentData<SpriteAnimationState>(entity);
    state.Playing = (byte)(state.Playing == 1 ? 0 : 1);
    em.SetComponentData(entity, state);
}
```

---

## 5. Changing Sprite Sheets at Runtime

To swap the sprite sheet on an entity at runtime, use `SpriteSheetRuntime`. **Do not** just change `SpriteData.SpriteSheetId` directly — the renderer groups entities by `SpriteSheetRenderKey` (a shared component), so you must update both the component data and the shared component together.

**Static methods:**

| Method | Description |
|---|---|
| `SetSheet(EntityManager, Entity, int sheetId)` | Changes the sprite sheet on a single entity. |
| `SetSheet(EntityManager, EntityQuery, int sheetId)` | Changes the sprite sheet on all entities matching the query (bulk update). |
| `CreateRenderKey(int sheetId)` | Creates a `SpriteSheetRenderKey` for the given sheet ID. |

**Example:**

```csharp
using ECS2D.Rendering;
using Unity.Entities;

[BurstCompile]
public partial struct EquipmentChangeSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var entityManager = state.EntityManager;

        // Get the new sheet ID from the database
        if (SpriteSheetDatabase.TryGetDefinition(2, out var newSheet))
        {
            // Change all entities with a specific tag
            var query = SystemAPI.QueryBuilder()
                .WithAll<EquippedItemTag>()
                .Build();

            SpriteSheetRuntime.SetSheet(entityManager, query, newSheet.SheetId);
        }
    }
}
```

**What happens internally:**  
`SetSheet` updates `SpriteData.SpriteSheetId` and also calls `entityManager.SetSharedComponent(entity, new SpriteSheetRenderKey { SheetId = sheetId })`. This forces the entity into the correct render batch. Skipping the shared component update will cause the sprite to render with the wrong sheet or not appear at all.

---

## 6. Sprite Sheet Database

`SpriteSheetDatabase` is a static class that manages a cached registry of all `SpriteSheetDefinition` assets in your project.

**How it loads definitions:**  
By default, it loads all `SpriteSheetDefinition` assets from `Resources/SpriteSheets/` using `Resources.LoadAll<SpriteSheetDefinition>("SpriteSheets")`. Place your sheet assets in an `Assets/Resources/SpriteSheets/` folder.

**API:**

| Member | Return Type | Description |
|---|---|---|
| `Definitions` | `SpriteSheetDefinition[]` | All loaded definitions (lazy-loads on first access). |
| `DefinitionsSignature` | `int` | A hash of all definitions, used by the renderer to detect changes. |
| `GetDefinitions()` | `SpriteSheetDefinition[]` | Returns all definitions (forces load if not yet loaded). |
| `TryGetDefinition(int sheetId, out SpriteSheetDefinition)` | `bool` | Looks up a definition by its `SheetId`. |
| `RefreshCache()` | `void` | Forces a reload of all definitions. Also called automatically when you edit a `SpriteSheetDefinition` in the Editor. |

**Custom loading (e.g., Addressables):**  
The database exposes an internal `DefinitionsLoader` delegate of type `Func<SpriteSheetDefinition[]>`. While marked `internal`, you can inject a custom loader for testing or alternative loading strategies:

```csharp
// Example: load via a custom method (requires reflection or internal access)
// SpriteSheetDatabase.DefinitionsLoader = () => LoadFromAddressables();
```

**Example — lookup a sheet by ID:**

```csharp
using ECS2D.Rendering;

void Example()
{
    if (SpriteSheetDatabase.TryGetDefinition(1, out var sheet))
    {
        UnityEngine.Debug.Log($"Found sheet: {sheet.name}, {sheet.FrameCount} frames");
    }

    // List all registered sheets
    foreach (var def in SpriteSheetDatabase.Definitions)
    {
        UnityEngine.Debug.Log($"Sheet {def.SheetId}: {def.name}");
    }
}
```

---

## 7. Frustum Culling

### 7a. Culling Authoring

Attach `SpriteCullingSettingsAuthoring` to control whether an entity participates in frustum culling.

| Field | Type | Default | Description |
|---|---|---|---|
| `CullingEnabled` | `bool` | `true` | Whether this entity is subject to culling. Baked into the `SpriteCullingSettings` ECS component as `byte` (1 = enabled, 0 = disabled). |

### 7b. Enabling/Disabling Individual Sprites at Runtime

The `SpriteCullState` component is an **enableable component** (`IEnableableComponent`). You can toggle individual sprite visibility by enabling or disabling this component:

```csharp
using ECS2D.Rendering;
using Unity.Entities;

// Hide a sprite
entityManager.SetComponentEnabled<SpriteCullState>(entity, false);

// Show it again
entityManager.SetComponentEnabled<SpriteCullState>(entity, true);
```

This is more efficient than destroying/recreating entities, as the entity remains in the world but is excluded from the culling and rendering queries.

### 7c. Global Culling Override

`SpriteCullingRuntime` allows you to globally override whether frustum culling is active, regardless of individual per-entity `SpriteCullingSettings`. This is useful for debugging (e.g., disabling culling to inspect all sprites) or for temporary full-scene visibility.

**Static API:**

| Method | Description |
|---|---|
| `SetOverride(bool enabled)` | Forces global culling to the given state. When set, individual per-entity culling settings are ignored. |
| `TryGetOverride(out bool enabled)` | Returns `true` if a global override is currently active, and outputs the override state in `enabled`. |
| `ClearOverride()` | Removes the global override. Per-entity culling settings take effect again. |

**Example — toggle culling with a debug key:**

```csharp
using ECS2D.Rendering;
using UnityEngine;

public class DebugCullingToggle : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F2))
        {
            if (SpriteCullingRuntime.TryGetOverride(out bool enabled))
            {
                // Toggle: if currently disabled, enable it; if enabled, disable it
                SpriteCullingRuntime.SetOverride(!enabled);
            }
            else
            {
                // No override active yet — set one (disable culling to see everything)
                SpriteCullingRuntime.SetOverride(false);
            }

            Debug.Log($"Culling override: {SpriteCullingRuntime.TryGetOverride(out var e)} -> {e}");
        }

        // Press F3 to restore normal per-entity culling
        if (Input.GetKeyDown(KeyCode.F3))
        {
            SpriteCullingRuntime.ClearOverride();
            Debug.Log("Culling override cleared.");
        }
    }
}
```

---

## 8. Entities References

`EntitiesReferenceAuthoring` lets you convert a GameObject prefab into an `Entity` reference and access it from ECS systems.

**Authoring properties:**
- `bulletPrefab` (`GameObject`): The prefab to convert to an entity reference.

**ECS component — `EntitiesReferences`:**

| Field | Type | Description |
|---|---|---|
| `BulletPrefab` | `Entity` | The baked entity reference for the assigned prefab. |

**Example — spawning from a system:**

```csharp
using ECS2D.Rendering;
using Unity.Entities;
using Unity.Transforms;

[BurstCompile]
public partial struct BulletSpawnerSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

        foreach (var (refs, transform) in
            SystemAPI.Query<RefRO<EntitiesReferences>, RefRO<LocalToWorld>>())
        {
            var bullet = ecb.Instantiate(refs.ValueRO.BulletPrefab);
            ecb.SetComponent(bullet, new LocalTransform
            {
                Position = transform.ValueRO.Position,
                Rotation = quaternion.identity,
                Scale = 1f
            });
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
```

---

## 9. ECS Components Reference

These are the raw ECS components baked from the authoring MonoBehaviours. Most developers don't interact with these directly, but they are useful for advanced runtime manipulation.

### `SpriteData` (`IComponentData`)

The core rendering data for a sprite.

| Field | Type | Description |
|---|---|---|
| `TranslationAndRotation` | `float4` | Position (xyz) and rotation in radians (w). |
| `Scale` | `float` | Uniform scale factor. |
| `Color` | `float4` | RGBA color (weighted for blending). |
| `SpriteFrameIndex` | `int` | Current frame index in the sprite sheet. |
| `SpriteSheetId` | `int` | ID of the sprite sheet. |

### `SpriteAnimationState` (`IComponentData`)

Runtime animation playback state.

| Field | Type | Description |
|---|---|---|
| `Time` | `float` | Current animation time in seconds. |
| `PlaybackSpeed` | `float` | Speed multiplier (from `SpriteAnimationAuthoring.PlaybackSpeed`). |
| `CurrentClipIndex` | `int` | Index of the currently playing clip. |
| `CurrentFrameIndex` | `int` | Current frame within the clip. |
| `Flags` | `byte` | State flags. `InitializedFlag = 1` is set after first animation update. |
| `Playing` | `byte` | `1` = playing, `0` = paused. |

### `SpriteAnimationSetReference` (`IComponentData`)

| Field | Type | Description |
|---|---|---|
| `Value` | `BlobAssetReference<SpriteAnimationSetBlob>` | Blob asset containing all clip data. Compiled during baking from `SpriteAnimationSetDefinition`. |

### `SpriteAnimationChangeRequest` (`IComponentData`)

See [Section 4d](#4d-changing-animations-at-runtime).

### `SpriteCullingSettings` (`IComponentData`)

| Field | Type | Description |
|---|---|---|
| `Enabled` | `byte` | `1` = culling enabled, `0` = disabled. |

### `SpriteCullState` (`IComponentData`, `IEnableableComponent`)

Empty struct. Its enabled/disabled state controls sprite visibility. See [Section 7b](#7b-enablingdisabling-individual-sprites-at-runtime).

### `SpriteSheetRenderKey` (`ISharedComponentData`)

Groups entities by sheet for batched rendering.

| Field | Type | Description |
|---|---|---|
| `SheetId` | `int` | The sprite sheet ID. |

### `EntitiesReferences` (`IComponentData`)

See [Section 8](#8-entities-references).

---

## 10. Systems Overview

All systems run in the `PresentationSystemGroup` (except `SpriteAnimationChangeRequestSystem` which is in `SimulationSystemGroup`).

| System | Group | Execution Order | Description |
|---|---|---|---|
| `SpriteAnimationChangeRequestSystem` | `SimulationSystemGroup` | Before `SpriteAnimationSystem` | Processes `SpriteAnimationChangeRequest` commands, applies them to `SpriteAnimationState`, then removes the request. |
| `SpriteTransformSyncSystem` | `PresentationSystemGroup` | First | Syncs `LocalToWorld` transform data into `SpriteData` (translation, rotation, scale). |
| `SpriteCullingSystem` | `PresentationSystemGroup` | After `SpriteTransformSyncSystem` | Performs orthographic frustum culling. Enables/disables `SpriteCullState` based on camera visibility. |
| `SpriteAnimationSystem` | `PresentationSystemGroup` | After `SpriteCullingSystem` | Advances animation time, evaluates frame indices, and updates `SpriteData.SpriteFrameIndex`. |
| `SpriteSystem` | `PresentationSystemGroup` | Last | Main rendering system. Batches visible sprites by `SpriteSheetRenderKey` and draws them via `Graphics.DrawMeshInstancedIndirect`. Uses burst-compiled jobs to upload sprite data to compute buffers. |
