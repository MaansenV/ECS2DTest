# Test Spec — DOTS 2D particle system for ECS2D renderer

## Scope
Validate the V1 reusable particle system planned in `.omx/plans/prd-dots-particle-system.md`.

The test plan assumes:
- package-local implementation in `Packages/com.ecs2d.renderer`
- `Authoring + Baker` as the primary entry point
- Circle-only spawn for V1
- pooled ECS particle entities
- explicit `inactive` / `active` / `resting` lifecycle semantics

## Acceptance Criteria Mapping
1. A particle emitter can be configured through package authoring/baking.
2. V1 supports burst count, spawn rate, max particles, lifetime min/max, speed min/max, start/end scale, start/end color, circle edge/area, direction fixed/random/outward, optional start rotation range, optional rotation speed range, and rest-phase timing.
3. Burst and looping emission both work in the same runtime model.
4. Random ranges affect per-particle spawn position, speed, lifetime, and optional rotation.
5. Scale and color change correctly over lifetime.
6. Resting particles leave active simulation queries but can remain visible.
7. The system remains package-reusable and does not depend on sample-only logic.
8. Ambient and impact-style proof effects both work.

## Unit / EditMode Tests
### A. Authoring and baking
- Baking writes emitter config correctly from authoring to ECS runtime data.
- Default values bake as expected for omitted optional fields.
- Invalid or missing required references fail predictably and clearly.

Suggested touchpoints:
- mirror the existing style of `SpriteAuthoringBakeTests`
- add particle-specific bake tests under `Packages/com.ecs2d.renderer/Tests/EditMode/`

### B. Capacity and pool semantics
- `MaxParticles` defines the maximum active/rest-capable particle capacity for an emitter.
- Emission does not instantiate/destroy entities in the hot path once initialized.
- Expired particles return to the inactive pool state.

### C. Circle spawn behavior
- `Circle Edge` spawns particles on the circumference.
- `Circle Area` spawns particles within the circle interior.
- Spawned positions respect emitter origin/space rules chosen by implementation.

### D. Direction initialization
- `Fixed` direction uses the configured direction.
- `Random` direction covers the intended angular range/space.
- `Outward` direction points away from the sampled circle spawn point.

### E. Random range semantics
- Lifetime is sampled within min/max bounds.
- Speed is sampled within min/max bounds.
- Optional start rotation and rotation speed stay within bounds.
- Burst siblings do not collapse to identical sampled values unless the configured range is degenerate.
- Seed behavior is deterministic enough for repeatable tests where appropriate.

### F. Over-lifetime behavior
- Scale interpolates from start to end across the particle lifetime.
- Color interpolates from start to end across the particle lifetime.
- Optional rotation speed updates orientation correctly during active simulation.

### G. Rest-phase behavior
- Particles transition into resting after the configured rest timing/easing conditions are met.
- Resting particles no longer appear in the active simulation query.
- Resting particles keep the expected visible render state until expiry or recycle.
- Expiry and rest are tested as separate concepts.

### H. Renderer integration
- Particle visuals populate the expected render-facing data path, likely `SpriteData`.
- Existing renderer batch/filter assumptions remain valid after particle integration.
- If the implementation bypasses `LocalTransform`, tests should prove render output still updates correctly.
- If the implementation uses `LocalTransform`, tests should prove resting particles avoid unnecessary transform-sync work where intended.

## Integration / Manual Verification
1. **Ambient proof effect**
   - Looping emitter with visible random speed/lifetime differences.
   - Confirm particles diverge naturally and do not move in lockstep.

2. **Impact/blood burst proof effect**
   - Burst emitter with multiple particles.
   - Confirm randomized velocity/lifetime/rotation creates a natural spread.
   - Confirm configured rest transition can leave settled particles visible.

3. **Rest-heavy scene**
   - Run an effect long enough that many particles are in the resting phase.
   - Confirm visual correctness and reduced active update pressure.

4. **Idle emitter case**
   - Confirm no unnecessary per-particle work happens when nothing active is emitting or moving.

## Performance Verification
### Required scenarios
1. **Active-heavy**
   - Many moving particles from looping emission.
2. **Rest-heavy**
   - Many visible settled/resting particles after initial activity.

### Measurements
- Total particle count
- Active particle count
- Resting particle count
- Relevant simulation system cost
- Relevant presentation/render cost
- Baseline vs post-change comparison where possible

### Reporting
- Particle counts used
- Scene/setup summary
- Frame-time observations
- Where cost moved after particles entered rest
- Whether the pooled ECS design stayed inside the intended V1 envelope

## Known Gaps to Watch
- Resting particles accidentally remaining in hot queries.
- Hidden transform-sync duplication if both `LocalTransform` and direct render data are updated.
- Seed behavior that looks random visually but is hard to test or replay.
- Pool bookkeeping bugs causing particles to leak, overlap slots, or fail to recycle.
- Pressure to add Point/Box shapes before Circle-only V1 is proven.

## Exit Criteria
The plan is test-ready when:
- baking tests exist
- runtime feature tests cover spawn/direction/random/over-lifetime/rest
- at least one ambient and one impact proof path exist
- performance evidence covers both active-heavy and rest-heavy cases
- no unresolved ambiguity remains about whether resting particles truly leave active simulation work
