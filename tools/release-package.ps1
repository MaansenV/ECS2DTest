[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Version,

    [string]$Remote = 'origin',

    [switch]$Push
)

$ErrorActionPreference = 'Stop'

function Fail([string]$Message)
{
    throw $Message
}

function Invoke-Git([string[]]$Arguments)
{
    & git @Arguments
    if ($LASTEXITCODE -ne 0)
    {
        Fail("git $($Arguments -join ' ') failed with exit code $LASTEXITCODE.")
    }
}

function Get-GitOutput([string[]]$Arguments)
{
    $output = & git @Arguments 2>&1
    if ($LASTEXITCODE -ne 0)
    {
        Fail("git $($Arguments -join ' ') failed.`n$output")
    }

    return ($output | Out-String).Trim()
}

function Normalize-PathString([string]$Path)
{
    return [System.IO.Path]::GetFullPath($Path).TrimEnd('\', '/')
}

function Set-JsonStringValue([string]$Path, [string]$PropertyName, [string]$Value)
{
    $content = Get-Content -LiteralPath $Path -Raw
    $pattern = '"' + [Regex]::Escape($PropertyName) + '"\s*:\s*"[^"]*"'
    $replacement = '"' + $PropertyName + '": "' + $Value + '"'
    $updated = [Regex]::Replace($content, $pattern, $replacement, 1)

    if ($updated -eq $content)
    {
        Fail "Failed to update JSON property '$PropertyName' in $Path"
    }

    [System.IO.File]::WriteAllText($Path, $updated, [System.Text.UTF8Encoding]::new($false))
}

function Get-StagedPaths()
{
    $output = & git diff --cached --name-only 2>&1
    if ($LASTEXITCODE -ne 0)
    {
        Fail "Failed to inspect staged files.`n$output"
    }

    return @($output | Where-Object { $_ -and $_.Trim() })
}

function Assert-NoUnexpectedStagedPaths([string[]]$AllowedPrefixes)
{
    $stagedPaths = Get-StagedPaths
    $unexpected = @()

    foreach ($path in $stagedPaths)
    {
        $isAllowed = $false
        foreach ($prefix in $AllowedPrefixes)
        {
            if ($path.StartsWith($prefix, [System.StringComparison]::OrdinalIgnoreCase))
            {
                $isAllowed = $true
                break
            }
        }

        if (-not $isAllowed)
        {
            $unexpected += $path
        }
    }

    if ($unexpected.Count -gt 0)
    {
        Fail "Unexpected staged paths detected. Unstage them before running this script:`n$($unexpected -join [Environment]::NewLine)"
    }
}

function Assert-BranchAllowed()
{
    $branch = Get-GitOutput @('branch', '--show-current')
    if ([string]::IsNullOrWhiteSpace($branch))
    {
        Fail 'Releases must not run from detached HEAD.'
    }

    if ($branch -notin @('master', 'main'))
    {
        Fail "Release workflow only allows main/master. Current branch: $branch"
    }
}

if (-not ($Version -match '^\d+\.\d+\.\d+$'))
{
    Fail 'Version must use x.y.z format, for example 1.0.6.'
}

$repoRoot = Get-GitOutput @('rev-parse', '--show-toplevel')
$currentLocation = (Get-Location).Path
if ((Normalize-PathString $repoRoot) -ine (Normalize-PathString $currentLocation))
{
    Fail "Run this script from the ECS2DTest repo root.`nExpected: $repoRoot`nCurrent:  $currentLocation"
}

$packageRoot = Join-Path $repoRoot 'Packages/com.ecs2d.renderer'
$packageJsonPath = Join-Path $packageRoot 'package.json'
if (-not (Test-Path -LiteralPath $packageJsonPath))
{
    Fail "Package file not found: $packageJsonPath"
}

$tagName = "v$Version"

Assert-BranchAllowed
Assert-NoUnexpectedStagedPaths @('Packages/com.ecs2d.renderer/')

$localTag = & git tag --list $tagName
if ($LASTEXITCODE -ne 0)
{
    Fail 'Failed to query local tags.'
}
if (($localTag | Out-String).Trim())
{
    Fail "Tag $tagName already exists locally."
}

$remoteTag = & git ls-remote --tags $Remote "refs/tags/$tagName" 2>&1
if ($LASTEXITCODE -ne 0)
{
    Fail "Failed to query remote tag $tagName on $Remote.`n$remoteTag"
}
if (($remoteTag | Out-String).Trim())
{
    Fail "Tag $tagName already exists on $Remote."
}

Set-JsonStringValue -Path $packageJsonPath -PropertyName 'version' -Value $Version

Invoke-Git @('add', '--', 'Packages/com.ecs2d.renderer')
Assert-NoUnexpectedStagedPaths @('Packages/com.ecs2d.renderer/')

$stagedDiff = Get-GitOutput @('diff', '--cached', '--name-only', '--', 'Packages/com.ecs2d.renderer')
if (-not $stagedDiff)
{
    Fail 'No staged package changes found after version bump. Refusing to create an empty release.'
}

$commitMessage = "Release com.ecs2d.renderer $Version"
Invoke-Git @('commit', '-m', $commitMessage)
Invoke-Git @('tag', '-a', $tagName, '-m', $commitMessage)

if ($Push)
{
    Invoke-Git @('push', $Remote, 'HEAD', $tagName)
}

Write-Host "Release prepared successfully."
Write-Host "Version: $Version"
Write-Host "Tag: $tagName"
Write-Host "Push: $Push"
