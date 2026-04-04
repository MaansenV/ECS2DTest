# PRD — Reusable DOTS 2D particle system for ECS2D renderer

## Requirements Summary
Source of truth: `.omx/specs/deep-interview-particle-system-2d-dots.md`

Add a reusable, high-performance 2D particle system to `Packages/com.ecs2d.renderer` that extends the existing renderer package rather than bypassing it. V1 stays intentionally compact: `Authoring + Baker`, Circle-only spawn, burst + looping emission, randomized per-particle values, color/scale over lifetime, and a rest phase so settled particles become much cheaper than actively simulated ones.

Brownfield anchors:
- `Packages/com.ecs2d.renderer/Runtime/SpriteData.cs`
- `Packages/com.ecs2d.renderer/Runtime/SpriteDataAuthoring.cs`
- `Packages/com.ecs2d.renderer/Runtime/SpriteSystem.cs`
- `Packages/com.ecs2d.renderer/Runtime/SpriteTransformSyncSystem.cs`
- `Packages/com.ecs2d.renderer/Tests/EditMode/SpriteAuthoringBakeTests.cs`
- `Packages/com.ecs2d.renderer/Tests/EditMode/SpriteCullingAndTransformSyncSystemTests.cs`

## RALPLAN-DR Summary
### Principles
1. Reuse the existing `SpriteData` render path before inventing a second particle renderer.
2. Keep V1 narrow: Circle-only, compact Shuriken basics, no module sprawl.
3. Avoid hot-path structural churn; prefer preallocated capacity, pooling semantics, and cheap state transitions.
4. Make active vs rest lifecycle cost an explicit feature rather than an incidental optimization.
5. Keep authoring simple through MonoBehaviour + Baker, while keeping runtime DOTS/Burst-friendly.

### Decision Drivers
1. One reusable system must cover both ambient looping particles and impact/blood bursts.
2. Performance matters more than breadth, especially for long-lived or partially settled effects.
3. The repo already has a renderer package with per-instance visual data and indirect instancing, so architectural reuse has real value.

### Viable Options
#### Option A — Reuse `SpriteData` with pooled ECS particle entities and explicit lifecycle states (**favored**)
**Approach:** Build particles inside `com.ecs2d.renderer` as preallocated ECS entities bound to emitters, rendered through `SpriteData`, with at least `inactive`, `active`, and `resting` lifecycle states. Active particles participate in per-frame simulation; resting particles do not.
**Pros:**
- Smallest diff from the current package.
- Natural fit for `Authoring + Baker`.
- Reuses current renderer batching and existing tests/patterns.
- Makes rest-state cost control explicit and testable.
**Cons:**
- Entity-per-particle still has ECS/query overhead.
- Poor state/query design could leave resting particles paying too much cost.
- Needs clear rules about whether particles use `LocalTransform` or write render-facing data directly.

#### Option B — Emitter-owned dense buffers plus a dedicated particle upload/render path
**Approach:** Emitters own dense particle arrays/buffers and update/upload them directly, minimizing per-particle ECS surface area.
**Pros:**
- Best theoretical performance ceiling.
- Avoids entity-per-particle overhead.
- Makes rest-state packing/filtering very cheap.
**Cons:**
- Highest V1 complexity and risk.
- Duplicates renderer concepts that already exist in the package.
- Harder to expose cleanly through the current package’s existing authoring/runtime patterns.
- More difficult to validate incrementally with existing test coverage.

#### Option C — ECS emitters that instantiate/destroy particle entities on demand
**Approach:** Spawn runtime entities for particles when needed and destroy them when done.
**Pros:**
- Simplest mental model.
- Fastest path to a rough prototype.
**Cons:**
- Directly conflicts with the performance-first goal.
- Hot structural churn becomes a bottleneck quickly.
- Rest phases do not solve the churn problem.

### Invalidated alternative rationale
Option C is inconsistent with the stated performance goal and is not viable for the first package version. Option B remains architecturally valid, but the current package already supplies the key render fields V1 needs. That makes Option B premature unless profiling shows pooled ECS particles cannot meet the expected counts or idle-cost requirements.

## Chosen Direction
Choose **Option A** for V1: implement a reusable particle system inside `Packages/com.ecs2d.renderer` using preallocated pooled ECS particle entities rendered through the existing `SpriteData` path, with explicit state/query separation so resting particles fall out of active simulation work.

## Why Chosen
- The current package already renders the visual fields V1 needs: position, rotation, scale, color, frame, depth.
- Existing package usage already follows `Authoring + Baker`, so adoption cost stays low.
- Pooling plus query-separated lifecycle states directly matches the clarified performance intent.
- This keeps a clear escape hatch to a denser emitter-buffer renderer later if profiling proves entity-per-particle V1 insufficient.

## Architectural Guardrails
1. **Resting particles must leave hot simulation queries.**
   - Model lifecycle state so active simulation queries only see active particles.
   - Prefer `IEnableableComponent` or explicit simulation tags/state bits over add/remove/destroy loops.
