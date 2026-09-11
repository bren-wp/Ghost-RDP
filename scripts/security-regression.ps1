$ErrorActionPreference = 'Stop'

$sourceFiles = Get-ChildItem -Path "$PSScriptRoot/../src" -Recurse -File -Include *.cs,*.xaml,*.json,*.config,*.props,*.csproj
$forbiddenPatterns = @(
    '(?i)/p:\s*',
    '(?i)--password(?:=|\s)',
    '(?i)password\s*=',
    '(?i)password\s+\d+:[a-z]:',
    '(?i)UseShellExecute\s*=\s*true',
    '(?i)ProcessStartInfo\s*\(\s*["''](?:cmd|powershell|pwsh)(?:\.exe)?["'']'
)

$violations = foreach ($pattern in $forbiddenPatterns) {
    $sourceFiles | Select-String -Pattern $pattern
}

if ($violations) {
    Write-Host 'Security regression check failed:' -ForegroundColor Red
    $violations | ForEach-Object { Write-Host "$($_.Path):$($_.LineNumber): $($_.Line.Trim())" }
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
