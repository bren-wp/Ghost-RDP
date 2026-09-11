# Changelog

All notable user-facing changes to Ghost RDP are recorded here.

## 0.9.1 - development

UI/UX consistency, accessibility hardening, and lower visual-tree overhead after the first production multi-architecture release.

### Changed

- App, Host, and Setup now apply the Ghost RDP window theme directly to their concrete WPF window classes instead of depending on an implicit base `Window` style.
- Standard text receives an explicit readable foreground so dark cards cannot inherit black Windows default text.
- Text fields use a rounded Ghost RDP dark template with explicit focus and disabled states.
- Combo boxes use a project-owned dark template and popup instead of falling back to a white Windows theme surface.
- Sidebar navigation now includes lightweight vector icons.
- Saved-computer lists explicitly enable WPF virtualization and recycling to reduce visual-container overhead for larger profile collections.
- WPF title/taskbar windows use a vector Ghost RDP application mark.
- README now includes the project logo, release/status badges, a technical UI/runtime overview, and explicit UI/performance documentation.

### Documentation

- Added `docs/UI-UX.md` covering visual hierarchy, control behavior, memory/performance decisions, accessibility, and screenshot policy.
- Runtime screenshots remain restricted to authentic validated Windows builds; no generated mockups are presented as application screenshots.

## 0.9.0 - 2026-09-11

Production packaging, stability hardening, and multi-architecture Windows support.

### Added

- Self-contained x86, x64, and ARM64 App, Host, Setup, and Portable packages.
- Canonical `setup.exe` and `portable.exe` x86 compatibility downloads for 32-bit Windows and x64 compatibility.
- Native ARM64 validation on GitHub's Windows ARM64 runner.
- Ghost RDP Setup with per-user installation, Start menu shortcuts, Windows Installed Apps registration, transactional staging, rollback, and optional local-data removal during uninstall.
- Runtime self-tests for App, Host, and Setup used by CI.
- PE-machine architecture checks and combined multi-architecture release assembly.
- Automatic GitHub Release publishing from exact `release/v<version>` branches.

### Changed

- App and Host production crash handling now closes safely on unexpected UI exceptions instead of leaving an unstable process running.
- WPF rendering uses layout rounding and device-pixel snapping for crisper UI rendering.
- Disabled production buttons use a non-interactive cursor and clearer visual state.
- Release packaging no longer depends on Inno Setup.

### Setup and uninstall

- Windows uninstall uses the installed `GhostRDP-Setup.exe --uninstall` entry.
- No separate `uninstall.exe` or `unins*.exe` is shipped or installed.
- Saved computers and settings are preserved by default; interactive uninstall can explicitly remove them.

### Security and privacy

- No saved or command-line RDP passwords; Windows/Microsoft Remote Desktop owns credential entry.
- No telemetry, analytics, ads, central RDP relay, automatic VPN configuration, UPnP, router port forwarding, firewall weakening, NLA disabling, or hidden service installation.
- Current packages remain unsigned until an authorized Authenticode certificate or signing service is configured.

## 0.8.0 - 2026-09-11

Release-candidate polish after the first complete Windows MVP milestone set.

### Added

- Read-only Ghost RDP Host readiness diagnostics backed by Windows registry, Service Control Manager, Firewall policy, DNS, and network-interface state.
- Explicit Direct/LAN, existing private VPN/overlay, and RD Gateway connection routes.
- Schema-versioned UI settings, expanded About/runtime transparency, keyboard access, UI Automation metadata, and Windows High Contrast support.
- Self-contained x64 Portable client/Host executables, Portable ZIP, per-user installer, SHA-256 manifest, and install/uninstall CI smoke tests.

### Security and privacy

- No saved or command-line passwords; Windows/Microsoft Remote Desktop owns credential entry.
- Host readiness remains read-only and conservative: unavailable required diagnostics never produce a green Ready state.
