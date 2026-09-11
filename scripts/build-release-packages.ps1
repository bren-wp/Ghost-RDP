param(
    [string]$OutputDirectory = 'artifacts/release',
    [ValidateSet('all', 'x86', 'x64', 'arm64')]
    [string]$Architecture = 'all'
)

$ErrorActionPreference = 'Stop'

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$outputRoot = if ([System.IO.Path]::IsPathRooted($OutputDirectory)) {
    [System.IO.Path]::GetFullPath($OutputDirectory)
}
else {
    [System.IO.Path]::GetFullPath((Join-Path $repositoryRoot $OutputDirectory))
}
$workRoot = Join-Path $repositoryRoot 'artifacts/package-work'
$appProject = Join-Path $repositoryRoot 'src/GhostRdp.App/GhostRdp.App.csproj'
$hostProject = Join-Path $repositoryRoot 'src/GhostRdp.Host/GhostRdp.Host.csproj'
$setupProject = Join-Path $repositoryRoot 'src/GhostRdp.Setup/GhostRdp.Setup.csproj'
$licensePath = Join-Path $repositoryRoot 'LICENSE'

function Get-ProjectVersion([string]$ProjectPath) {
    [xml]$projectXml = Get-Content -Path $ProjectPath -Raw
    $value = [string]($projectXml.Project.PropertyGroup.Version | Select-Object -First 1)
    if ([string]::IsNullOrWhiteSpace($value)) {
        throw "Could not determine version from $ProjectPath"
    }

    return $value
}

$appVersion = Get-ProjectVersion $appProject
$hostVersion = Get-ProjectVersion $hostProject
$setupVersion = Get-ProjectVersion $setupProject
if ($appVersion -ne $hostVersion -or $appVersion -ne $setupVersion) {
    throw "Ghost RDP App, Host, and Setup versions must match. App=$appVersion Host=$hostVersion Setup=$setupVersion"
}
$version = $appVersion

$architectures = if ($Architecture -eq 'all') { @('x86', 'x64', 'arm64') } else { @($Architecture) }

Remove-Item -Path $workRoot -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -Path $outputRoot -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path $workRoot,$outputRoot | Out-Null
Copy-Item $licensePath (Join-Path $outputRoot 'LICENSE.txt')

$commonPublishArguments = @(
    '-c', 'Release',
    '--self-contained', 'true',
    '-p:PublishSingleFile=true',
    '-p:IncludeNativeLibrariesForSelfExtract=true',
    '-p:PublishTrimmed=false',
    '-p:DebugType=None',
    '-p:DebugSymbols=false'
)

foreach ($arch in $architectures) {
    $rid = "win-$arch"
    $archWork = Join-Path $workRoot $arch
    $appPublish = Join-Path $archWork 'app'
    $hostPublish = Join-Path $archWork 'host'
    $setupPublish = Join-Path $archWork 'setup'
    $payloadDirectory = Join-Path $archWork 'payload'
    New-Item -ItemType Directory -Force -Path $appPublish,$hostPublish,$setupPublish,$payloadDirectory | Out-Null

    Write-Host "Publishing Ghost RDP $version for $rid..."
    & dotnet publish $appProject @commonPublishArguments '-r' $rid '-o' $appPublish
    if ($LASTEXITCODE -ne 0) { throw "Ghost RDP App publish failed for $rid." }

    & dotnet publish $hostProject @commonPublishArguments '-r' $rid '-o' $hostPublish
    if ($LASTEXITCODE -ne 0) { throw "Ghost RDP Host publish failed for $rid." }

    $appExecutable = Join-Path $appPublish 'GhostRdp.App.exe'
    $hostExecutable = Join-Path $hostPublish 'GhostRdp.Host.exe'
    if (-not (Test-Path $appExecutable) -or -not (Test-Path $hostExecutable)) {
        throw "Self-contained publish did not produce expected $rid executables."
    }

    $portableApp = Join-Path $outputRoot "GhostRDP-Portable-$arch.exe"
    $portableHost = Join-Path $outputRoot "GhostRDP-Host-$arch.exe"
    Copy-Item $appExecutable $portableApp
    Copy-Item $hostExecutable $portableHost

    Copy-Item $appExecutable (Join-Path $payloadDirectory 'GhostRDP.exe')
    Copy-Item $hostExecutable (Join-Path $payloadDirectory 'GhostRDP-Host.exe')
    Copy-Item $licensePath (Join-Path $payloadDirectory 'LICENSE.txt')

    $payloadZip = Join-Path $archWork "GhostRDP-Payload-$arch.zip"
    Compress-Archive -Path (Join-Path $payloadDirectory '*') -DestinationPath $payloadZip -CompressionLevel Optimal
    Copy-Item $payloadZip (Join-Path $outputRoot "GhostRDP-Portable-$arch.zip")

    $setupArguments = @(
        '-c', 'Release',
        '-r', $rid,
        '--self-contained', 'true',
        '-p:PublishSingleFile=true',
        '-p:IncludeNativeLibrariesForSelfExtract=true',
        '-p:PublishTrimmed=false',
        '-p:DebugType=None',
        '-p:DebugSymbols=false',
        "-p:PayloadZip=$payloadZip",
        '-o', $setupPublish
    )
    & dotnet publish $setupProject @setupArguments
    if ($LASTEXITCODE -ne 0) { throw "Ghost RDP Setup publish failed for $rid." }

    $setupExecutable = Join-Path $setupPublish 'GhostRdp.Setup.exe'
    if (-not (Test-Path $setupExecutable)) {
        throw "Setup publish did not produce the expected executable for $rid."
    }

    Copy-Item $setupExecutable (Join-Path $outputRoot "GhostRDP-Setup-$arch.exe")

    if ($arch -eq 'x86') {
        Copy-Item $portableApp (Join-Path $outputRoot 'portable.exe')
        Copy-Item $setupExecutable (Join-Path $outputRoot 'setup.exe')
    }
}

$deliverables = Get-ChildItem -Path $outputRoot -File | Where-Object { $_.Extension -in @('.exe', '.zip') } | Sort-Object Name
$hashLines = foreach ($item in $deliverables) {
    $hash = Get-FileHash -Path $item.FullName -Algorithm SHA256
    "{0}  {1}" -f $hash.Hash.ToLowerInvariant(), $item.Name
}
$hashLines | Set-Content -Path (Join-Path $outputRoot 'SHA256SUMS.txt') -Encoding utf8

$manifest = [ordered]@{
    product = 'Ghost RDP'
    version = $version
    architectures = $architectures
    canonicalSetup = if ($architectures -contains 'x86') { 'setup.exe' } else { $null }
    canonicalPortable = if ($architectures -contains 'x86') { 'portable.exe' } else { $null }
    uninstall = 'Windows Installed Apps uses the installed GhostRDP-Setup.exe; no separate uninstall.exe is shipped.'
}
$manifest | ConvertTo-Json -Depth 4 | Set-Content -Path (Join-Path $outputRoot 'RELEASE-MANIFEST.json') -Encoding utf8

Remove-Item -Path $workRoot -Recurse -Force -ErrorAction SilentlyContinue
Write-Host "Ghost RDP $version release packages created in $outputRoot" -ForegroundColor Green
