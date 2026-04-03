# Deep Interview Summary — Sprite Sorting Renderer

- Profile: standard
- Context: brownfield
- Final ambiguity: 0.10
- Threshold: 0.20
- Context snapshot: `.omx/context/sprite-sorting-renderer-20260403T175600Z.md`

## Transcript Summary
1. User wants SpriteRenderer-like control so one sprite can always render above another.
2. Existing behavior is perceived as Y-driven and should remain acceptable within a bucket.
3. Confirmed precedence: sorting layer first; Y-ordering only inside the same layer bucket.
4. `OrderInLayer` is not needed for v1.
5. Sorting must work across different sprite sheets / material groups.
6. v1 should be a small performant custom ECS solution, not full Unity SortingLayer integration.
7. Performance success criterion: sorting overhead should be hardly noticeable at 20k+ sprites.

## External Plan Evaluation
The external plan at `c:\Users\viola\.cursor\plans\sprite_render-reihenfolge_d540536e.plan.md` is technically plausible for intra-sheet sorting, but it is incomplete for this task because the user requires global cross-sheet layer precedence and does not want `OrderInLayer` or full Unity layer integration in v1.
