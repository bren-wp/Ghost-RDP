# Ghost RDP 0.9.1 — development notes

Ghost RDP 0.9.1 is the current maintenance line following the first production multi-architecture release. It focuses on UI correctness, accessibility, saved-computer responsiveness, runtime dependency discipline, and documentation consistency without changing the RDP credential/security boundary.

This version is **not yet the published production release**. Version 0.9.0 remains the latest GitHub Release until the 0.9.1 exact release commit passes all release gates and is published.

## UI and UX

- App, Host, Setup, and profile-editor windows receive the intended Ghost RDP foreground/background style directly.
- Dark text fields and combo boxes use project-owned WPF templates rather than white platform-theme fallbacks.
- Standard text receives an explicit semantic foreground for readable dark-card content.
- Sidebar navigation and window branding use lightweight project-owned vector assets.
- High Contrast, keyboard focus, access keys, UI Automation names, and textual status reporting remain supported.

## Performance and stability

- Saved-computer `ListBox` rendering uses WPF virtualization and recycling.
- Text search uses a short 180 ms WPF dispatcher debounce instead of rebuilding the visible list for every intermediate keystroke.
- Explicit sort and favorites changes remain immediate.
- Selection is restored by UUID when the selected profile remains visible after refresh.
- Favorite totals are cached between profile mutations.
- Filtering/sorting is isolated in a deterministic component with dedicated App tests.
- The debounce timer is stopped and detached when the main window closes.

## Runtime dependency policy

- Production projects under `src/` use only .NET 8/WPF, Windows platform APIs, and Ghost RDP project references.
- CI rejects third-party runtime `PackageReference` or external file-based assembly dependencies in production projects.
- Existing Microsoft test tooling is development-only and is not shipped with production binaries.
- Packaging continues to use the repository-owned Ghost RDP Setup project; no third-party installer compiler is required.

## Documentation

- Added a central documentation index/synchronization policy.
- Added a dedicated runtime dependency policy.
- Synchronized README, Changelog, Roadmap, Architecture, Build, Security, Privacy, Accessibility, Windows, Host, Remote Access, Packaging, Release, Release Notes, and UI/UX documentation with the 0.9.1 behavior.
- README product imagery remains repository-owned and no longer depends on external badge-rendering services for core status presentation.
- Runtime screenshots remain authentic-only evidence.

## Packaging and security baseline

0.9.1 retains the 0.9.0 production packaging contract: self-contained x86, x64, and ARM64 App/Host/Setup/Portable artifacts, canonical `setup.exe` and `portable.exe`, SHA-256/integrity validation, native ARM64 smoke testing, and Windows Installed Apps uninstall through the same installed `GhostRDP-Setup.exe --uninstall` executable.

Ghost RDP still does not store RDP/RD Gateway passwords, add a central relay, configure a VPN, expose TCP 3389, modify firewall/NLA/service state, or install a hidden service. Current packages remain unsigned until authorized Authenticode signing is configured.

---

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
