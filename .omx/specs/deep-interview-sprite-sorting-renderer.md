# Deep Interview Spec — Performant Sprite Sorting for ECS Renderer

## Metadata
- Profile: standard
- Rounds: 6
- Final ambiguity: 0.10
- Threshold: 0.20
- Context type: brownfield
- Context snapshot: `.omx/context/sprite-sorting-renderer-20260403T175600Z.md`
- Transcript: `.omx/interviews/sprite-sorting-renderer-20260403T180100Z.md`

## Clarity Breakdown
| Dimension | Score |
|---|---:|
| Intent | 0.80 |
| Outcome | 0.95 |
| Scope | 0.98 |
| Constraints | 0.92 |
| Success Criteria | 0.72 |
| Context | 0.88 |

## Intent
The user wants a small, performant way to control draw priority similarly to Unity's SpriteRenderer, so selected sprites can always render above others even when the current renderer's effective ordering is driven by Y/depth behavior.

## Desired Outcome
Add a custom ECS `SortingLayer` capability to the existing renderer so that:
1. `SortingLayer` decides high-level draw priority globally.
2. The current Y-based ordering remains the tie-breaker within the same `SortingLayer`.
3. The solution remains suitable for scenes with 20k+ sprites.

## In Scope
- Introduce a custom integer-based `SortingLayer` field/component for ECS sprites.
- Make `SortingLayer` precedence work globally, including across different sprite sheets / render groups.
- Preserve the current Y-based ordering inside each layer bucket.
- Keep the solution minimal and performance-oriented.
- Reassess the existing renderer pipeline in `Packages/com.ecs2d.renderer/Runtime/SpriteSystem.cs`.
- Update or add tests proving global layer precedence and same-layer Y behavior.

## Out of Scope / Non-goals
- Full Unity `SortingLayer` name/ID/editor parity with `SpriteRenderer`.
- `OrderInLayer` in v1.
- GPU sorting as a first implementation choice.
- A large editor UX system for layer management.

## Decision Boundaries
OMX may decide without further confirmation:
- Whether `SortingLayer` lives inside `SpriteData` or as a separate ECS component, as long as the runtime path stays minimal and performant.
- Whether implementation uses globally sorted draw buckets, sorted group iteration, or another small design that preserves the required semantics.
- The exact test shape and internal sort-key encoding.

OMX must preserve:
- `SortingLayer` wins before Y ordering.
- Y ordering remains active inside the same `SortingLayer`.
- Cross-sheet ordering must work.
- v1 stays a small custom ECS solution.

## Constraints
- Existing renderer currently batches per `SpriteSheetRenderKey` and issues one indirect draw per render group: `Packages/com.ecs2d.renderer/Runtime/SpriteSystem.cs:124-160`.
- Existing sprite runtime data has no explicit sorting field: `Packages/com.ecs2d.renderer/Runtime/SpriteData.cs:6-15`.
- Existing authoring path writes `SpriteData` and `SpriteSheetRenderKey`: `Packages/com.ecs2d.renderer/Runtime/SpriteDataAuthoring.cs:46-59`.
- Performance matters at 20k+ sprites; sorting overhead should be hardly noticeable.

## Testable Acceptance Criteria
1. A sprite with higher `SortingLayer` renders above a sprite with lower `SortingLayer`, even when they belong to different sprite sheets.
2. Two sprites in the same `SortingLayer` continue to follow the current Y-based ordering behavior.
3. Sprites without explicit advanced ordering controls do not require `OrderInLayer` in v1.
4. The implementation does not attempt full Unity `SpriteRenderer` sorting-layer compatibility.
5. The renderer keeps a low-overhead path appropriate for 20k+ sprites; verification should include profiler/benchmark comparison of sorting off vs sorting on in a representative many-sprite scenario.

## Assumptions Exposed + Resolutions
- Assumption: the existing behavior is effectively Y-driven from the user's point of view. Resolution: preserve that behavior only as an intra-layer tie-breaker.
- Assumption: `OrderInLayer` may be unnecessary complexity for v1. Resolution: confirmed by user; exclude from v1.
- Assumption: per-sheet sorting is enough. Resolution: rejected by user; sorting must work globally across sheets.

## Pressure-pass Findings
- Revisited earlier assumption about SpriteRenderer parity.
- Result: user explicitly rejected full Unity sorting-layer integration and chose a smaller custom ECS layer model for v1.

## Brownfield Evidence vs Inference
- Evidence: current renderer loops render groups per sheet and draws each group separately (`SpriteSystem.cs:124-160`).
- Evidence: no explicit sorting field exists in `SpriteData` (`SpriteData.cs:6-15`).
- Inference: the external plan is insufficient because it centers on per-sheet ordering, while the clarified requirement is global cross-sheet layer precedence.

## Technical Context Findings
- Current architecture is compute-buffer based and optimized around per-sheet grouping.
- Therefore, the core design challenge is not merely adding a per-entity sort key; it is preserving batching/performance while making layer precedence global across groups.

## Condensed Transcript
- Q: Which sorting behavior is wanted?  
  A: SpriteRenderer-like ability for one sprite to always render above another; current Y behavior is okay but layer control is needed.
- Q: Should layer win before Y?  
  A: Yes.
- Q: Do we need `OrderInLayer` in v1?  
  A: No, only `SortingLayer`; afterwards it should continue to work via Y.
- Q: Must this work across different sprite sheets?  
  A: Definitely yes.
- Q: Should v1 mimic Unity sorting layer integration?  
  A: No; small performant custom solution.
- Q: What is the performance acceptance target?  
  A: It should be hardly noticeable.

## Recommended Execution Bridge
### Recommended next step: `$ralplan`
Use the spec as source of truth and produce a trustworthy implementation plan that explicitly solves the mismatch between global layer precedence and the current per-sheet render loop.

Suggested invocation:
`$plan --consensus --direct .omx/specs/deep-interview-sprite-sorting-renderer.md`

Other options:
- `$autopilot .omx/specs/deep-interview-sprite-sorting-renderer.md`
- `$ralph .omx/specs/deep-interview-sprite-sorting-renderer.md`
- `$team .omx/specs/deep-interview-sprite-sorting-renderer.md`
- Refine further
