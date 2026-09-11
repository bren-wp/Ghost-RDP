param(
    [Parameter(Mandatory = $true)]
    [string]$AppDirectory,

    [Parameter(Mandatory = $true)]
    [string]$HostDirectory
)

$ErrorActionPreference = 'Stop'

$expected = @(
    (Join-Path $AppDirectory 'GhostRdp.App.exe'),
    (Join-Path $HostDirectory 'GhostRdp.Host.exe')
)

foreach ($path in $expected) {
    if (-not (Test-Path $path)) {
        throw "Package validation failed. Missing executable: $path"
    }
}

$unexpectedRdpFiles = Get-ChildItem -Path $AppDirectory,$HostDirectory -Recurse -File -Filter *.rdp -ErrorAction SilentlyContinue
if ($unexpectedRdpFiles) {
    throw 'Package validation failed. Static .rdp files must not be shipped.'
}

Write-Host 'Package validation passed.' -ForegroundColor Green
