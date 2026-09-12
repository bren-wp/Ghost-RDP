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
$secondaryInstallDirectory = Join-Path $testRoot 'secondary-install'
$foreignDirectory = Join-Path $testRoot 'foreign-data'
$foreignSentinel = Join-Path $foreignDirectory 'sentinel.txt'
$helperProbeDirectory = Join-Path $testRoot 'helper-probe'
$helperProbe = Join-Path $helperProbeDirectory 'GhostRDP-Setup.exe'
$uninstallKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\GhostRDP'
$sentinelContent = "Ghost RDP installer ownership regression sentinel`r`n"
New-Item -ItemType Directory -Force -Path $testRoot,$foreignDirectory | Out-Null
[System.IO.File]::WriteAllText($foreignSentinel, $sentinelContent, [System.Text.UTF8Encoding]::new($false))
Remove-Item -Path $uninstallKey -Recurse -Force -ErrorAction SilentlyContinue

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

function ConvertTo-CanonicalPath([string]$Path) {
    if ([string]::IsNullOrWhiteSpace($Path)) {
        return $null
    }

    return [System.IO.Path]::GetFullPath([Environment]::ExpandEnvironmentVariables($Path.Trim())).TrimEnd(
        [System.IO.Path]::DirectorySeparatorChar,
        [System.IO.Path]::AltDirectorySeparatorChar)
}

function Assert-ForeignDirectoryUntouched {
    if (-not (Test-Path $foreignDirectory -PathType Container)) {
        throw 'Installer ownership regression failed. The foreign directory was removed.'
    }
    if (-not (Test-Path $foreignSentinel -PathType Leaf)) {
        throw 'Installer ownership regression failed. The foreign sentinel file was removed.'
    }

    $actualContent = [System.IO.File]::ReadAllText($foreignSentinel)
    if (-not [string]::Equals($actualContent, $sentinelContent, [System.StringComparison]::Ordinal)) {
        throw 'Installer ownership regression failed. The foreign sentinel file was modified.'
    }

    $foreignItems = @(Get-ChildItem -Path $foreignDirectory -Force)
    if ($foreignItems.Count -ne 1 -or -not [string]::Equals($foreignItems[0].FullName, $foreignSentinel, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw 'Installer ownership regression failed. Unexpected files were written into the foreign directory.'
    }
}

function Assert-RegisteredInstallAlive {
    if (-not (Test-Path $installDirectory -PathType Container)) {
        throw 'Installer ownership regression failed. The registered installation directory was removed.'
    }
    if (-not (Test-Path (Join-Path $installDirectory 'GhostRDP.exe') -PathType Leaf)) {
        throw 'Installer ownership regression failed. The registered Ghost RDP executable was removed.'
    }
    if (-not (Test-Path $uninstallKey)) {
        throw 'Installer ownership regression failed. The registered uninstall entry was removed.'
    }
}

try {
    $foreignInstallExitCode = Invoke-SetupProcess $setup @('--install', '--quiet', '--path', $foreignDirectory)
    if ($foreignInstallExitCode -eq 0) {
        throw 'Installer ownership regression failed. Setup accepted an existing unregistered directory.'
    }
    Assert-ForeignDirectoryUntouched

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
    $expectedInstallLocation = ConvertTo-CanonicalPath $installDirectory
    $registeredInstallLocation = ConvertTo-CanonicalPath ([string]$registration.InstallLocation)
    if (-not [string]::Equals($registeredInstallLocation, $expectedInstallLocation, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Installer smoke test failed. InstallLocation is incorrect. Expected '$expectedInstallLocation', registered '$registeredInstallLocation'."
    }

    $expectedInstalledSetup = Join-Path $expectedInstallLocation 'GhostRDP-Setup.exe'
    $expectedUninstallString = '"' + $expectedInstalledSetup + '" --uninstall'
    if (-not [string]::Equals([string]$registration.UninstallString, $expectedUninstallString, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Installer smoke test failed. Windows uninstall must use the installed Setup executable. Registered '$($registration.UninstallString)'."
    }

    $reinstallExitCode = Invoke-SetupProcess $setup @('--install', '--quiet', '--path', $installDirectory)
    if ($reinstallExitCode -ne 0) {
        throw "Installer ownership regression failed. Reinstalling the registered installation returned exit code $reinstallExitCode."
    }
    Assert-RegisteredInstallAlive

    $secondaryInstallExitCode = Invoke-SetupProcess $setup @('--install', '--quiet', '--path', $secondaryInstallDirectory)
    if ($secondaryInstallExitCode -eq 0) {
        throw 'Installer ownership regression failed. Setup accepted a second install path while another Ghost RDP installation was registered.'
    }
    if (Test-Path $secondaryInstallDirectory) {
        throw 'Installer ownership regression failed. Setup created the rejected secondary installation directory.'
    }
    Assert-RegisteredInstallAlive

    $foreignUninstallExitCode = Invoke-SetupProcess $setup @('--uninstall', '--quiet', '--path', $foreignDirectory)
    if ($foreignUninstallExitCode -eq 0) {
        throw 'Installer ownership regression failed. Uninstall accepted a directory that is not the registered Ghost RDP installation.'
    }
    Assert-ForeignDirectoryUntouched
    Assert-RegisteredInstallAlive

    New-Item -ItemType Directory -Force -Path $helperProbeDirectory | Out-Null
    Copy-Item -LiteralPath $setup -Destination $helperProbe -Force
    $foreignHelperExitCode = Invoke-SetupProcess $helperProbe @(
        '--uninstall-helper',
        '--install-dir', $foreignDirectory,
        '--parent-pid', '0',
        '--quiet')
    if ($foreignHelperExitCode -eq 0) {
        throw 'Installer ownership regression failed. The uninstall helper accepted a directory that is not the registered Ghost RDP installation.'
    }
    Assert-ForeignDirectoryUntouched
    Assert-RegisteredInstallAlive

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
    Assert-ForeignDirectoryUntouched

    Write-Host 'Setup ownership, install/update, and Windows uninstall smoke tests passed without a separate uninstall.exe.' -ForegroundColor Green
}
finally {
    Remove-Item -Path $uninstallKey -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item -Path $testRoot -Recurse -Force -ErrorAction SilentlyContinue
}
