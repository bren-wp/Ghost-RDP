param(
    [Parameter(Mandatory = $true)]
    [string]$SetupPath
)

$ErrorActionPreference = 'Stop'
$setup = [System.IO.Path]::GetFullPath($SetupPath)
if (-not (Test-Path $setup -PathType Leaf)) {
    throw "Installer smoke test could not find setup executable: $setup"
}

$testRoot = Join-Path $env:TEMP ("GhostRDP-Installer-Test-" + [Guid]::NewGuid().ToString('N'))
$installDirectory = Join-Path $testRoot 'install'
$installLog = Join-Path $testRoot 'install.log'
$uninstallLog = Join-Path $testRoot 'uninstall.log'
New-Item -ItemType Directory -Force -Path $testRoot | Out-Null

try {
    $installArguments = @(
        '/VERYSILENT',
        '/SUPPRESSMSGBOXES',
        '/NORESTART',
        "/DIR=$installDirectory",
        "/LOG=$installLog"
    )
    $installProcess = Start-Process -FilePath $setup -ArgumentList $installArguments -Wait -PassThru
    if ($installProcess.ExitCode -ne 0) {
        throw "Installer smoke test failed with exit code $($installProcess.ExitCode)."
    }

    foreach ($name in @('GhostRDP.exe', 'GhostRDP-Host.exe', 'LICENSE.txt')) {
        if (-not (Test-Path (Join-Path $installDirectory $name) -PathType Leaf)) {
            throw "Installer smoke test failed. Installed file is missing: $name"
        }
    }

    $uninstaller = Get-ChildItem -Path $installDirectory -File -Filter 'unins*.exe' | Select-Object -First 1
    if (-not $uninstaller) {
        throw 'Installer smoke test failed. Inno Setup uninstaller was not registered in the install directory.'
    }

    $uninstallArguments = @('/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART', "/LOG=$uninstallLog")
    $uninstallProcess = Start-Process -FilePath $uninstaller.FullName -ArgumentList $uninstallArguments -Wait -PassThru
    if ($uninstallProcess.ExitCode -ne 0) {
        throw "Uninstaller smoke test failed with exit code $($uninstallProcess.ExitCode)."
    }

    Start-Sleep -Milliseconds 500
    if (Test-Path (Join-Path $installDirectory 'GhostRDP.exe')) {
        throw 'Uninstaller smoke test failed. GhostRDP.exe remained after uninstall.'
    }

    if (Test-Path (Join-Path $installDirectory 'GhostRDP-Host.exe')) {
        throw 'Uninstaller smoke test failed. GhostRDP-Host.exe remained after uninstall.'
    }

    Write-Host 'Installer install/uninstall smoke test passed.' -ForegroundColor Green
}
finally {
    Remove-Item -Path $testRoot -Recurse -Force -ErrorAction SilentlyContinue
}
