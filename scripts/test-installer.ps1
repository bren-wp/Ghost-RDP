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
$uninstallKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\GhostRDP'
New-Item -ItemType Directory -Force -Path $testRoot | Out-Null

function Invoke-SetupProcess([string]$FilePath, [string[]]$Arguments) {
    $startInfo = [System.Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = $FilePath
    $startInfo.UseShellExecute = $false
    foreach ($argument in $Arguments) {
        [void]$startInfo.ArgumentList.Add($argument)
    }

    $process = [System.Diagnostics.Process]::Start($startInfo)
    if (-not $process) { throw "Could not start $FilePath" }
    try {
        $process.WaitForExit()
        return $process.ExitCode
    }
    finally {
        $process.Dispose()
    }
}

try {
    $installExitCode = Invoke-SetupProcess $setup @('--install', '--quiet', '--path', $installDirectory)
    if ($installExitCode -ne 0) {
        throw "Installer smoke test failed with exit code $installExitCode."
    }

    foreach ($name in @('GhostRDP.exe', 'GhostRDP-Host.exe', 'GhostRDP-Setup.exe', 'LICENSE.txt')) {
        if (-not (Test-Path (Join-Path $installDirectory $name) -PathType Leaf)) {
            throw "Installer smoke test failed. Installed file is missing: $name"
        }
    }

    $unexpectedUninstaller = Get-ChildItem -Path $installDirectory -File |
        Where-Object { $_.Name -match '(?i)^(?:uninstall|unins).*\.exe$' }
    if ($unexpectedUninstaller) {
        throw 'Installer smoke test failed. A separate uninstall executable was installed.'
    }

    if (-not (Test-Path $uninstallKey)) {
        throw 'Installer smoke test failed. Windows uninstall registration is missing.'
    }
    $registration = Get-ItemProperty $uninstallKey
    if ($registration.InstallLocation -ne $installDirectory) {
        throw 'Installer smoke test failed. InstallLocation is incorrect.'
    }
    if ($registration.UninstallString -notmatch 'GhostRDP-Setup\.exe" --uninstall$') {
        throw 'Installer smoke test failed. Windows uninstall must use the installed Setup executable.'
    }

    $installedSetup = Join-Path $installDirectory 'GhostRDP-Setup.exe'
    $uninstallExitCode = Invoke-SetupProcess $installedSetup @('--uninstall', '--quiet')
    if ($uninstallExitCode -ne 0) {
        throw "Uninstall bootstrap failed with exit code $uninstallExitCode."
    }

    $deadline = [DateTime]::UtcNow.AddSeconds(30)
    while ([DateTime]::UtcNow -lt $deadline -and ((Test-Path $installDirectory) -or (Test-Path $uninstallKey))) {
        Start-Sleep -Milliseconds 250
    }

    if (Test-Path $installDirectory) {
        throw 'Uninstaller smoke test failed. Installation directory remained after uninstall.'
    }
    if (Test-Path $uninstallKey) {
        throw 'Uninstaller smoke test failed. Windows uninstall registration remained after uninstall.'
    }

    Write-Host 'Setup install/Windows uninstall smoke test passed without a separate uninstall.exe.' -ForegroundColor Green
}
finally {
    Remove-Item -Path $uninstallKey -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item -Path $testRoot -Recurse -Force -ErrorAction SilentlyContinue
}
