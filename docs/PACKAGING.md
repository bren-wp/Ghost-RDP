# Windows Packaging

Ghost RDP produces self-contained Windows packages for x86 (32-bit), x64, and ARM64 from the same source revision.

## Release artifacts

A complete release contains:

- `setup.exe` — canonical x86/32-bit compatibility Setup;
- `portable.exe` — canonical x86/32-bit compatibility client;
- `GhostRDP-Setup-x86.exe`, `GhostRDP-Portable-x86.exe`, `GhostRDP-Host-x86.exe`, and `GhostRDP-Portable-x86.zip`;
- equivalent native `x64` and `arm64` Setup, Portable, Host, and ZIP artifacts;
- `LICENSE.txt`;
- `RELEASE-MANIFEST.json`;
- `SHA256SUMS.txt`.

The x86 compatibility executables are intentionally also exposed under the simple `setup.exe` and `portable.exe` names. Native x64 and ARM64 builds are available for users who want architecture-matched binaries.

## Setup behavior

Ghost RDP Setup is a project in the repository, not a third-party generated uninstaller model. It installs per-user under `%LOCALAPPDATA%\Programs\Ghost RDP` by default and therefore does not require elevation for the normal path.

Setup installs:

- `GhostRDP.exe`;
- `GhostRDP-Host.exe`;
- `GhostRDP-Setup.exe`;
- `LICENSE.txt`;
- Start menu shortcuts for the client and Host tool;
- a Windows Installed Apps entry under the current user.

Installation uses a staging directory. An existing installation is moved to a temporary backup only after the new payload is extracted and validated. If finalization fails, Setup attempts to restore the previous installation instead of leaving a half-written directory.

## Uninstall behavior

Windows Installed Apps invokes the installed `GhostRDP-Setup.exe --uninstall` entry. There is no separate persistent `uninstall.exe`, `unins*.exe`, or standalone uninstaller binary in either Setup or Portable packages.

During removal, Setup copies the same Setup executable to a temporary location so Windows can delete the installed copy and application directory after the original process exits. The temporary helper schedules its own cleanup and is not an installed product component.

Saved computers and UI settings under `%LOCALAPPDATA%\Ghost RDP` are preserved by default. The interactive uninstall UI has an explicit option to remove that local user data as well.

## Security boundaries

Setup and Portable packaging do not:

- enable Remote Desktop;
- open TCP 3389 or other firewall ports;
- change NLA or Windows security policy;
- install a background service;
- create a scheduled task or stealth persistence;
- configure VPN software, UPnP, or router port forwarding;
- store RDP or RD Gateway passwords.

## Build and validation

```powershell
./scripts/build-release-packages.ps1 -Architecture all -OutputDirectory ./artifacts/release
./scripts/validate-release-package.ps1 -ReleaseDirectory ./artifacts/release -Architecture all
./scripts/smoke-test-portable.ps1 -AppPath ./artifacts/release/GhostRDP-Portable-x64.exe -HostPath ./artifacts/release/GhostRDP-Host-x64.exe -SetupPath ./artifacts/release/GhostRDP-Setup-x64.exe
./scripts/test-installer.ps1 -SetupPath ./artifacts/release/GhostRDP-Setup-x64.exe
```

Validation checks expected files, minimum artifact sizes, SHA-256 hashes, Portable ZIP contents, PE machine architecture, absence of static `.rdp` files, and absence of separate uninstall executables. CI additionally runs the ARM64 package on a native Windows ARM64 runner.

## Signing

Current packages are unsigned. Authenticode signing must be added only when an authorized signing certificate or signing service exists. The release process must not fabricate a signing-success claim.
