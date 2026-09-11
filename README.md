<p align="center">
  <img src="assets/ghost-rdp-logo.svg" alt="Ghost RDP" width="420" />
</p>

# Ghost RDP

**Private remote desktop management for computers you control.**

[![Windows](https://img.shields.io/badge/Windows-10%2F11-5B7CFA)](docs/WINDOWS.md)
[![.NET](https://img.shields.io/badge/.NET-8.0-7A98FF)](docs/BUILD.md)
[![CI](https://github.com/bren-wp/Ghost-RDP/actions/workflows/ci.yml/badge.svg)](https://github.com/bren-wp/Ghost-RDP/actions/workflows/ci.yml)
[![Release](https://img.shields.io/badge/release-v0.9.0-4AD79B)](https://github.com/bren-wp/Ghost-RDP/releases/tag/v0.9.0)
[![Privacy](https://img.shields.io/badge/telemetry-none-4AD79B)](docs/PRIVACY.md)

Ghost RDP is a Windows-first desktop application for managing Microsoft Remote Desktop connections to computers the user controls. It is built around explicit user actions, Windows security boundaries, local persistence, accessibility, low background activity, and a no-telemetry privacy baseline.

Latest production release: **0.9.0**. Current development version: **0.9.1**.

<p align="center">
  <img src="assets/ghost-rdp-ui-overview.svg" alt="Ghost RDP interface and runtime overview" width="1000" />
</p>

## Production features

- Native WPF Windows client with saved computers, Quick Connect, search, favorites, sorting, Settings, and About views.
- Schema-versioned local computer profiles with stable UUIDs and atomic replacement writes.
- Direct/LAN, existing private VPN/overlay, and Microsoft RD Gateway connection routes.
- Safe `mstsc.exe` integration using validated temporary `.rdp` files, strict server authentication, CredSSP, structured process arguments, and no password field.
- Windows-owned credential entry; Ghost RDP does not accept or pass the RDP or RD Gateway password.
- Separate Ghost RDP Host application with read-only Windows readiness diagnostics.
- Windows High Contrast support, visible focus states, access keys, UI Automation names, and production-safe WPF exception handling.
- Self-contained x86, x64, and ARM64 App, Host, Setup, and Portable packages.
- Canonical `setup.exe` and `portable.exe` 32-bit/x86 compatibility downloads plus native x64 and ARM64 builds.
- Per-user Ghost RDP Setup with transactional staging/rollback and Windows Installed Apps registration.
- Windows uninstall through the installed `GhostRDP-Setup.exe --uninstall`; no separate `uninstall.exe` or `unins*.exe` is shipped.
- SHA-256 manifests, PE-architecture validation, runtime self-tests, package validation, and real install/uninstall smoke tests.
- Native ARM64 CI validation on a Windows ARM64 runner.
- Automated GitHub Release publishing only after all architecture jobs pass for the exact release commit.

## UI and UX

The 0.9.1 development pass standardizes the App, Host, and Setup visual system around the same dark palette and Windows-native interaction model. Derived WPF windows now receive the intended application background and foreground deterministically, all ordinary text receives an explicit readable foreground, text fields and dropdowns use matching dark templates, and the sidebar includes lightweight vector navigation icons.

The UI uses vector geometry rather than bitmap-heavy decoration, layout rounding/device-pixel snapping, practical hit targets, visible keyboard focus, and system High Contrast handling. See [UI and UX](docs/UI-UX.md) and [Accessibility](docs/ACCESSIBILITY.md).

## Performance and memory behavior

Ghost RDP does not run telemetry, analytics, advertising, a central relay, background polling loops, or an always-on Windows service. Saved-computer lists use WPF UI virtualization with recycling so off-screen rows are not retained as a full visual tree. Icons are vector-based and do not require large bitmap assets in memory.

Saved-computer text search uses a short debounce before applying filtering and sorting, avoiding a full filter/sort/materialization pass for every intermediate keystroke. Sort and favorites changes remain immediate, current selection is restored when the selected profile remains visible, and the favorites total is cached between profile mutations rather than recomputed on every view refresh. The filtering/sorting component is covered by App tests and uses only existing .NET/WPF functionality.

Actual working-set memory varies by Windows version, DPI, architecture, .NET runtime state, and the number of profiles displayed, so the project does not publish an artificial RAM guarantee. Performance work focuses on avoiding unnecessary background activity, keeping the visual tree compact, limiting transient allocations, and releasing temporary RDP files/process resources promptly.

## Security baseline

Passwords are not part of saved computers, Quick Connect persistence, UI settings, generated `.rdp` files, or process command lines. Windows/Microsoft Remote Desktop owns target and RD Gateway credential prompts.

Ghost RDP does not enable Remote Desktop, open firewall ports, configure UPnP or router forwarding, weaken NLA, install a VPN, create a hidden service, or add stealth persistence. Ghost RDP Host is diagnostic-only and does not change Windows RDP/service/firewall/network state. See [SECURITY.md](docs/SECURITY.md).

## Privacy baseline

There is no telemetry, analytics, advertising, fingerprinting, or central Ghost RDP relay carrying RDP traffic. Saved computers and UI preferences remain under the current Windows user's local application-data directory. Host diagnostics are read locally and are not uploaded. See [PRIVACY.md](docs/PRIVACY.md).

## Downloads

Production releases provide:

- `setup.exe` — x86/32-bit compatibility Setup;
- `portable.exe` — x86/32-bit compatibility Portable client;
- native x64 and ARM64 Setup/Portable executables;
- architecture-specific Portable ZIP packages and Host diagnostics binaries;
- `SHA256SUMS.txt` and `RELEASE-MANIFEST.json`.

The x86 compatibility executables also run on x64 Windows through Windows' x86 compatibility layer. Native x64 and ARM64 builds are provided for architecture-matched execution.

## Build

```powershell
dotnet restore GhostRdp.sln
dotnet format GhostRdp.sln --verify-no-changes --no-restore
dotnet build GhostRdp.sln -c Release --no-restore
dotnet test GhostRdp.sln -c Release --no-build
./scripts/build-release-packages.ps1 -Architecture all -OutputDirectory ./artifacts/release
./scripts/validate-release-package.ps1 -ReleaseDirectory ./artifacts/release -Architecture all
```

See [BUILD.md](docs/BUILD.md), [PACKAGING.md](docs/PACKAGING.md), and [RELEASE.md](docs/RELEASE.md).

## Documentation

- [Architecture](docs/ARCHITECTURE.md)
- [UI and UX](docs/UI-UX.md)
- [Security](docs/SECURITY.md)
- [Privacy](docs/PRIVACY.md)
- [Accessibility](docs/ACCESSIBILITY.md)
- [Windows packaging](docs/PACKAGING.md)
- [Release validation](docs/RELEASE.md)
- [Windows behavior](docs/WINDOWS.md)
- [Ghost RDP Host](docs/HOST.md)
- [Remote access model](docs/REMOTE-ACCESS.md)
- [Build](docs/BUILD.md)
- [Roadmap](docs/ROADMAP.md)
- [Changelog](CHANGELOG.md)

## Screenshots

Runtime screenshots are accepted only when captured from a real validated Windows build. Generated mockups are not presented as application screenshots. The Windows capture device is currently unavailable, so this branch intentionally uses the project logo and the technical UI overview above rather than fabricating screenshots.

## Signing

Current packages are unsigned. Authenticode signing will be added only when an authorized signing certificate or signing service is configured.

## License

This repository is licensed under the Mozilla Public License 2.0. See [LICENSE](LICENSE).
