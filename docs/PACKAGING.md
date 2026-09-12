# Windows Packaging

Ghost RDP produces self-contained Windows packages for x86 (32-bit), x64, and ARM64 from the same source revision. Packaging is repository-owned and does not require a third-party installer compiler.

## Release artifacts

A complete release contains:

- `setup.exe` — canonical x86/32-bit compatibility Setup;
- `portable.exe` — canonical x86/32-bit compatibility client;
- `GhostRDP-Setup-x86.exe`, `GhostRDP-Portable-x86.exe`, `GhostRDP-Host-x86.exe`, and `GhostRDP-Portable-x86.zip`;
- equivalent native `x64` and `arm64` Setup, Portable, Host, and ZIP artifacts;
- `LICENSE.txt`;
- `RELEASE-MANIFEST.json`;
- `SHA256SUMS.txt`.

The x86 compatibility executables are intentionally also exposed under the simple `setup.exe` and `portable.exe` names. Native x64 and ARM64 builds are available for architecture-matched execution.

## Runtime dependency baseline

App, Host, and Setup production projects use the repository's .NET/WPF/Windows stack and Ghost RDP project references only. No third-party runtime NuGet package or external file-based runtime assembly is part of the production source projects.

Release binaries are self-contained, so end users do not need to install a separate .NET runtime. See [DEPENDENCIES.md](DEPENDENCIES.md).

## Release preflight

Before production packaging, normal CI runs `scripts/release-preflight.ps1` to verify that App, Host, and Setup versions match. Release-preparation validation additionally checks finalized CHANGELOG and release-note metadata, while the actual publication workflow also requires the exact `release/v<version>` branch name.

This metadata preflight runs before architecture packaging so an inconsistent release cannot produce or publish mislabeled artifacts.

## Setup behavior

Ghost RDP Setup is a project in this repository, not a third-party generated installer/uninstaller model. It installs per-user under `%LOCALAPPDATA%\Programs\Ghost RDP` by default and therefore does not require elevation for the normal path.

Setup installs:

- `GhostRDP.exe`;
- `GhostRDP-Host.exe`;
- `GhostRDP-Setup.exe`;
- `LICENSE.txt`;
- Start menu shortcuts for the client and Host tool;
- a Windows Installed Apps entry under the current user.

A fresh install creates a new target directory. Setup does not take over an arbitrary pre-existing directory: if the requested target already exists, its canonical path must match the `InstallLocation` in the current user's Ghost RDP Windows Installed Apps registration. If an installation is already registered at another path, Setup rejects a second path instead of orphaning the existing installation.

Installation uses a staging directory. A recognized existing installation is moved to a temporary backup only after the new payload is extracted and validated. If finalization fails, Setup attempts to restore the previous installation instead of leaving a half-written directory. Reinstall/update on the same registered path remains supported.

## Uninstall behavior

Windows Installed Apps invokes the installed `GhostRDP-Setup.exe --uninstall` entry. There is no separate persistent `uninstall.exe`, `unins*.exe`, or standalone uninstaller binary in either Setup or Portable packages.

Before removal, the normal uninstall bootstrap requires the canonical requested target to match the current-user Ghost RDP `InstallLocation`. The temporary helper repeats the same check immediately before recursive deletion. An unregistered or mismatched directory is rejected even if supplied through `--path` or by direct helper invocation. If the registered program directory has already disappeared, uninstall may still remove stale shortcuts and the Windows Installed Apps registration without attempting to delete an unrelated path.

During removal, Setup copies the same Setup executable to a temporary location so Windows can delete the installed copy and application directory after the original process exits. The temporary helper schedules its own cleanup and is not an installed product component.

The complete `%LOCALAPPDATA%\Ghost RDP` local-data directory is preserved by default. This includes `computers.json`, its `computers.json.bak` previous-valid-store backup, any `computers.preserved-*.json` recovery copies, and `settings.json`. The interactive uninstall UI has an explicit option to remove that local data directory when the user wants complete local-data deletion.

## Security boundaries

Setup and Portable packaging do not:

- enable Remote Desktop;
- open TCP 3389 or other firewall ports;
- change NLA or Windows security policy;
- install a background service;
- create a scheduled task or stealth persistence;
- configure VPN software, UPnP, or router port forwarding;
- store RDP or RD Gateway passwords;
- install a third-party runtime component.

Setup also does not recursively replace or remove an arbitrary existing `--path`; ownership is bound to the canonical path recorded for Ghost RDP in Windows Installed Apps.

## Build and validation

```powershell
./scripts/security-regression.ps1
./scripts/release-preflight.ps1
./scripts/build-release-packages.ps1 -Architecture all -OutputDirectory ./artifacts/release
./scripts/validate-release-package.ps1 -ReleaseDirectory ./artifacts/release -Architecture all
./scripts/smoke-test-portable.ps1 -AppPath ./artifacts/release/GhostRDP-Portable-x64.exe -HostPath ./artifacts/release/GhostRDP-Host-x64.exe -SetupPath ./artifacts/release/GhostRDP-Setup-x64.exe
./scripts/test-installer.ps1 -SetupPath ./artifacts/release/GhostRDP-Setup-x64.exe
```

Validation checks expected files, minimum artifact sizes, SHA-256 hashes, Portable ZIP contents, PE machine architecture, absence of static `.rdp` files, absence of separate uninstall executables, and the no-third-party-runtime-dependency source policy. The installer smoke test additionally uses an existing foreign directory with a sentinel file to verify that rejected install, uninstall, and direct-helper targets leave unrelated data untouched; verifies same-path reinstall/update; rejects a second install path while Ghost RDP is registered elsewhere; then completes the normal uninstall lifecycle. CI additionally runs the ARM64 package and the same installer lifecycle on a native Windows ARM64 runner.

Saved-computer backup/recovery correctness is covered by Core automated tests before packaging, including backup rotation, fail-closed behavior for an unreadable primary, explicit recovery/preservation, corrupt-backup rejection, and valid-primary rollback rejection.

## Signing

Current packages are unsigned. Authenticode signing must be added only when an authorized signing certificate or signing service exists. The release process must not fabricate a signing-success claim.

## Documentation synchronization

Packaging changes must update README, CHANGELOG, ROADMAP, Build, Release, Release Notes, Security, Privacy, Dependencies, Windows, and this document when affected. See [Documentation index](README.md).
