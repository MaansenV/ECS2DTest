# tools/

## Responsibility

Release automation for the ECS2D renderer package. The folder hosts the scripted workflow that prepares versioned package releases and keeps repository release state consistent.

## Design

The folder is centered on `release-package.ps1`, a deterministic PowerShell entrypoint for packaging, version bumping, and release orchestration. The script treats package metadata as the source of truth and applies release steps in a repeatable order.

## Flow

Release flow starts from a target version, updates package versioning, stages release artifacts, and can optionally push tags and create a GitHub release. The script sequences validation, file updates, git operations, and publication as one controlled pipeline.

## Integration

Integrates with `Packages/com.ecs2d.renderer/package.json` for versioning, the repo’s git history for release commits/tags, and documentation/release notes for publishing. It is the bridge between package changes and the external release process.