2. **Do not require `LocalTransform` unless it buys something important.**
   - If particle simulation can write render-facing fields directly to `SpriteData`, prefer that over a second transform-sync hop.
   - If `LocalTransform` is kept for consistency, document the extra sync cost and make sure resting particles can avoid unnecessary updates.
3. **Keep pooling local and bounded.**
   - Bind particle capacity to emitter `MaxParticles` rather than introducing a global allocator in V1.
4. **Preserve a future renderer escape hatch.**
   - Keep emitter config, particle state, and render-binding boundaries clean enough that Option B can replace the presentation path later without rewriting authoring semantics.

## Requirements / Acceptance Criteria
1. The package exposes a particle emitter through `Authoring + Baker`.
2. V1 supports:
   - Burst Count
   - Spawn Rate
   - Max Particles
   - Lifetime Min / Max
   - Speed Min / Max
   - Start Scale / End Scale
   - Start Color / End Color
   - Circle shape only
   - Circle mode: `Edge` or `Area`
   - Direction mode: `Fixed`, `Random`, `Outward`
   - Optional Start Rotation Min / Max
   - Optional Rotation Speed Min / Max
   - Rest-phase timing
   - Speed easing sufficient to settle into rest
3. The same runtime supports both looping ambient emission and burst emission.
4. Two particles from the same burst can visibly diverge through randomized spawn position, speed, lifetime, and optional rotation.
5. Scale and color interpolate over lifetime using compact start/end semantics.
6. Active particles simulate every frame as needed; resting particles do not participate in the active simulation query.
7. Idle emitters and fully resting particle sets do not perform per-particle hot-path work just to remain visible.
8. The feature remains package-reusable and does not depend on sample-only code.
9. Verification demonstrates one ambient effect and one impact/blood burst effect.

## Implementation Steps
1. **Define authoring and baked emitter data**
   - Add a compact particle emitter authoring component in `Packages/com.ecs2d.renderer/Runtime/`.
   - Follow the style of `SpriteDataAuthoring`.
   - Bake config into emitter runtime components and prefab/entity references needed for pooled particle instances.

2. **Define particle runtime data and lifecycle**
   - Add particle components for age, lifetime, velocity/speed, optional rotation speed, random seed/state, emitter ownership/index, and rest-phase timing.
   - Add explicit lifecycle state for `inactive`, `active`, and `resting`.
   - Decide whether render updates go directly into `SpriteData` or through `LocalTransform`; prefer the smallest hot path.

3. **Preallocate bounded capacity**
   - Build per-emitter particle pools bounded by `MaxParticles`.
   - Avoid instantiate/destroy in the hot path; structural work should happen at setup/initialization, not every emission tick.

4. **Implement emission scheduling**
   - Support burst and looping emission from the same emitter config.
   - Define deterministic/random seed derivation so the same emitter can produce varied particles without all values lining up.

5. **Implement circle spawn and direction initialization**
   - Support `Circle Edge` and `Circle Area`.
   - Support `Fixed`, `Random`, and `Outward` directions.
   - Document how fixed direction is expressed in authoring/runtime data.

6. **Implement active simulation**
   - Update only active particles each frame.
   - Advance age, movement, optional rotation, scale interpolation, color interpolation, and rest-transition easing.

7. **Implement rest-phase transition**
   - After configured time/easing, transition eligible particles into a resting state.
   - Resting particles should retain visible render data but fall out of active motion/update queries.
   - If fully expired, return particles to the inactive pool state without churn-heavy destruction.

8. **Bind to the renderer package**
   - Reuse `SpriteData`, `SpriteCullState`, and `SpriteSheetRenderKey` where appropriate.
   - Keep the existing `SpriteSystem` batching path intact unless implementation reveals a specific incompatibility.

9. **Add regression and feature tests**
   - Extend package EditMode coverage for baking, spawn modes, random ranges, over-lifetime values, and rest-state behavior.

10. **Capture performance evidence**
   - Measure active-heavy and rest-heavy scenarios separately.
   - Compare with a baseline scene or baseline revision to prove the chosen V1 path is acceptable.

## Risks and Mitigations
- **Risk:** Entity-per-particle V1 still carries too much runtime overhead.
  - **Mitigation:** define benchmark targets and a pivot trigger to the dense-buffer alternative.
- **Risk:** Resting particles remain visible but still sit in expensive queries.
  - **Mitigation:** acceptance criteria and tests must prove active-query exclusion explicitly.
- **Risk:** `LocalTransform` + `SpriteTransformSyncSystem` duplicates work for particles.
  - **Mitigation:** decide early whether particles write `SpriteData` directly; do not drift into both paths accidentally.
- **Risk:** Pooling design becomes overcomplicated.
  - **Mitigation:** keep capacity local to each emitter; avoid global allocators and dynamic resizing complexity in V1.
- **Risk:** Randomization still looks repetitive or unstable.
  - **Mitigation:** make per-particle seed derivation explicit and add deterministic tests around configured ranges.
