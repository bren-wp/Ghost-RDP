param(
    [Parameter(Mandatory = $true)]
    [string]$InputRoot,
    [Parameter(Mandatory = $true)]
    [string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
$inputPath = [System.IO.Path]::GetFullPath($InputRoot)
$outputPath = [System.IO.Path]::GetFullPath($OutputDirectory)
Remove-Item -Path $outputPath -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path $outputPath | Out-Null

$componentManifests = Get-ChildItem -Path $inputPath -Recurse -File -Filter 'RELEASE-MANIFEST.json'
if ($componentManifests.Count -ne 3) {
    throw "Expected three architecture component manifests, found $($componentManifests.Count)."
}

$versions = @()
$architectures = @()
foreach ($manifestFile in $componentManifests) {
    $manifest = Get-Content $manifestFile.FullName -Raw | ConvertFrom-Json
    $versions += [string]$manifest.version
    $architectures += @($manifest.architectures)

    $componentDirectory = $manifestFile.Directory.FullName
    Get-ChildItem -Path $componentDirectory -File | Where-Object {
        $_.Extension -in @('.exe', '.zip') -or $_.Name -eq 'LICENSE.txt'
    } | ForEach-Object {
        $destination = Join-Path $outputPath $_.Name
        if (Test-Path $destination) {
            $existingHash = (Get-FileHash $destination -Algorithm SHA256).Hash
            $incomingHash = (Get-FileHash $_.FullName -Algorithm SHA256).Hash
            if ($existingHash -ne $incomingHash) {
                throw "Conflicting release artifact: $($_.Name)"
            }
        }
        else {
            Copy-Item $_.FullName $destination
        }
    }
}

$uniqueVersions = $versions | Sort-Object -Unique
if ($uniqueVersions.Count -ne 1) {
    throw "Architecture packages have mismatched versions: $($uniqueVersions -join ', ')"
}
$version = $uniqueVersions[0]
$uniqueArchitectures = $architectures | Sort-Object -Unique
foreach ($required in @('x86', 'x64', 'arm64')) {
    if ($uniqueArchitectures -notcontains $required) {
        throw "Release assembly is missing architecture: $required"
    }
}

$deliverables = Get-ChildItem -Path $outputPath -File | Where-Object { $_.Extension -in @('.exe', '.zip') } | Sort-Object Name
$hashLines = foreach ($item in $deliverables) {
    $hash = Get-FileHash -Path $item.FullName -Algorithm SHA256
    "{0}  {1}" -f $hash.Hash.ToLowerInvariant(), $item.Name
}
$hashLines | Set-Content -Path (Join-Path $outputPath 'SHA256SUMS.txt') -Encoding utf8

$manifest = [ordered]@{
    product = 'Ghost RDP'
    version = $version
    architectures = @('x86', 'x64', 'arm64')
    canonicalSetup = 'setup.exe'
    canonicalPortable = 'portable.exe'
    canonicalArchitecture = 'x86'
    uninstall = 'Windows Installed Apps uses the installed GhostRDP-Setup.exe; no separate uninstall.exe is shipped.'
}
$manifest | ConvertTo-Json -Depth 4 | Set-Content -Path (Join-Path $outputPath 'RELEASE-MANIFEST.json') -Encoding utf8

Write-Host "Assembled Ghost RDP $version multi-architecture release." -ForegroundColor Green
