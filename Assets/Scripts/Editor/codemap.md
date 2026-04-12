# Assets/Scripts/Editor/

Editor-only utility layer for authoring support, kept outside runtime assemblies. This folder hosts curve preset generation/editing helpers and small expression-evaluation utilities used by editor tooling.

## Responsibility

Provide custom Inspector/Window tooling for creating and maintaining curve presets and related serialized assets. Keep all editor dependencies isolated behind the `Assets.Scripts.Editor` asmdef boundary so runtime code does not reference UnityEditor APIs.

## Design

Small, task-focused editor classes: a window drives preset authoring, a writer materializes preset data, and an expression evaluator supports user-entered curve formulas. The code favors direct file/asset emission over shared runtime services to minimize coupling.

## Flow

User opens the curve preset window, enters parameters or expressions, and triggers generation. The editor layer evaluates expressions, builds preset data, and writes the resulting assets/files through the preset writer.

## Integration

Integrates with Unity editor UI, asset serialization, and the runtime curve-preset consumers indirectly through generated outputs. The asmdef boundary keeps this folder editor-only while allowing produced presets to be used by non-editor code.
