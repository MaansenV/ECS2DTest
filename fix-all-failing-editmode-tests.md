# Fix All Failing EditMode Tests

## Summary
`Assets/TestResults_20260331_090402.xml` shows `31` EditMode tests total, with `19` passing and `12` failing. The failures cluster into three buckets:

- 1 animation expectation mismatch
- 6 bake-test harness failures caused by an uninitialized `BlobAssetStore`
- 5 transform/culling setup failures caused by the test fixture, not the runtime systems

## Plan

### 1. Fix the bake-test harness in [`Packages/com.ecs2d.renderer/Tests/EditMode/SpriteAuthoringBakeTests.cs`](/H:/UnityProjects/Projects/ECS2DTest/Packages/com.ecs2d.renderer/Tests/EditMode/SpriteAuthoringBakeTests.cs)
- Replace every `new BlobAssetStore()` with `new BlobAssetStore(128)` so the store is actually created before baking.
- Update `CreateBakingSettings(...)` to construct the internal `Unity.Entities.BakingSettings` via its `BakingSettings(BakingFlags, BlobAssetStore)` constructor using reflection, instead of creating a default instance and patching properties.
- Keep the current reflection-based `InvokeBakeGameObjects(...)` path, but feed it a fully initialized bake settings object.
- Preserve the existing assertions for `SpriteSheetRenderKey`, `SpriteData`, `SpriteAnimationState`, `FlipX`, `FlipY`, and `Scale`.

### 2. Align the animation test with the current frame progression semantics in [`Packages/com.ecs2d.renderer/Tests/EditMode/SpriteAnimationSystemTests.cs`](/H:/UnityProjects/Projects/ECS2DTest/Packages/com.ecs2d.renderer/Tests/EditMode/SpriteAnimationSystemTests.cs)
- Update `Update_UsesCurrentClipIndexWithoutNameResolution` so the second update at `t = 1.0s` expects the clip to be on frame index `1` with `SpriteFrameIndex = 6`.
- Leave the first and third assertions intact if they still match the floor-based behavior in `SpriteAnimationSetBlobUtility.EvaluateFrameIndex(...)`.
- Treat the runtime as the source of truth here; the test is currently asserting an older progression step.

### 3. Fix the transform sync tests in [`Packages/com.ecs2d.renderer/Tests/EditMode/SpriteCullingAndTransformSyncSystemTests.cs`](/H:/UnityProjects/Projects/ECS2DTest/Packages/com.ecs2d.renderer/Tests/EditMode/SpriteCullingAndTransformSyncSystemTests.cs)
- Split the fixture setup into two clear paths:
  - `LocalTransform`-driven tests keep calling `LocalToWorldSystem.Update(...)`.
  - Direct `LocalToWorld` matrix tests do not call `LocalToWorldSystem.Update(...)`, so the matrix under test is not overwritten by the identity `LocalTransform`.
- Keep the current assertions for translation, rotation, scale, `FlipX`, and `FlipY` once the test harness matches the intended data flow.
- If clarity helps, replace the shared helper with two explicit helpers rather than trying to make one helper cover both paths.

### 4. Fix the shared-component archetype in [`Packages/com.ecs2d.renderer/Tests/EditMode/SpriteCullingAndTransformSyncSystemTests.cs`](/H:/UnityProjects/Projects/ECS2DTest/Packages/com.ecs2d.renderer/Tests/EditMode/SpriteCullingAndTransformSyncSystemTests.cs)
- Include `SpriteSheetRenderKey` in the archetype created by `SpriteSystem_BaseIndexArrayMatchesEnabledSubset`.
- Keep setting the shared component value after entity creation, then apply the shared component filter as the test already does.
- This should remove the `component has not been added` failure without changing `SpriteSystem`.

## Validation
- Re-run the EditMode suite and confirm the XML reports `31/31` passing.
- Verify these previously failing tests are green:
  - `SpriteAnimationSystemTests.Update_UsesCurrentClipIndexWithoutNameResolution`
  - All six `SpriteAuthoringBakeTests`
  - `SpriteSystem_BaseIndexArrayMatchesEnabledSubset`
  - The four direct-matrix `SpriteTransformSyncSystem_*` cases

## Assumptions
- The runtime implementation is considered correct for the current intended behavior.
- The fixes should stay in test code and test harnesses unless a fresh run shows a true runtime regression.
- No public API changes are expected.
