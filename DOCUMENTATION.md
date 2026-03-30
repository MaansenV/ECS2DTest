# ECS2D Renderer Documentation

This document explains how to use the core components, ScriptableObjects, and authoring scripts of the ECS2D Renderer package.

---

## 1. Setting up a Sprite Sheet (`SpriteSheetDefinition`)

To render a sprite, you first need a **`SpriteSheetDefinition`**. This is a `ScriptableObject` that holds the material, texture, and grid layout of your sprite sheet.

**How to create:**
Right-click in the Project window -> `Create > ECS2D > Sprite Sheet Definition`.

**Properties:**
* **`SheetId`** (`int`): A unique identifier for the sprite sheet used by the rendering system.
* **`BaseMaterial`** (`Material`): The material applied when rendering the sprites.
* **`Texture`** (`Texture2D`): The source texture containing the sprite grid.
* **`WorldBounds`** (`Bounds`): The bounding box in world space, used for culling.
* **`InitialCapacity`** (`int`): The initial allocation size for tracking instances in the renderer.
* **`CapacityStep`** (`int`): The chunk size to allocate when the initial instance capacity is exceeded.
* **`AutoGenerateGridFrames`** (`bool`): If enabled, automatically splits the texture into frames based on the defined rows and columns.
* **`Columns`** (`int`): The number of columns in the sprite sheet grid.
* **`Rows`** (`int`): The number of rows in the sprite sheet grid.
* **`FrameCount`** *(Read-only)*: The total number of frames in the sprite sheet.

---

## 2. Rendering a Sprite (`SpriteDataAuthoring`)

Attach the `SpriteDataAuthoring` component to your GameObject or Prefab to define which sprite sheet it uses and how it is visually represented.

**Properties:**
* **`SpriteSheet`** (`SpriteSheetDefinition`): Reference to the ScriptableObject defining the sprite sheet. (Required)
* **`SpriteFrameIndex`** (`int`): The index of the specific frame to render from the referenced `SpriteSheet`.
* **`BaseScale`** (`float`): A base scale multiplier for the sprite (Default: `1f`). *Note: The renderer relies on uniform scaling and uses the transform's X-axis scale.*
* **`Color`** (`float4`): The tint or color applied to the sprite (Default: White).
* **`RotationOffsetDegrees`** (`float`): An additional rotation offset (in degrees) applied to the sprite.

```csharp
using ECS2D.Rendering;
using UnityEngine;

public class MySpriteSetup : MonoBehaviour
{
    public SpriteDataAuthoring spriteAuthoring;
    public SpriteSheetDefinition mySheet;

    void Start()
    {
        spriteAuthoring.SpriteSheet = mySheet;
        spriteAuthoring.SpriteFrameIndex = 0;
    }
}
```

---

## 3. Animating a Sprite

Animation in ECS2D requires two pieces: defining the animations in a ScriptableObject, and playing them via an authoring component.

### A. Defining Animations (`SpriteAnimationSetDefinition` & `SpriteAnimationClip`)

Create a **`SpriteAnimationSetDefinition`** ScriptableObject to act as a container linking a specific sprite sheet to its associated animation clips.

**Properties of `SpriteAnimationSetDefinition`:**
* **`SpriteSheet`** (`SpriteSheetDefinition`): Reference to the underlying sprite sheet layout/texture to be animated.
* **`Clips`** (`List<SpriteAnimationClip>`): A list of all available animation clips configured for this specific sprite sheet.

**Properties of a `SpriteAnimationClip`:**
* **`Name`** (`string`): The name of the animation clip (e.g., "Run", "Idle").
* **`Row`** (`int`): The grid row index where the animation starts.
* **`StartColumn`** (`int`): The grid column index where the animation begins.
* **`FrameCount`** (`int`): The total number of frames in the animation sequence.
* **`FrameRate`** (`float`): The target playback speed in frames per second (FPS).
* **`Loop`** (`bool`): If true, the animation will automatically loop back to the beginning once it finishes.
* **`PingPong`** (`bool`): If true, the animation will play in reverse upon reaching the end (e.g., 0, 1, 2, 1, 0).

### B. Playing Animations (`SpriteAnimationAuthoring`)

Attach `SpriteAnimationAuthoring` to the same GameObject as your `SpriteDataAuthoring` to control animations.

**Properties:**
* **`AnimationSet`** (`SpriteAnimationSetDefinition`): Reference to the animation set asset.
* **`StartAnimation`** (`string`): The name of the animation clip to begin playing by default (must match the `Name` in your clips).
* **`PlaybackSpeed`** (`float`): A multiplier applied to the clip's base `FrameRate` (e.g., `1.0` is normal, `0.5` is half-speed).
* **`PlayOnStart`** (`bool`): If true, the animation begins playing immediately.

```csharp
using ECS2D.Rendering;
using UnityEngine;

public class MyAnimationController : MonoBehaviour
{
    public SpriteAnimationAuthoring animationAuthoring;
    public SpriteAnimationSetDefinition animationSet;

    void Start()
    {
        animationAuthoring.AnimationSet = animationSet;
        animationAuthoring.StartAnimation = "Idle";
        animationAuthoring.PlayOnStart = true;
    }
}
```

---

## 4. Frustum Culling (`SpriteCullingSettingsAuthoring`)

To optimize rendering, you can attach `SpriteCullingSettingsAuthoring` to a sprite to determine if it should be subject to frustum culling.

**Properties:**
* **`CullingEnabled`** (`bool`): Determines if the sprite should be subject to culling systems. (Default: `true`). When baked, it assigns a value to the ECS component `SpriteCullingSettings`.

*Note: The `SpriteCullingSystem` will automatically hide entities that are outside the camera view based on this setting.*

---

## 5. Referencing Entities (`EntitiesReferenceAuthoring`)

Use `EntitiesReferenceAuthoring` to easily keep track of spawned entities or link GameObjects to their ECS counterparts.

```csharp
using ECS2D.Rendering;
using UnityEngine;
using Unity.Entities;

public class SpawnerExample : MonoBehaviour
{
    public EntitiesReferenceAuthoring entityReference;
    public GameObject prefabToSpawn;

    // Use entityReference to interact with the ECS entity mapped to this GameObject.
}
```

---

## System Execution Order

The ECS systems run automatically in the Unity ECS loop:
1. `SpriteAnimationSystem` updates the active frame based on delta time.
2. `SpriteTransformSyncSystem` syncs transform matrices.
3. `SpriteCullingSystem` checks bounds against the main camera.
4. `SpriteSystem` batches and draws the visible sprites using `Graphics.DrawMeshInstancedIndirect`.
