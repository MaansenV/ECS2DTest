# Packages/com.ecs2d.renderer/Editor/Styles/

## Responsibility
Holds USS/USS-related editor style assets for the ECS2D renderer inspector and editor UI. These files define visual presentation only, not behavior.

## Design
Style assets are kept alongside editor code and applied through standard Unity UI Toolkit style sheets. They centralize spacing, layout, typography, and state-specific visuals for consistent package UI.

## Flow
Editor windows and inspectors load the style sheets during UI construction, then bind them to containers and controls. The styles cascade through the visual tree and override default UI Toolkit appearance where needed.

## Integration
Used by `Packages/com.ecs2d.renderer/Editor` UI elements, custom inspectors, and any editor tools that share the package visual language. Changes here affect only editor presentation and do not impact runtime code.
