# ECS2D Renderer Documentation

This document explains how to use the core components and assets of the ECS2D Renderer package with short examples.

## 1. Setting up a Sprite Sheet

To render a sprite, you first need a **SpriteSheetDefinition**. This is a ScriptableObject that holds the material, texture, and grid layout (columns/rows) of your sprite sheet.

**How to use:**
1. Right-click in the Project window -> `Create > ECS2D > Sprite Sheet Definition`.
2. Assign your Material and set the Grid Size (e.g., 4 columns, 1 row).

## 2. Rendering a Sprite (SpriteDataAuthoring)

Attach the `SpriteDataAuthoring` component to your GameObject/Prefab to define which sprite sheet it uses and which frame to display.

```csharp
using ECS2D.Rendering;
using UnityEngine;

public class MySpriteSetup : MonoBehaviour
{
    public SpriteDataAuthoring spriteAuthoring;
    public SpriteSheetDefinition mySheet;

    void Start()
    {
        // Example of assigning a sheet and initial frame from code,
        // though usually you just do this in the Inspector.
        spriteAuthoring.SpriteSheet = mySheet;
        spriteAuthoring.InitialFrameIndex = 0;
    }
}
```

## 3. Animating a Sprite

### Creating an Animation Set
Create a **SpriteAnimationSetDefinition** ScriptableObject to define animations (like Idle, Run, Attack). Each animation has a start frame, frame count, and frame rate.

### Using SpriteAnimationAuthoring
Attach `SpriteAnimationAuthoring` to the same GameObject to control animations.

```csharp
using ECS2D.Rendering;
using UnityEngine;
using Unity.Entities;

public class MyAnimationController : MonoBehaviour
{
    public SpriteAnimationAuthoring animationAuthoring;
    public SpriteAnimationSetDefinition animationSet;

    void Start()
    {
        animationAuthoring.AnimationSet = animationSet;
        // The default animation (e.g. index 0) will play automatically based on the authoring settings.
    }
}
```

*Note: Under the hood, `SpriteAnimationSystem` updates the current frame index for rendering.*

## 4. Frustum Culling (SpriteCullingSettingsAuthoring)

To optimize rendering, you can add `SpriteCullingSettingsAuthoring` to a manager object in your scene.

```csharp
using ECS2D.Rendering;
using UnityEngine;

public class CullingSetup : MonoBehaviour
{
    public SpriteCullingSettingsAuthoring cullingSettings;

    void SetupCulling()
    {
        // Enables orthographic camera frustum culling for all sprites
        cullingSettings.EnableCulling = true;
        
        // Add a small margin to prevent popping at screen edges
        cullingSettings.CullingMargin = 0.5f;
    }
}
```

*Note: The `SpriteCullingSystem` will automatically hide entities that are outside the camera view.*

## 5. Referencing Entities (EntitiesReferenceAuthoring)

Use `EntitiesReferenceAuthoring` to easily keep track of spawned entities or link GameObjects to their ECS counterparts.

```csharp
using ECS2D.Rendering;
using UnityEngine;
using Unity.Entities;

public class SpawnerExample : MonoBehaviour
{
    public EntitiesReferenceAuthoring entityReference;
    public GameObject prefabToSpawn;

    void Start()
    {
        // Example: Spawning entities and storing their references for quick access
        // (Assuming you have converted the prefab to an entity first)
    }
}
```

## System Execution Order

The ECS systems run automatically in the Unity ECS loop:
1. `SpriteAnimationSystem` updates the active frame based on delta time.
2. `SpriteCullingSystem` checks bounds against the main camera.
3. `SpriteSystem` batches and draws the visible sprites using `Graphics.DrawMeshInstancedIndirect`.
