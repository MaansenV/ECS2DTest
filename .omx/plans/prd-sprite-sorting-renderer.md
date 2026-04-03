# PRD — Performant global SortingLayer for ECS2D sprite renderer

## Requirements Summary
Source of truth: `.omx/specs/deep-interview-sprite-sorting-renderer.md`

The renderer must add a small custom `SortingLayer` feature that works across different sprite sheets/material groups while keeping runtime overhead low enough to remain barely noticeable with 20k+ sprites. The user rejected `OrderInLayer` and full Unity `SpriteRenderer` parity for v1. The current renderer batches by `SpriteSheetRenderKey` and draws each batch separately via `Graphics.DrawMeshInstancedIndirect` (`Packages/com.ecs2d.renderer/Runtime/SpriteSystem.cs:124-160`), so a purely per-sheet solution does not satisfy the clarified requirement.

## RALPLAN-DR Summary
### Principles
1. Preserve batching and avoid per-frame CPU sort work unless profiling proves it necessary.
2. Make `SortingLayer` semantics global across sheets, not local to a single render group.
3. Keep v1 small: custom ECS integers only, no `OrderInLayer`, no Unity layer-name system.
4. Prefer explicit, testable render-order semantics over inferred scene behavior.
5. Do not silently break existing gameplay uses of world Z; any change to render depth must be isolated or clearly documented.
6. Maintain a fast path for projects that leave sorting at its default layer.

### Decision Drivers
1. 20k+ sprite scenes require near-flat overhead.
2. Global layer precedence must work across `SpriteSheetRenderKey` groups.
3. Intra-layer ordering should remain the current 'foreground/background by vertical placement' gameplay behavior the user expects.

### Viable Options
#### Option A — Dedicated render-depth key derived from layer + vertical order (**favored**)
**Approach:** Add `SortingLayer` to sprite runtime data and derive a dedicated render-depth value from `(SortingLayer, vertical sort value, deterministic tie-break)` instead of CPU sorting the upload stream.
**Pros:**
- Preserves current per-sheet batching and indirect draws.
- Works across sheets because depth testing is global, not group-local.
- Avoids O(N log N) CPU sorting and dirty-cache complexity.
- Small runtime diff: mostly data + transform/render-depth calculation + tests.
**Cons:**
- Requires choosing/documenting a safe depth encoding and precision budget.
- Needs explicit decision about whether vertical sort uses transform Y, pivot/foot Y, or another anchor.
- Must avoid silently repurposing gameplay/world Z if it already carries meaning elsewhere.

#### Option B — Global layer buckets + sorted per-layer draw passes
**Approach:** Split render groups by `(SortingLayer, SheetId)` and iterate globally by layer, keeping per-group indirect draws.
**Pros:**
- Simple conceptual model for layer precedence.
- Lower architectural risk than a full explicit per-entity sort.
**Cons:**
- Same-layer ordering across different sheets is still not guaranteed unless another cross-sheet tie-break exists.
- Likely increases draw pass fragmentation as layer counts rise.
- May still require a second mechanism for Y-based tie-breaks.

#### Option C — Explicit gather/sort/permute with composite sort keys
**Approach:** Gather entities, build composite keys `(SortingLayer, Y, tie-break, sheet)`, sort, then permute upload buffers.
**Pros:**
- Most explicit and flexible ordering semantics.
- Easiest to extend later to `OrderInLayer`.
**Cons:**
- Highest steady-state CPU cost.
- Dirty/caching complexity becomes important immediately.
- Larger diff and more verification burden for v1.

### Invalidated alternative rationale
Option B does not fully satisfy the clarified same-layer cross-sheet ordering requirement on its own. Option C satisfies semantics but violates the performance-first principle for v1 unless profiling proves the depth-bias path insufficient.

## Chosen Direction
Choose **Option A** for v1: encode a dedicated global render-depth key from `SortingLayer` and vertical placement so that the GPU depth test enforces both cross-sheet layer precedence and intra-layer vertical ordering without introducing per-frame CPU sorting.

