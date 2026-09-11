# Ghost RDP 0.9.0

Ghost RDP 0.9.0 is the first production-packaged multi-architecture Windows release.

## Downloads

- `setup.exe` — x86/32-bit compatibility installer; also runs on x64 Windows through Windows x86 compatibility.
- `portable.exe` — x86/32-bit compatibility portable client.
- `GhostRDP-Setup-x64.exe` and `GhostRDP-Portable-x64.exe` — native x64 builds.
- `GhostRDP-Setup-arm64.exe` and `GhostRDP-Portable-arm64.exe` — native Windows ARM64 builds.
- Architecture-specific Host diagnostics executables and Portable ZIP packages are included as well.
- `SHA256SUMS.txt` contains SHA-256 hashes for release executables and archives.

## Production hardening

- App, Host, and Setup are aligned at version 0.9.0.
- Added production-safe process-level crash handling for the WPF App and Host.
- Added deterministic runtime self-test entry points used by CI without exposing development UI.
- Added native x86, x64, and ARM64 self-contained packaging.
- Added PE-machine validation so architecture labels cannot silently point at the wrong binary type.
- Added real install/uninstall smoke testing for Setup on x86, x64, and native ARM64 Windows runners.
- Setup uses transactional staging and rollback when replacing an existing installation.

## Setup and uninstall

Ghost RDP Setup installs per-user under `%LOCALAPPDATA%\Programs\Ghost RDP` by default and registers Ghost RDP in Windows Installed Apps. Windows invokes the installed `GhostRDP-Setup.exe --uninstall` entry when the user removes the application. No separate `uninstall.exe` or `unins*.exe` is shipped or installed.

Saved computers and UI settings are preserved by default during uninstall. The interactive uninstaller offers an explicit opt-in to remove that local user data as well.

## Security and privacy

Setup does not enable Remote Desktop, open firewall ports, create a service, configure UPnP/port forwarding, weaken NLA, install a VPN, or store RDP passwords. Ghost RDP continues to leave credential entry to Windows/Microsoft Remote Desktop.

There is no telemetry, analytics, advertising, or central Ghost RDP relay carrying RDP traffic.

## Signing

The release is currently unsigned. Authenticode signing is not claimed until an authorized signing certificate or signing service is configured.
