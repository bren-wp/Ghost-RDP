param(
    [Parameter(Mandatory = $true)]
    [string]$AppPath,
    [Parameter(Mandatory = $true)]
    [string]$HostPath,
    [Parameter(Mandatory = $false)]
    [string]$SetupPath
)

$ErrorActionPreference = 'Stop'

function Invoke-SelfTest([string]$Path, [string]$Label) {
    $fullPath = [System.IO.Path]::GetFullPath($Path)
    if (-not (Test-Path $fullPath -PathType Leaf)) {
        throw "$Label self-test executable is missing: $fullPath"
    }

    $startInfo = [System.Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = $fullPath
    $startInfo.UseShellExecute = $false
    [void]$startInfo.ArgumentList.Add('--self-test')

    $process = [System.Diagnostics.Process]::Start($startInfo)
    if (-not $process) { throw "$Label self-test could not start." }
    try {
        if (-not $process.WaitForExit(30000)) {
            try { $process.Kill($true) } catch { }
            throw "$Label self-test timed out."
        }
        if ($process.ExitCode -ne 0) {
            throw "$Label self-test failed with exit code $($process.ExitCode)."
        }
    }
    finally {
        $process.Dispose()
    }
}

Invoke-SelfTest $AppPath 'Ghost RDP client'
Invoke-SelfTest $HostPath 'Ghost RDP Host'
if (-not [string]::IsNullOrWhiteSpace($SetupPath)) {
    Invoke-SelfTest $SetupPath 'Ghost RDP Setup'
}

Write-Host 'Portable runtime self-tests passed.' -ForegroundColor Green
