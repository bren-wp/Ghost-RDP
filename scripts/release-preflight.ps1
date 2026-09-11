param(
    [switch]$RequireReleaseMetadata,
    [switch]$RequireReleaseBranch
)

$ErrorActionPreference = 'Stop'

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path

function Get-ProjectVersion([string]$ProjectPath) {
    [xml]$projectXml = Get-Content -Path $ProjectPath -Raw
    $value = [string]($projectXml.Project.PropertyGroup.Version | Select-Object -First 1)
    if ([string]::IsNullOrWhiteSpace($value)) {
        throw "Could not determine version from $ProjectPath"
    }

    return $value.Trim()
}

$appProject = Join-Path $repositoryRoot 'src/GhostRdp.App/GhostRdp.App.csproj'
$hostProject = Join-Path $repositoryRoot 'src/GhostRdp.Host/GhostRdp.Host.csproj'
$setupProject = Join-Path $repositoryRoot 'src/GhostRdp.Setup/GhostRdp.Setup.csproj'

$appVersion = Get-ProjectVersion $appProject
$hostVersion = Get-ProjectVersion $hostProject
$setupVersion = Get-ProjectVersion $setupProject

if ($appVersion -ne $hostVersion -or $appVersion -ne $setupVersion) {
    throw "Ghost RDP App, Host, and Setup versions must match. App=$appVersion Host=$hostVersion Setup=$setupVersion"
}

$version = $appVersion

if ($RequireReleaseBranch) {
    $actualBranch = [string]$env:GITHUB_REF_NAME
    if ([string]::IsNullOrWhiteSpace($actualBranch)) {
        throw 'GITHUB_REF_NAME is required when -RequireReleaseBranch is used.'
    }

    $expectedBranch = "release/v$version"
    if ($actualBranch -ne $expectedBranch) {
        throw "Release branch must be $expectedBranch; actual branch is $actualBranch"
    }
}

if ($RequireReleaseMetadata) {
    $escapedVersion = [regex]::Escape($version)
    $changelogPath = Join-Path $repositoryRoot 'CHANGELOG.md'
    $releaseNotesPath = Join-Path $repositoryRoot 'docs/RELEASE-NOTES.md'
    $readmePath = Join-Path $repositoryRoot 'README.md'

    $changelog = Get-Content -Path $changelogPath -Raw
    if ($changelog -notmatch "(?m)^## $escapedVersion - \d{4}-\d{2}-\d{2}\s*$") {
        throw "CHANGELOG.md must contain a dated release heading for version $version."
    }

    if ($changelog -match "(?m)^## $escapedVersion - development\s*$") {
        throw "CHANGELOG.md still marks version $version as development."
    }

    $releaseNotes = Get-Content -Path $releaseNotesPath -Raw
    if ($releaseNotes -notmatch "(?m)^# Ghost RDP $escapedVersion\s*$") {
        throw "docs/RELEASE-NOTES.md must start with '# Ghost RDP $version'."
    }

    if ($releaseNotes -match '(?i)development notes|not yet the published production release') {
        throw 'docs/RELEASE-NOTES.md still contains pre-release-only wording.'
    }

    $readme = Get-Content -Path $readmePath -Raw
    if ($readme -notmatch [regex]::Escape($version)) {
        throw "README.md must mention release version $version."
    }
}

Write-Host "Ghost RDP release preflight passed for version $version."