- **Risk:** Rest timing and lifetime semantics become muddled.
  - **Mitigation:** document and test them separately: lifetime expiry vs time-to-rest.

## Verification Steps
1. Baking tests for emitter config and defaults.
2. Spawn tests for Circle `Edge` and `Area`.
3. Direction tests for `Fixed`, `Random`, `Outward`.
4. Random range tests for lifetime, speed, position, and optional rotation.
5. Over-lifetime tests for scale and color interpolation.
6. Rest-phase tests proving particles leave active simulation queries while remaining renderable.
7. Ambient proof effect.
8. Impact/blood burst proof effect.
9. Performance capture for:
   - active-heavy looping emission
   - rest-heavy settled particles
10. Manual verification that idle/rest-heavy scenes stay visually correct without active motion updates.

## Performance Envelope and Pivot Criteria
### V1 target
- Accept pooled ECS particles as V1 if representative scenes show no material architecture break for expected use:
- ambient loop scenes remain stable without obvious simulation spikes
- rest-heavy scenes show clear reduction in active simulation work after settling

### Required evidence
- Capture one active-heavy run and one rest-heavy run with consistent particle counts, camera setup, and sprite sheet distribution.
- Record:
- total particle count
- active particle count
- resting particle count
- relevant simulation/presentation frame time deltas

### Pivot trigger
- Reconsider Option B if profiling shows either:
- resting particles still consume substantial per-particle simulation cost after transition
- active-heavy counts exceed acceptable frame cost with the pooled ECS design
- or the plan requires duplicated transform/render state that meaningfully erodes the reuse benefit

## ADR
### Decision
Implement V1 particles as pooled ECS particle entities inside `com.ecs2d.renderer`, rendered through the existing `SpriteData` path and managed with explicit active/rest lifecycle states.

### Drivers
- Reuse the current renderer package.
- Keep V1 compact and reusable.
- Support both moving and settled particles efficiently.
- Preserve a low-friction authoring path.

### Alternatives Considered
- Dedicated emitter-owned dense particle buffers and a custom renderer path.
- Simpler instantiate/destroy particle entities on demand.

### Why Chosen
This is the smallest architecture that still satisfies the clarified performance, authoring, and reuse goals, provided the implementation enforces real query separation for rest states and retains a measurable fallback threshold.

### Consequences
- Pooling and lifecycle state design become central.
- Tests must cover cost semantics, not only visuals.
- The plan must make a deliberate choice about transform sync vs direct render-data writes.
- A denser particle renderer remains an explicitly preserved future escape hatch.

### Follow-ups
- Revisit dense-buffer rendering only if profiling shows entity-based V1 is insufficient.
- Consider Point/Box shapes only after Circle-only V1 is validated.
- If particles need animation frames later, decide whether that lives in the same `SpriteData`-based path or a particle-specialized extension.

## Critic Verdict
`APPROVE after revision`

Critic conditions resolved in this final version:
- dense-buffer alternative explicitly invalidated for V1, not merely ignored
- active/rest query split made mandatory
- transform-sync duplication called out as an explicit decision point
- benchmark evidence and pivot criteria added

## Available-Agent-Types Roster
- `planner` — sequencing / plan refinement
- `architect` — architecture guardrails / tradeoffs
- `critic` — quality gate
- `executor` — implementation
- `test-engineer` — baking/runtime/perf tests
- `verifier` — acceptance and evidence validation
- `debugger` — regressions / perf diagnosis

## Follow-up Staffing Guidance
### Ralph path
- `executor` (high): authoring, particle data model, emitter/pool/simulation systems, `SpriteData` integration
- `test-engineer` (medium): baking/runtime/perf coverage
- `verifier` (high): ambient, burst, and rest-phase acceptance proof

### Team path
- Lane 1: `executor` (high) — emitter + particle data model, pooling, state queries, simulation
- Lane 2: `executor` or `test-engineer` (medium) — Authoring+Baker integration and EditMode tests
- Lane 3: `verifier` (high) — proof scenes/manual validation/perf evidence

Suggested reasoning by lane:
- architecture/data-path lane: `high`
- test/perf lane: `medium`
- verification lane: `high`

## Launch Hints
- Ralph: `$ralph .omx/plans/prd-dots-particle-system.md`
- Team: `$team .omx/plans/prd-dots-particle-system.md`
- Team CLI hint: `omx team ".omx/plans/prd-dots-particle-system.md"`

## Team Verification Path
1. Confirm baked/runtime data matches the PRD.
2. Prove ambient looping and impact burst behavior separately.
3. Prove resting particles leave active simulation queries while remaining render-correct.
4. Capture active-heavy and rest-heavy perf evidence.
5. Run a final verifier pass against the deep-interview spec before shutdown.

## Changelog
- Initial consensus draft created from `.omx/specs/deep-interview-particle-system-2d-dots.md`.
- Architect review forced the plan to make rest-state query exclusion explicit.
- Critic review required measurable benchmark evidence, pivot criteria, and an explicit decision about transform-sync duplication.
