# ECS2D Rendering

Reusable ECS sprite rendering core extracted from the sample project.

## Requirements
- Unity 6000.3 or newer
- Entities 1.4.x
- Burst, Collections, and Mathematics from the package manifest

## Install
- If this repository is used as a local package source, add `Packages/com.ecs2d.renderer` directly.
- If published to git, use Unity's Git URL package install with the repository URL and package path.

## Included runtime pieces
- `ECS2D.Rendering.SpriteSystem`
- `ECS2D.Rendering.SpriteData`
- `ECS2D.Rendering.SpriteSheetDefinition`
- `ECS2D.Rendering.SpriteSheetDatabase`
- `ECS2D.Rendering.SpriteAnimationClip`
- `ECS2D.Rendering.SpriteAnimationSetDefinition`
- `ECS2D.Rendering.SpriteAnimationAuthoring`
- `ECS2D.Rendering.SpriteAnimationState`
- `ECS2D.Rendering.SpriteAnimationSetReference`
- `ECS2D.Rendering.SpriteAnimationSystem`
- `ECS2D.Rendering.EntitiesReferences`
- `ECS2D.Rendering.SpriteDataAuthoring`
- `ECS2D.Rendering.EntitiesReferenceAuthoring`
- `Third.mat`, `emojione-2.png`, and `Instan.shader`

## Usage
1. Create one or more `SpriteSheetDefinition` assets under `Resources/SpriteSheets`.
2. For animated sprites, create a `SpriteAnimationSetDefinition` asset that points at exactly one sprite sheet and contains named clips.
3. Add `SpriteDataAuthoring` to a prefab for transform/color setup, then add `SpriteAnimationAuthoring` to the same GameObject and pick the start animation by name.
4. The animation baker stores the clip set as a blob reference and initializes `SpriteAnimationState` with the chosen clip.
5. `SpriteAnimationSystem` runs before `SpriteSystem`, advances the clip, and writes the final frame index back into `SpriteData.SpriteFrameIndex`.
6. `SpriteSystem` renders those baked entities in both the editor world and the play world.
7. Add `EntitiesReferenceAuthoring` plus `SpawnSettingsAuthoring` only in scenes where you want the optional spawn benchmark workflow.
8. A ready-made example is available on `Assets/Prefabs/SwordMan/SwordMan.prefab` with `Assets/Resources/SpriteAnimations/SwordManAnimations.asset`.
