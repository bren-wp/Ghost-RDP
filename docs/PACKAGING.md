# Windows Packaging

Ghost RDP produces x64 Setup and Portable artifacts from the same Release source revision.

## Release artifacts

`./scripts/build-release-packages.ps1` creates:

- `GhostRDP-Portable-x64.exe` — self-contained single-file Ghost RDP client;
- `GhostRDP-Host-x64.exe` — self-contained single-file Host readiness tool;
- `GhostRDP-Portable-x64.zip` — portable client, Host tool, and license in one archive;
- `GhostRDP-Setup-x64.exe` — per-user Windows installer built with Inno Setup;
- `LICENSE.txt`;
- `SHA256SUMS.txt` — SHA-256 hashes for the executable and archive deliverables.

The self-contained publish includes the .NET runtime needed by the application. Portable mode does not register a service, firewall rule, protocol handler, scheduled task, or startup persistence.

## Setup behavior

The installer is intentionally per-user and defaults to `%LOCALAPPDATA%\Programs\Ghost RDP`, so it does not require elevation for the normal install path. It installs:

- `GhostRDP.exe`;
- `GhostRDP-Host.exe`;
- `LICENSE.txt`;
- Start menu shortcuts for the client and Host tool;
- an optional desktop shortcut when selected by the user;
- the standard Inno Setup uninstall registration.

Setup does not enable Remote Desktop, open TCP 3389, change Windows Firewall/NLA, configure VPN software, install a background service, or create stealth persistence.

Uninstall removes installed program files and shortcuts. It deliberately does not delete `%LOCALAPPDATA%\Ghost RDP\computers.json` or `settings.json`; saved computers and UI preferences remain user-owned data unless the user removes them separately.

## Build requirements

- Windows;
- .NET 8 SDK;
- PowerShell;
- Inno Setup 6 (`ISCC.exe`).

GitHub's Windows 2025 runner image currently includes Inno Setup, so CI uses the installed compiler instead of downloading an installer tool during the build.

## Local build

```powershell
./scripts/build-release-packages.ps1 -OutputDirectory ./artifacts/release
./scripts/validate-release-package.ps1 -ReleaseDirectory ./artifacts/release
./scripts/test-installer.ps1 -SetupPath ./artifacts/release/GhostRDP-Setup-x64.exe
```

The installer smoke test performs a silent install into a temporary directory, verifies the expected binaries, runs the generated uninstaller, and verifies that the installed application binaries are removed.

## Signing

Current development packages are unsigned. Authenticode signing must be added as a release-pipeline concern only when an authorized code-signing certificate is available. The build must never fabricate a signing-success claim when no real certificate/signing service is configured.