## Why chosen
- The current renderer already writes per-instance position into the instance buffer and the shader writes depth (`Packages/com.ecs2d.renderer/Runtime/SpriteTransformSyncSystem.cs:19-24,52-53`, `Packages/com.ecs2d.renderer/Runtime/Shaders/Instan.shader:15,54-68`).
- That makes depth-biasing the smallest change that naturally works across all draw calls, including separate sheets.
- It avoids the explicit dirty/cache system the user asked about, which would be less valuable once movement changes the vertical tie-break frequently.
- A dedicated render-depth path preserves an escape hatch if gameplay/world Z semantics need to remain unchanged.

## Requirements / Acceptance Criteria
1. Sprites expose a custom integer `SortingLayer` in ECS/runtime authoring for v1.
2. A sprite with higher `SortingLayer` renders in front of a lower layer even when the sprites use different `SpriteSheetDefinition` assets.
3. Within the same `SortingLayer`, vertical ordering follows `LocalToWorld.Position.y` as the documented v1 tie-break rather than draw-call order.
4. Equal-layer/equal-vertical-position cases use a deterministic tie-break so ordering does not flicker across sheets.
5. Default content keeps a low-friction fast path (for example layer `0` with no extra editor system beyond a simple field/component).
6. Tests cover cross-sheet precedence and same-layer vertical tie-break behavior.
7. Verification includes an empirical many-sprite comparison (sorting baseline vs sorting-enabled) showing overhead is small enough to be "kaum spürbar" for representative 20k+ scenes.

## Implementation Steps
1. **Codify the sort data model**
   - Add a small runtime field/component for `SortingLayer`.
   - Likely touchpoints: `Packages/com.ecs2d.renderer/Runtime/SpriteData.cs`, `Packages/com.ecs2d.renderer/Runtime/SpriteDataAuthoring.cs`, and any animation/authoring paths that construct `SpriteData`.
   - Decide whether vertical tie-break is derived from `LocalToWorld.Position.y`, a pivot/foot anchor, or another explicit signal; prefer derivation to avoid extra authoring burden, but document the chosen anchor.

2. **Introduce explicit render-depth derivation**
   - Update transform sync or render upload logic so the depth written for rendering is derived from `SortingLayer` plus vertical placement instead of relying on incidental world Z.
   - Primary touchpoints: `Packages/com.ecs2d.renderer/Runtime/SpriteTransformSyncSystem.cs` and/or `Packages/com.ecs2d.renderer/Runtime/SpriteSystem.cs`.
   - Prefer a dedicated render-depth path or clearly isolated render-only value so gameplay/world Z semantics are not silently repurposed.
   - Preserve deterministic ordering inside a layer and document any tie-break for identical Y values.

3. **Keep batching intact and avoid CPU sorting**
   - Leave the per-sheet `renderGroups` + `DrawMeshInstancedIndirect` architecture intact (`SpriteSystem.cs:124-160`).
   - If needed, only add minimal per-group metadata or material/shader support; do not add per-frame gather/sort/permute in v1.

4. **Guard correctness with focused tests**
   - Extend edit-mode tests around `SpriteSystem`/transform sync to prove: (a) higher layer beats lower layer across sheets; (b) same-layer vertical order is stable and documented.
   - Likely touchpoints: `Packages/com.ecs2d.renderer/Tests/EditMode/SpriteCullingAndTransformSyncSystemTests.cs` plus new tests if needed.

5. **Measure the performance claim**
   - Add a lightweight benchmark/profiler harness or documented manual performance scenario comparing before/after with representative many-sprite counts.
   - Reuse existing package test/performance infrastructure if available; otherwise document a reproducible profiling scene/setup.

## Risks and Mitigations
- **Risk:** Depth encoding range/precision can cause z-fighting or clipping.
  - **Mitigation:** Reserve explicit layer stride, clamp documented vertical span, and test with representative camera ranges.
