$ErrorActionPreference = 'Stop'

$sourceFiles = Get-ChildItem -Path "$PSScriptRoot/../src" -Recurse -File -Include *.cs,*.xaml,*.json,*.config,*.props,*.csproj
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

$hostSourceFiles = Get-ChildItem -Path "$PSScriptRoot/../src/GhostRdp.Host" -Recurse -File -Include *.cs
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

if ($violations -or $hostViolations) {
    Write-Host 'Security regression check failed:' -ForegroundColor Red
    @($violations) + @($hostViolations) | ForEach-Object {
        Write-Host "$($_.Path):$($_.LineNumber): $($_.Line.Trim())"
    }
    exit 1
}

$requiredDocs = @(
    'ARCHITECTURE.md',
    'SECURITY.md',
    'PRIVACY.md',
    'WINDOWS.md',
    'HOST.md',
    'REMOTE-ACCESS.md',
    'ROADMAP.md',
    'BUILD.md'
)

foreach ($document in $requiredDocs) {
    $path = Join-Path "$PSScriptRoot/../docs" $document
    if (-not (Test-Path $path)) {
        throw "Required document missing: $document"
    }
}

Write-Host 'Security regression checks passed.' -ForegroundColor Green
