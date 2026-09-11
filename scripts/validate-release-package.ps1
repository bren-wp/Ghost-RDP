param(
    [Parameter(Mandatory = $true)]
    [string]$ReleaseDirectory,
    [ValidateSet('all', 'x86', 'x64', 'arm64')]
    [string]$Architecture = 'all'
)

$ErrorActionPreference = 'Stop'
$releaseRoot = [System.IO.Path]::GetFullPath($ReleaseDirectory)
$architectures = if ($Architecture -eq 'all') { @('x86', 'x64', 'arm64') } else { @($Architecture) }

function Get-PeMachine([string]$Path) {
    $stream = [System.IO.File]::Open($Path, 'Open', 'Read', 'ReadWrite')
    $reader = [System.IO.BinaryReader]::new($stream)
    try {
        if ($reader.ReadUInt16() -ne 0x5A4D) { throw "Not a PE executable: $Path" }
        $stream.Position = 0x3C
        $peOffset = $reader.ReadInt32()
        $stream.Position = $peOffset
        if ($reader.ReadUInt32() -ne 0x00004550) { throw "Invalid PE signature: $Path" }
        return $reader.ReadUInt16()
    }
    finally {
        $reader.Dispose()
        $stream.Dispose()
    }
}

function Assert-PeArchitecture([string]$Path, [string]$Arch) {
    $expected = switch ($Arch) {
        'x86' { 0x014c }
        'x64' { 0x8664 }
        'arm64' { 0xaa64 }
        default { throw "Unsupported architecture: $Arch" }
    }
    $actual = Get-PeMachine $Path
    if ($actual -ne $expected) {
        throw ("Architecture validation failed for {0}. Expected {1}, PE machine 0x{2:x4}." -f (Split-Path $Path -Leaf), $Arch, $actual)
    }
}

$expectedFiles = @('LICENSE.txt', 'SHA256SUMS.txt', 'RELEASE-MANIFEST.json')
foreach ($arch in $architectures) {
    $expectedFiles += @(
        "GhostRDP-Portable-$arch.exe",
        "GhostRDP-Host-$arch.exe",
        "GhostRDP-Portable-$arch.zip",
        "GhostRDP-Setup-$arch.exe"
    )
}
if ($architectures -contains 'x86') {
    $expectedFiles += @('portable.exe', 'setup.exe')
}

foreach ($name in $expectedFiles | Select-Object -Unique) {
    $path = Join-Path $releaseRoot $name
    if (-not (Test-Path $path -PathType Leaf)) {
        throw "Release package validation failed. Missing file: $name"
    }
}

foreach ($arch in $architectures) {
    foreach ($name in @("GhostRDP-Portable-$arch.exe", "GhostRDP-Host-$arch.exe", "GhostRDP-Setup-$arch.exe")) {
        $path = Join-Path $releaseRoot $name
        if ((Get-Item $path).Length -lt 1MB) {
            throw "Release package validation failed. Artifact is unexpectedly small: $name"
        }
        Assert-PeArchitecture $path $arch
    }

    $zipPath = Join-Path $releaseRoot "GhostRDP-Portable-$arch.zip"
    if ((Get-Item $zipPath).Length -lt 1MB) {
        throw "Release package validation failed. Portable ZIP is unexpectedly small: $zipPath"
    }
}

if ($architectures -contains 'x86') {
    Assert-PeArchitecture (Join-Path $releaseRoot 'portable.exe') 'x86'
    Assert-PeArchitecture (Join-Path $releaseRoot 'setup.exe') 'x86'

    $portableAliasHash = (Get-FileHash (Join-Path $releaseRoot 'portable.exe') -Algorithm SHA256).Hash
    $portableX86Hash = (Get-FileHash (Join-Path $releaseRoot 'GhostRDP-Portable-x86.exe') -Algorithm SHA256).Hash
    if ($portableAliasHash -ne $portableX86Hash) { throw 'portable.exe must exactly match the x86 compatibility build.' }

    $setupAliasHash = (Get-FileHash (Join-Path $releaseRoot 'setup.exe') -Algorithm SHA256).Hash
    $setupX86Hash = (Get-FileHash (Join-Path $releaseRoot 'GhostRDP-Setup-x86.exe') -Algorithm SHA256).Hash
    if ($setupAliasHash -ne $setupX86Hash) { throw 'setup.exe must exactly match the x86 compatibility build.' }
}

$unexpectedRdpFiles = Get-ChildItem -Path $releaseRoot -Recurse -File -Filter *.rdp -ErrorAction SilentlyContinue
if ($unexpectedRdpFiles) {
    throw 'Release package validation failed. Static .rdp files must not be shipped.'
}

$unexpectedUninstallers = Get-ChildItem -Path $releaseRoot -Recurse -File -ErrorAction SilentlyContinue |
    Where-Object { $_.Name -match '(?i)^(?:uninstall|unins).*\.exe$' }
if ($unexpectedUninstallers) {
    throw 'Release package validation failed. A separate uninstall executable must not be shipped.'
}

$hashFile = Join-Path $releaseRoot 'SHA256SUMS.txt'
$hashLines = Get-Content $hashFile
$hashTargets = Get-ChildItem -Path $releaseRoot -File | Where-Object { $_.Extension -in @('.exe', '.zip') }
foreach ($item in $hashTargets) {
    $expectedLine = $hashLines | Where-Object { $_ -match "\s+$([regex]::Escape($item.Name))$" } | Select-Object -First 1
    if (-not $expectedLine) {
        throw "Release package validation failed. SHA256SUMS.txt is missing $($item.Name)."
    }

    $expectedHash = ($expectedLine -split '\s+')[0]
    $actualHash = (Get-FileHash -Path $item.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($expectedHash.ToLowerInvariant() -ne $actualHash) {
        throw "Release package validation failed. SHA-256 mismatch for $($item.Name)."
    }
}

Add-Type -AssemblyName System.IO.Compression.FileSystem
foreach ($arch in $architectures) {
    $zipPath = Join-Path $releaseRoot "GhostRDP-Portable-$arch.zip"
    $zip = [System.IO.Compression.ZipFile]::OpenRead($zipPath)
    try {
        $zipNames = $zip.Entries | ForEach-Object { $_.FullName }
        foreach ($requiredName in @('GhostRDP.exe', 'GhostRDP-Host.exe', 'LICENSE.txt')) {
            if ($zipNames -notcontains $requiredName) {
                throw "Portable ZIP validation failed for $arch. Missing entry: $requiredName"
            }
        }
        if ($zipNames | Where-Object { $_ -match '(?i)(?:uninstall|unins).*\.exe$' }) {
            throw "Portable ZIP validation failed for $arch. Uninstaller executable found."
        }
    }
    finally {
        $zip.Dispose()
    }
}

$manifest = Get-Content (Join-Path $releaseRoot 'RELEASE-MANIFEST.json') -Raw | ConvertFrom-Json
if ([string]::IsNullOrWhiteSpace($manifest.version)) {
    throw 'Release manifest does not contain a version.'
}
if ($manifest.uninstall -notmatch 'no separate uninstall\.exe') {
    throw 'Release manifest must state the no-separate-uninstaller contract.'
}

Write-Host "Release package validation passed for: $($architectures -join ', ')" -ForegroundColor Green
