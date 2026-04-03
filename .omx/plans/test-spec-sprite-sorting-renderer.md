# Test Spec — Sprite Sorting Renderer

## Scope
Validate the v1 custom `SortingLayer` implementation for the ECS2D renderer.

## Acceptance Criteria Mapping
1. Higher `SortingLayer` beats lower `SortingLayer` across different sprite sheets.
2. Same-layer sprites follow the documented vertical tie-break based on `LocalToWorld.Position.y` in v1.
3. The chosen vertical anchor is explicit and tested.
4. Equal-vertical-value ties resolve deterministically.
5. No `OrderInLayer` is required in v1.
6. The implementation remains a custom ECS solution, not Unity SpriteRenderer parity work.
7. If world Z semantics change, the behavior is documented and covered by tests/manual verification.
8. Sorting-enabled performance remains close to baseline in a representative 20k+ sprite scenario.

## Unit / EditMode Tests
### A. Runtime data / baking
- Baking/authoring writes the configured `SortingLayer` into runtime data.
- Default authoring produces the expected default layer value.

### B. Cross-sheet ordering semantics
- Construct two sprites using different `SpriteSheetRenderKey` values and different `SortingLayer` values.
- Verify the derived render-depth / sort value places the higher layer in front.
- Include inverse case to prove lower layer remains behind.

### C. Same-layer vertical tie-break
- Construct two sprites in the same layer with different `LocalToWorld.Position.y` values.
- Verify the documented front/back relation matches the implementation.
- Include equal-Y tie case and assert deterministic fallback behavior (for example stable entity/index order or another explicitly documented fallback).
- If v1 chooses transform Y instead of a pivot/foot anchor, assert/document that exact rule so future changes are intentional.

### D. Regression safety
- Ensure existing render-group batching/query behavior still counts the right entities after the data model change.
- Ensure animation/transform sync still updates sprite frame/transform data correctly with sorting enabled.

## Integration / Manual Verification
1. Scene with at least two sprite sheets and two layers.
2. Same-layer overlap case demonstrating vertical ordering.
3. Different-layer overlap case demonstrating layer precedence even when sheets differ.
4. Visual/manual confirmation that default-layer content still renders normally.

## Performance Verification
### Target
Sorting-enabled scenes at 20k+ sprites should be only lightly worse than baseline; any overhead should be difficult to notice during normal use.

### Method
- Capture baseline with current renderer in a representative scene.
- Capture after implementation with identical scene/entity count.
- Measure frame time and CPU main-thread / render-thread cost attributable to presentation/render systems.
- Prefer Unity profiler capture or automated performance test if available.

### Report
- Sprite count used
- Baseline frame time
- Post-change frame time
- Observed delta
- Notes about camera/setup and sheet/layer distribution

## Known Gaps to Watch
- Depth precision / z-fighting if layer stride is too small.
- Differences between editor and player profiling.
- Edge cases when gameplay also relies on world Z.
- Artist expectation mismatch if transform-center Y does not match desired foot-based sorting.


