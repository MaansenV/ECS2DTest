Metadata
- Profile: standard
- Context type: brownfield
- Final ambiguity: 0.16
- Threshold: 0.20
- Context snapshot: `.omx/context/particle-system-2d-dots-20260403T221019Z.md`

Condensed transcript

1. Q: Should the first version live in the reusable renderer package or only in project/sample code?
   A: Reusable.

2. Q: Which things should explicitly stay out of scope for V1?
   A: Keep it basic and avoid noise / feature bloat.

3. Q: Should V1 really support Point + Circle + Box, or only Circle first?
   A: Only Circle shape.

4. Q: Which effect type matters most: impact bursts, ambient looping particles, or both?
   A: Both equally; first use cases are ambient particles and blood impacts with bursts.

5. Q: For performance, should everything still simulate every frame, or should long-lived particles avoid per-frame checks?
   A: Moving ambient particles should simulate every frame; settled decal-like blood particles should not need per-frame work.

6. Q: Should V1 explicitly support a rest phase, or can that wait for V2?
   A: Rest phases should already be supported in V1, potentially with speed easing and a configured time when the particle settles.

7. Q: What should be the primary integration surface for Unity users: Authoring+Baker or pure ECS-first?
   A: Authoring + Baker.

Pressure-pass findings
- The initial broad shape list was challenged and reduced to Circle-only for V1.
- The vague performance goal was refined into two lifecycle classes:
- active moving particles requiring frame simulation
- settled/resting particles that should become very cheap after a configured rest transition

Brownfield evidence
- Renderer package exists in `Packages/com.ecs2d.renderer`.
- Current renderer draws `SpriteData` through the package runtime and already supports per-instance color, scale, rotation, frame index, flip, and depth.
- No existing particle/emitter runtime was found.