- **Risk:** Existing gameplay may already use world Z for something meaningful.
  - **Mitigation:** Keep render-depth separate from gameplay/world Z unless code inspection during implementation proves repurposing is safe.
- **Risk:** The project's current 'Y sorting' may actually be inferred rather than implemented.
  - **Mitigation:** Make the tie-break rule explicit in code/tests instead of preserving an undocumented emergent behavior.
- **Risk:** Using transform center Y may not match the intended foot/pivot order.
  - **Mitigation:** Document the chosen anchor in v1 and isolate the calculation so a later pivot-based refinement is localized.
- **Risk:** Transparent/cutout behavior plus ZWrite can produce edge cases.
  - **Mitigation:** Verify with overlapping alpha-cutout sprites in same and different sheets.

## Verification Steps
1. Unit/edit-mode tests proving global layer precedence across sheets.
2. Unit/edit-mode tests proving same-layer vertical tie-break behavior, including equal-Y deterministic fallback.
3. Manual scene verification with at least two sheets and multiple layers.
4. Performance capture in a 20k+ sprite scenario comparing before/after.

## ADR
### Decision
Use a custom ECS `SortingLayer` plus dedicated derived render depth instead of explicit CPU sorting for v1.

### Drivers
- Global ordering must cross sheet boundaries.
- Performance must stay nearly flat at 20k+ sprites.
- v1 should remain small and easy to reason about.

### Alternatives considered
- Per-layer draw buckets.
- Global gather/sort/permute with composite keys.

### Why chosen
It best preserves batching and avoids recurring sort work while satisfying the clarified semantics.

### Consequences
- Render ordering becomes an explicit depth-encoding concern.
- Future `OrderInLayer` support can be layered on top later if needed.
- Tests/documentation must define the exact vertical tie-break.
- Implementation should avoid silently overloading gameplay/world Z unless explicitly validated.

### Follow-ups
- If profiling shows depth-bias is insufficient or precision-limited, revisit Option C with targeted dirty-region optimization rather than whole-frame global sorts.

## Available-Agent-Types Roster
- `planner` — plan refinement / sequencing
- `architect` — design review / tradeoffs
- `critic` — plan quality gate
- `executor` — implementation
- `test-engineer` — test design + perf verification
- `verifier` — final validation evidence
- `debugger` — if render-order regressions appear

## Follow-up Staffing Guidance
### Ralph path
- `executor` (high): implement runtime/data-path changes
- `test-engineer` (medium): add correctness + perf verification
- `verifier` (high): confirm acceptance criteria and benchmark evidence

### Team path
- Lane 1: `executor` (high) — runtime data model + render-depth derivation
- Lane 2: `test-engineer` (medium) — edit-mode tests + perf harness/docs
- Lane 3: `verifier` (high) — review semantics, risk checks, acceptance proof

## Launch Hints
- Ralph: `$ralph .omx/plans/prd-sprite-sorting-renderer.md`
- Team: `$team .omx/plans/prd-sprite-sorting-renderer.md`
- Team CLI hint: `omx team ".omx/plans/prd-sprite-sorting-renderer.md"`

## Team Verification Path
1. Team proves runtime behavior with tests and a reproducible multi-sheet scene.
2. Team captures performance evidence for representative 20k+ counts.
3. Ralph/verifier confirms no requirement drift vs the deep-interview spec before shutdown.

## Changelog
- Initial consensus draft created from deep-interview artifact.`r`n- Reviewer feedback applied: make the vertical anchor explicit (`LocalToWorld.Position.y` for v1), preserve gameplay/world-Z semantics, and require deterministic equal-Y tie behavior in tests.
- Favored option shifted away from the external per-sheet CPU-sort plan toward a depth-bias plan due to the clarified cross-sheet requirement and performance priority.
- Added architectural guardrails for dedicated render-depth, deterministic equal-Y tie-breaks, and explicit vertical-anchor documentation.


