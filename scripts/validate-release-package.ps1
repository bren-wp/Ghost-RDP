param(
    [Parameter(Mandatory = $true)]
    [string]$ReleaseDirectory
)

$ErrorActionPreference = 'Stop'
$releaseRoot = [System.IO.Path]::GetFullPath($ReleaseDirectory)

$expectedFiles = @(
    'GhostRDP-Portable-x64.exe',
    'GhostRDP-Host-x64.exe',
    'GhostRDP-Portable-x64.zip',
    'GhostRDP-Setup-x64.exe',
    'LICENSE.txt',
    'SHA256SUMS.txt'
)

foreach ($name in $expectedFiles) {
    $path = Join-Path $releaseRoot $name
    if (-not (Test-Path $path -PathType Leaf)) {
        throw "Release package validation failed. Missing file: $name"
    }
}

$binaryNames = @('GhostRDP-Portable-x64.exe', 'GhostRDP-Host-x64.exe', 'GhostRDP-Portable-x64.zip', 'GhostRDP-Setup-x64.exe')
foreach ($name in $binaryNames) {
    $item = Get-Item (Join-Path $releaseRoot $name)
    if ($item.Length -lt 1MB) {
        throw "Release package validation failed. Artifact is unexpectedly small: $name ($($item.Length) bytes)"
    }
}

$unexpectedRdpFiles = Get-ChildItem -Path $releaseRoot -Recurse -File -Filter *.rdp -ErrorAction SilentlyContinue
if ($unexpectedRdpFiles) {
    throw 'Release package validation failed. Static .rdp files must not be shipped.'
}

$hashFile = Join-Path $releaseRoot 'SHA256SUMS.txt'
$hashLines = Get-Content $hashFile
foreach ($name in $binaryNames) {
    $expectedLine = $hashLines | Where-Object { $_ -match "\s+$([regex]::Escape($name))$" } | Select-Object -First 1
    if (-not $expectedLine) {
        throw "Release package validation failed. SHA256SUMS.txt is missing $name."
    }

    $expectedHash = ($expectedLine -split '\s+')[0]
    $actualHash = (Get-FileHash -Path (Join-Path $releaseRoot $name) -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($expectedHash.ToLowerInvariant() -ne $actualHash) {
        throw "Release package validation failed. SHA-256 mismatch for $name."
    }
}

Add-Type -AssemblyName System.IO.Compression.FileSystem
$zipPath = Join-Path $releaseRoot 'GhostRDP-Portable-x64.zip'
$zip = [System.IO.Compression.ZipFile]::OpenRead($zipPath)
try {
    $zipNames = $zip.Entries | ForEach-Object { $_.FullName }
    foreach ($requiredName in @('GhostRDP-Portable-x64.exe', 'GhostRDP-Host-x64.exe', 'LICENSE.txt')) {
        if ($zipNames -notcontains $requiredName) {
            throw "Portable ZIP validation failed. Missing entry: $requiredName"
        }
    }
}
finally {
    $zip.Dispose()
}

Write-Host 'Release package validation passed.' -ForegroundColor Green
