$ErrorActionPreference = 'Stop'

function Get-ProjectSourceFiles {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [Parameter(Mandatory = $true)]
        [string[]]$Include
    )

    Get-ChildItem -Path $Path -Recurse -File -Include $Include |
        Where-Object {
            $_.FullName -notmatch '[\\/](?:bin|obj)[\\/]'
        }
}

$sourceFiles = Get-ProjectSourceFiles -Path "$PSScriptRoot/../src" -Include @('*.cs', '*.xaml', '*.json', '*.config', '*.props', '*.targets', '*.csproj')
$forbiddenPatterns = @(
    '(?i)/p:\s*',
    '(?i)--password(?:=|\s)',
    '(?i)password\s*=',
    '(?i)password\s+\d+:[a-z]:',
    '(?i)gatewayaccesstoken\s*:',
    '(?i)UseShellExecute\s*=\s*true',
    '(?i)ProcessStartInfo\s*\(\s*["''](?:cmd|powershell|pwsh)(?:\.exe)?["'']'
)

$violations = foreach ($pattern in $forbiddenPatterns) {
    $sourceFiles | Select-String -Pattern $pattern
}

# Runtime projects intentionally use only the repository's .NET/WPF/Windows stack.
# Tests may keep their existing Microsoft test-tooling packages, but src/ must not
# acquire third-party PackageReference or file HintPath dependencies.
$runtimeProjectFiles = Get-ProjectSourceFiles -Path "$PSScriptRoot/../src" -Include @('*.csproj', '*.props', '*.targets')
$forbiddenRuntimeDependencyPatterns = @(
    '(?i)<PackageReference\b',
    '(?i)<Reference\b[^>]*>\s*<HintPath\b',
    '(?i)<Reference\b[^>]*/>\s*<!--\s*external'
)

$runtimeDependencyViolations = foreach ($pattern in $forbiddenRuntimeDependencyPatterns) {
    $runtimeProjectFiles | Select-String -Pattern $pattern
}

$hostSourceFiles = Get-ProjectSourceFiles -Path "$PSScriptRoot/../src/GhostRdp.Host" -Include @('*.cs')
$forbiddenHostMutationPatterns = @(
    '(?i)\.SetValue\s*\(',
    '(?i)\.DeleteValue\s*\(',
    '(?i)\.CreateSubKey\s*\(',
    '(?i)StartService(?:W|A)?\s*\(',
    '(?i)ControlService\s*\(',
    '(?i)NetFwRule',
    '(?i)netsh\b',
    '(?i)Set-NetFirewall',
    '(?i)Enable-NetFirewall',
    '(?i)Disable-NetFirewall'
)

$hostViolations = foreach ($pattern in $forbiddenHostMutationPatterns) {
    $hostSourceFiles | Select-String -Pattern $pattern
}

$packagingFiles = @(
    Get-ChildItem -Path "$PSScriptRoot/../packaging" -Recurse -File -Include *.iss,*.ps1 -ErrorAction SilentlyContinue
    Get-Item -Path @(
        "$PSScriptRoot/build-release-packages.ps1",
        "$PSScriptRoot/test-installer.ps1",
        "$PSScriptRoot/validate-release-package.ps1"
    ) -ErrorAction SilentlyContinue
)
$forbiddenPackagingPatterns = @(
    '(?i)\bnetsh\b',
    '(?i)Set-NetFirewall',
    '(?i)Enable-NetFirewall',
    '(?i)Disable-NetFirewall',
    '(?i)\bsc(?:\.exe)?\s+(?:create|config|start)\b',
    '(?i)\bNew-Service\b',
    '(?im)^\s*PrivilegesRequired\s*=\s*admin\s*$'
)

$packagingViolations = foreach ($pattern in $forbiddenPackagingPatterns) {
    $packagingFiles | Select-String -Pattern $pattern
}

if ($violations -or $runtimeDependencyViolations -or $hostViolations -or $packagingViolations) {
    Write-Host 'Security/dependency regression check failed:' -ForegroundColor Red
    @($violations) + @($runtimeDependencyViolations) + @($hostViolations) + @($packagingViolations) | ForEach-Object {
        Write-Host "$($_.Path):$($_.LineNumber): $($_.Line.Trim())"
    }
    exit 1
}

$requiredDocs = @(
    'README.md',
    'DEPENDENCIES.md',
    'ARCHITECTURE.md',
    'SECURITY.md',
    'PRIVACY.md',
    'ACCESSIBILITY.md',
    'WINDOWS.md',
    'HOST.md',
    'REMOTE-ACCESS.md',
    'PACKAGING.md',
    'RELEASE.md',
    'RELEASE-NOTES.md',
    'ROADMAP.md',
    'BUILD.md',
    'UI-UX.md'
)

foreach ($document in $requiredDocs) {
    $path = Join-Path "$PSScriptRoot/../docs" $document
    if (-not (Test-Path $path)) {
        throw "Required document missing: $document"
    }
}

Write-Host 'Security and runtime dependency regression checks passed.' -ForegroundColor Green
