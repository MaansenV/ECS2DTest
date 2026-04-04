Metadata
- Profile: standard
- Rounds: 7
- Final ambiguity: 0.16
- Threshold: 0.20
- Context type: brownfield
- Context snapshot: `.omx/context/particle-system-2d-dots-20260403T221019Z.md`
- Transcript: `.omx/interviews/particle-system-2d-dots-20260404T065240Z.md`

Clarity breakdown
- Intent: 0.88
- Outcome: 0.90
- Scope: 0.92
- Constraints: 0.92
- Success: 0.84
- Context: 0.88

Intent
- Build a very performant, reusable 2D DOTS particle system for `com.ecs2d.renderer`.
- Cover a compact Shuriken-like baseline without turning V1 into a large generic particle framework.

Desired outcome
- A package-level particle feature that supports both:
- looping ambient particles
- impact/blood burst effects
- It should feel natural through random variation and basic over-lifetime behavior, while remaining efficient for large counts and long-running effects.

In scope
- Package implementation inside `Packages/com.ecs2d.renderer`
- Unity-facing `Authoring + Baker` workflow as the primary setup surface
- DOTS-native runtime using ECS data and Burst-friendly systems
- Basic emission controls:
- Burst Count
- Spawn Rate
- Max Particles
- Lifetime random range:
- Lifetime Min / Max
- Speed random range:
- Speed Min / Max
- Scale over lifetime:
- Start Scale / End Scale
- Color over lifetime:
- Start Color / End Color
- Single spawn shape in V1:
- Circle only
- Circle spawn area modes:
- Edge
- Area
- Direction modes for circle:
- Outward
- Random
- Fixed
- Optional rotation randomization:
- Start Rotation Min / Max
- Rotation Speed Min / Max
- Randomized spawn position within the circle shape
- Randomized per-particle speed/lifetime/rotation so particles do not move identically
- Support for both continuous emission and burst emission
- Support for a rest phase:
- configurable time until rest
- speed easing toward rest
- settled/resting particles should become cheaper than actively simulated particles

Out of scope / Non-goals
- Point shape in V1
- Box shape in V1
- More than one shape family in V1
- Large “noise/module soup” feature expansion
- Full Shuriken parity
- Advanced curves/gradients authoring beyond compact start/end + min/max ranges unless needed as an internal representation

Decision boundaries
- OMX may choose the exact ECS component layout, system split, and data packing strategy.
- OMX may choose the specific `ISystem`/job architecture as long as it stays Burst-friendly and package-reusable.
- OMX may infer a minimal, compact authoring UI instead of exposing every future extension point now.
- OMX should preserve Circle-only scope for V1 and avoid quietly re-expanding to Point/Box.

Constraints
- Must integrate with the existing renderer package instead of living only in `Assets`.
- Must follow DOTS patterns suitable for high entity counts:
- `ISystem` preferred
- Burst where possible
- avoid structural churn in hot paths
- group similar particle archetypes cleanly
- Must work well with the existing `SpriteData` rendering path or a renderer-adjacent path that preserves the package’s batching/per-instance model.
- Long-lived settled particles should not keep paying the same runtime cost as actively moving particles.
- Primary user setup path should be MonoBehaviour authoring converted by a Baker.

Assumptions exposed and resolutions
- Assumption: V1 should support multiple shapes because the original list mentioned Point/Circle/Box.
  Resolution: rejected; V1 is Circle-only.
- Assumption: all particles should use the same simulation path.
  Resolution: rejected; V1 should support active simulation and a cheaper rest phase for settled particles.
- Assumption: a pure ECS runtime surface is enough for first adoption.
  Resolution: rejected; V1 should expose Authoring + Baker first.

Pressure-pass findings
- Scope was reduced from “multiple shapes” to “Circle-only” after challenging bloat risk.
- The vague performance goal became an explicit requirement for differentiated lifecycle cost:
- moving particles are simulated
- rested particles transition into a cheap state

Brownfield evidence vs inference
- Evidence: `Packages/com.ecs2d.renderer/Runtime/SpriteData.cs` already carries per-instance color, scale, rotation-related data, sprite sheet frame, and depth fields.
- Evidence: `Packages/com.ecs2d.renderer/Runtime/SpriteSystem.cs` already uses indirect instanced drawing with grouped sprite buffers.
- Evidence: `Packages/com.ecs2d.renderer/Runtime/SpriteTransformSyncSystem.cs` syncs render state from `LocalToWorld`.
- Inference: the first particle implementation should likely reuse `SpriteData`-driven rendering rather than invent a totally separate renderer unless a later planning step proves a clear performance reason.

Technical context findings
- The repo already has a reusable rendering package, so particle runtime belongs there.
- The project sample currently has only a simple grid spawner and no particle architecture to preserve.
- The current renderer already supports the visual fields V1 particles need for:
- scale
- color
- rotation
- frame selection

Testable acceptance criteria
- A user can add a particle emitter through `Authoring + Baker` without writing ECS boilerplate manually.
- The emitter can be configured for:
- burst count
- spawn rate
- max particles
- lifetime min/max
- speed min/max
- start/end scale
- start/end color
- circle shape with edge/area spawn
- direction mode fixed/random/outward
- optional rotation min/max and rotation speed min/max
- Two particles spawned from the same burst can visibly differ because random ranges are applied per particle.
- The system can produce both:
- an ambient looping effect
- an impact/blood burst effect
- Particles can scale down and disappear after their lifetime.
- Rest-phase-capable particles can transition to a settled cheap state after a configured time.
- The implementation remains package-reusable and does not depend on sample-scene-only code.
- Verification should include at least one runtime/demo proof for ambient and one for impact-style burst behavior.

Recommended execution handoff
- Recommended next step: `$ralplan`
- Reason: requirements are now clear enough, but architecture choices still matter:
- how emitters, particles, and rest states are modeled
- whether rendering fully reuses `SpriteData`
- what verification/tests best protect behavior and performance

Suggested invocation
- `$plan --consensus --direct .omx/specs/deep-interview-particle-system-2d-dots.md`

Alternative handoffs
- `$autopilot .omx/specs/deep-interview-particle-system-2d-dots.md`
- `$ralph .omx/specs/deep-interview-particle-system-2d-dots.md`
- `$team .omx/specs/deep-interview-particle-system-2d-dots.md`
- Refine further
