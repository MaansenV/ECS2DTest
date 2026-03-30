# ECS2D Rendering Documentation

A high-performance, reusable ECS-native 2D sprite rendering core extracted from the sample project. Built on Unity's Data-Oriented Technology Stack (DOTS), it uses Burst-compiled jobs, multi-threading, and GPU instancing (`Graphics.DrawMeshInstancedIndirect`) to efficiently render and animate thousands of sprites, bypassing standard Unity `SpriteRenderer` overhead.

## 1. Getting Started / Installation

### Requirements
- Unity 6000.3 or newer
- Entities 1.4.x
- Burst, Collections, and Mathematics from the package manifest

### Installation
- **Local Package:** If this repository is used as a local package source, add `Packages/com.ecs2d.renderer` directly in the Package Manager.
- **Git URL:** If published to git, use Unity's Git URL package install with the repository URL and package path.

## 2. Architecture & Data Flow

The system operates in a streamlined pipeline designed for DOTS:

1. **Authoring (Baking):** Converts standard GameObjects and ScriptableObjects (`SpriteSheetDefinition`, `SpriteAnimationSetDefinition`) into optimized ECS entities and immutable Blob Assets.
2. **Simulation (`SpriteAnimationSystem`):** Runs in the `SimulationSystemGroup`. It updates animation time, resolves the current frame from blob assets, and writes the active frame index back to the entity's data.
3. **Presentation - Culling (`SpriteCullingSystem`):** Runs in the `PresentationSystemGroup`. Performs fast frustum culling against the main orthographic camera using Burst jobs, toggling entity visibility.
4. **Presentation - Rendering (`SpriteSystem`):** Runs after culling. Groups visible sprites by their shared sprite sheet, writes transform/color/frame data to triple-buffered `ComputeBuffers` via parallel jobs, and issues a single instanced draw call per sprite sheet.

## 3. Core Components

The renderer is driven by several key ECS components:

- **`SpriteData` (ComponentData):** The core payload for each sprite. Contains `TranslationAndRotation` (float4), `Scale` (float), `Color` (float4), `SpriteFrameIndex`, and `SpriteSheetId`.
- **`SpriteCullState` (Enableable ComponentData):** A tag component acting as a visibility flag. If disabled by the culling system, the entity is skipped during rendering.
- **`SpriteSheetRenderKey` (SharedComponentData):** Used by the `SpriteSystem` to efficiently batch filter and chunk entities that share the same material and sprite sheet definition.
- **`SpriteAnimationSetReference` (ComponentData):** Contains a `BlobAssetReference` linking the entity to its read-only animation definitions, optimizing memory and access speed.
- **`SpriteAnimationState` (ComponentData):** Tracks runtime playback variables such as `Time`, `PlaybackSpeed`, `CurrentClipIndex`, `CurrentFrameIndex`, and play state flags.

## 4. Systems Overview

- **`SpriteAnimationSystem`:** Evaluates delta time against a `SpriteAnimationSetBlob`. It computes the correct frame index based on the active clip and writes it to `SpriteData.SpriteFrameIndex`.
- **`SpriteCullingSystem`:** Calculates the bounds of the active orthographic `Camera.main`. A burst-compiled job iterates over all `SpriteData` entities and toggles the `SpriteCullState` based on intersection with the camera view.
- **`SpriteSystem`:** Queries all entities with `SpriteData`, a `SpriteSheetRenderKey`, and an enabled `SpriteCullState`. It groups them by Sprite Sheet ID, uploads data directly into ComputeBuffers, and draws a single quad mesh per sprite sheet utilizing triple-buffering to prevent GPU stalls.

## 5. Step-by-Step Guides

### Creating a Sprite Sheet Definition
1. Create a `SpriteSheetDefinition` asset (e.g., under `Resources/SpriteSheets`).
2. Assign the target Material (e.g., `Third.mat` using `Instan.shader`).
3. You can use grid-based auto-generation (`AutoGenerateGridFrames`) or define custom frame bounds manually.

### Setting up a Static Sprite
1. Attach a `SpriteDataAuthoring` script to a Unity GameObject/Prefab.
2. Assign your created `SpriteSheetDefinition`.
3. Configure the base scale, color, rotation offset, and the specific `SpriteFrameIndex` you wish to display.
4. During subscene baking, it will automatically generate the ECS entity with the necessary components.

### Setting up an Animated Sprite
1. Create a `SpriteAnimationSetDefinition` asset. It must point to exactly one grid-based `SpriteSheetDefinition` (with `AutoGenerateGridFrames` enabled).
2. Define your animation clips. Each clip is authored using `Row`, `StartColumn`, and `FrameCount` (keeping the clip on a single sprite-sheet row).
3. On your GameObject, attach *both* `SpriteDataAuthoring` and `SpriteAnimationAuthoring`.
4. In `SpriteAnimationAuthoring`, assign the `SpriteAnimationSetDefinition`, choose the `StartAnimation` name, `PlaybackSpeed`, and toggle `PlayOnStart`.
5. *(Example: See `Assets/Prefabs/SwordMan/SwordMan.prefab` and `Assets/Resources/SpriteAnimations/SwordManAnimations.asset`)*

## 6. Advanced / Workflow Tips

- **Benchmarking / Spawning:** Add `EntitiesReferenceAuthoring` plus `SpawnSettingsAuthoring` in scenes where you want the optional spawn benchmark workflow to stress-test the renderer.
- **Triple Buffering:** The `SpriteSystem` utilizes a triple-buffered ComputeBuffer approach for uploading instance data. This ensures that the CPU can write to the next frame's buffer without waiting for the GPU to finish reading the current frame's buffer, maximizing throughput.
- **Camera Limitations:** The `SpriteCullingSystem` currently relies on `Camera.main` and assumes an orthographic projection to calculate frustum bounds. Ensure your main camera is tagged correctly and is set to Orthographic mode.
- **Included Assets:** The package includes basic placeholder assets like `Third.mat`, `emojione-2.png`, and `Instan.shader` to help you get started immediately.