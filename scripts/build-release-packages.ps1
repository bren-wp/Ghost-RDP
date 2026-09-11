param(
    [string]$OutputDirectory = 'artifacts/release'
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
$appPublish = Join-Path $workRoot 'app'
$hostPublish = Join-Path $workRoot 'host'
$portablePayload = Join-Path $workRoot 'portable'
$appProject = Join-Path $repositoryRoot 'src/GhostRdp.App/GhostRdp.App.csproj'
$hostProject = Join-Path $repositoryRoot 'src/GhostRdp.Host/GhostRdp.Host.csproj'
$installerScript = Join-Path $repositoryRoot 'packaging/windows/GhostRDP.iss'
$licensePath = Join-Path $repositoryRoot 'LICENSE'

[xml]$appProjectXml = Get-Content -Path $appProject -Raw
$version = [string]($appProjectXml.Project.PropertyGroup.Version | Select-Object -First 1)
if ([string]::IsNullOrWhiteSpace($version)) {
    throw 'Could not determine Ghost RDP version from the App project.'
}

Remove-Item -Path $workRoot,$outputRoot -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path $appPublish,$hostPublish,$portablePayload,$outputRoot | Out-Null

$commonPublishArguments = @(
    '-c', 'Release',
    '-r', 'win-x64',
    '--self-contained', 'true',
    '-p:PublishSingleFile=true',
    '-p:IncludeNativeLibrariesForSelfExtract=true',
    '-p:PublishTrimmed=false',
    '-p:DebugType=None',
    '-p:DebugSymbols=false'
)

Write-Host "Publishing Ghost RDP $version self-contained portable executables..."
& dotnet publish $appProject @commonPublishArguments '-o' $appPublish
if ($LASTEXITCODE -ne 0) {
    throw 'Ghost RDP App self-contained publish failed.'
}

& dotnet publish $hostProject @commonPublishArguments '-o' $hostPublish
if ($LASTEXITCODE -ne 0) {
    throw 'Ghost RDP Host self-contained publish failed.'
}

$appExecutable = Join-Path $appPublish 'GhostRdp.App.exe'
$hostExecutable = Join-Path $hostPublish 'GhostRdp.Host.exe'
if (-not (Test-Path $appExecutable) -or -not (Test-Path $hostExecutable)) {
    throw 'Self-contained publish did not produce the expected executables.'
}

$portableApp = Join-Path $outputRoot 'GhostRDP-Portable-x64.exe'
$portableHost = Join-Path $outputRoot 'GhostRDP-Host-x64.exe'
$portableLicense = Join-Path $outputRoot 'LICENSE.txt'
Copy-Item $appExecutable $portableApp
Copy-Item $hostExecutable $portableHost
Copy-Item $licensePath $portableLicense

Copy-Item $portableApp (Join-Path $portablePayload 'GhostRDP-Portable-x64.exe')
Copy-Item $portableHost (Join-Path $portablePayload 'GhostRDP-Host-x64.exe')
Copy-Item $portableLicense (Join-Path $portablePayload 'LICENSE.txt')

$portableZip = Join-Path $outputRoot 'GhostRDP-Portable-x64.zip'
Compress-Archive -Path (Join-Path $portablePayload '*') -DestinationPath $portableZip -CompressionLevel Optimal

$innoCandidates = @(
    (Get-Command ISCC.exe -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Source -ErrorAction SilentlyContinue),
    (Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6/ISCC.exe'),
    (Join-Path $env:ProgramFiles 'Inno Setup 6/ISCC.exe')
) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) -and (Test-Path $_) } | Select-Object -Unique

$innoCompiler = $innoCandidates | Select-Object -First 1
if (-not $innoCompiler) {
    throw 'Inno Setup 6 compiler (ISCC.exe) was not found. Install Inno Setup 6 before building the installer.'
}

Write-Host "Building installer with $innoCompiler"
& $innoCompiler "/DAppVersion=$version" "/DSourceDir=$outputRoot" "/O$outputRoot" $installerScript
if ($LASTEXITCODE -ne 0) {
    throw 'Inno Setup compilation failed.'
}

$setupExecutable = Join-Path $outputRoot 'GhostRDP-Setup-x64.exe'
if (-not (Test-Path $setupExecutable)) {
    throw 'Installer build did not produce GhostRDP-Setup-x64.exe.'
}

$hashTargets = @($portableApp, $portableHost, $portableZip, $setupExecutable)
$hashLines = foreach ($path in $hashTargets) {
    $hash = Get-FileHash -Path $path -Algorithm SHA256
    "{0}  {1}" -f $hash.Hash.ToLowerInvariant(), (Split-Path $path -Leaf)
}
$hashLines | Set-Content -Path (Join-Path $outputRoot 'SHA256SUMS.txt') -Encoding utf8

Remove-Item -Path $workRoot -Recurse -Force -ErrorAction SilentlyContinue
Write-Host "Release packages created in $outputRoot" -ForegroundColor Green
