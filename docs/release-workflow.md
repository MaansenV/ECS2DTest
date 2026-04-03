# ECS2D Renderer Release Workflow

This repo owns the release workflow for `com.ecs2d.renderer`.

## Goal

Create a new package release without manual file editing, manual version bumping, or manual tag handling.

## Agent workflow

From the repo root, run:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\release-package.ps1 -Version 1.0.6 -Push
```

If the agent should also push the commit and tag to GitHub, keep `-Push`.
If it should prepare the release locally first, omit `-Push`.

## What the script does

1. Validates that the version looks like `x.y.z`
2. Validates that the git tag `v<version>` does not already exist
3. Updates `Packages/com.ecs2d.renderer/package.json`
4. Stages the package directory only: `Packages/com.ecs2d.renderer`
5. Creates a release commit
6. Creates an annotated tag `v<version>`
7. Optionally pushes `HEAD` and the tag together

## Preconditions

- Run from the `ECS2DTest` repo root
- Close Unity before running the script
- The package changes that belong in the release must already be present under `Packages/com.ecs2d.renderer`
- Do not rely on untracked files outside the package folder; they are intentionally ignored by the script
- The staged index must not contain unrelated files; the script fails if it finds them
- Releases are only allowed from `main` or `master`

## Safety behavior

- The script fails if the target tag already exists locally or on `origin`
- The script only stages the package folder, so scene/layout/test noise outside the package is not pulled into the release commit
- The script fails if the package folder has no staged diff after the version update, to avoid empty releases
- The script fails if unrelated staged paths already exist, so agents do not accidentally commit someone else's staged work

## Examples

### Local dry run style release

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\release-package.ps1 -Version 1.0.6
```

### Full release to GitHub

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\release-package.ps1 -Version 1.0.6 -Push
```

## Expected result

- `Packages/com.ecs2d.renderer/package.json` contains the new version
- Git history contains a release commit
- Git contains tag `v<version>`
- With `-Push`, GitHub contains both the commit and the tag

## Rollback notes

If the script ran without `-Push`, you can undo the prepared local release with:

```powershell
git tag -d v1.0.6
git reset --soft HEAD~1
```

If a pushed release must be replaced, do not move the old tag. Create a new version instead.

## Consumer update

After releasing here, update consumers like `EndlessFrontline` with their own update script.
