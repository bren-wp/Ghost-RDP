<p align="center">
  <img src="assets/ghost-rdp-logo.svg" alt="Ghost RDP" width="420" />
</p>

# Ghost RDP

**Private remote desktop management for computers you control.**

[![Windows](https://img.shields.io/badge/Windows-10%2F11-5B7CFA)](docs/WINDOWS.md)
[![.NET](https://img.shields.io/badge/.NET-8.0-7A98FF)](docs/BUILD.md)
[![CI](https://github.com/bren-wp/Ghost-RDP/actions/workflows/ci.yml/badge.svg)](https://github.com/bren-wp/Ghost-RDP/actions/workflows/ci.yml)
[![Privacy](https://img.shields.io/badge/telemetry-none-4AD79B)](docs/PRIVACY.md)

Ghost RDP is a Windows-first desktop application for managing Remote Desktop connections to computers the user controls. The project is intentionally designed around explicit user actions, Windows security boundaries, accessibility, and a no-telemetry privacy baseline.

Current development version: **0.8.0**.

## Implemented

- .NET 8 solution with separate Core, App, Host, and test projects;
- native WPF Windows UI using the Ghost RDP dark visual system;
- saved computers with add, edit, delete, duplicate, favorite, search, favorites filter, and sorting;
- schema-versioned local JSON profile storage with stable UUIDs, atomic replacement writes, and in-memory migration from v1 to v2;
- Quick Connect validation with an explicit `Save as computer` action and no hidden profile creation;
- profile metadata for host/IP, RDP port, username, domain, remote-access route, optional RD Gateway host, notes, favorite state, and tags;
- Direct/LAN, existing private VPN/overlay, and explicit RD Gateway connection routes;
- RD Gateway `.rdp` integration using documented Microsoft gateway properties while leaving gateway and target credential entry to Windows;
- actual Microsoft `mstsc.exe` detection and Connect actions that remain disabled when the runtime is unavailable;
- safe `mstsc.exe` launch integration using `ProcessStartInfo.ArgumentList` with `UseShellExecute = false`;
- random temporary `.rdp` session files containing validated connection metadata but no password field;
- strict server authentication requirement and CredSSP enabled in generated `.rdp` files;
- temporary `.rdp` cleanup after Microsoft RDP exits plus stale-session cleanup after abnormal termination;
- Windows-owned credential entry: Ghost RDP does not pass passwords to `mstsc.exe`, process arguments, profiles, settings, or `.rdp` files;
- separate, visible Ghost RDP Host application with read-only readiness diagnostics;
- Host checks for Windows edition, RDP enabled state, `TermService`, RDP port, NLA, firewall/profile state, inbound RDP rule availability, LAN addresses, and VPN/private-overlay adapter indicators;
- conservative Host readiness evaluation: unknown values never become a green ready state;
- schema-versioned local UI settings for startup view, default computer sort, and optional last-view memory;
- Settings and expanded About views that expose local storage/runtime boundaries without revealing secrets;
- keyboard access keys, visible focus treatment, assistive-technology automation names, polite status announcements, and automatic Windows High Contrast palette handling;
- self-contained x64 Portable client and Host executables plus a Portable ZIP;
- per-user Inno Setup x64 installer with standard uninstall registration and no firewall/service/network changes;
- SHA-256 manifest plus CI validation and real silent install/uninstall smoke testing of the generated setup;
- shared input validation and log-secret sanitization primitives;
- CI for formatting, Release builds, tests, security regression checks, development publish output, release packaging, package validation, and installer smoke testing;
- architecture, security, privacy, accessibility, packaging, release-validation, Windows, host, remote-access, build, and roadmap documentation.

## Security baseline

Passwords are not part of the saved computer, Quick Connect, or UI-settings persistence schemas. The Microsoft RDP launch flow deliberately does not accept or transport a password: Windows/Microsoft Remote Desktop owns credential entry when a session starts, including RD Gateway credential prompts. Secrets must not be serialized to profiles or settings, written to logs, included in `.rdp` files, or passed on a process command line. Corrupted or unsupported profile/settings stores are not silently overwritten by loading them. See [SECURITY.md](docs/SECURITY.md).

Ghost RDP does not silently expose Remote Desktop to the Internet or weaken Windows security controls. Private-network mode does not install or configure a VPN/overlay. Ghost RDP Host is diagnostic-only: it does not enable RDP, start services, alter firewall rules, change NLA, or modify network exposure. Setup and Portable packaging do not add a background service, firewall rule, scheduled task, or automatic RDP exposure.

## Privacy baseline

The current architecture has no telemetry, analytics, ads, fingerprinting, or central Ghost RDP server carrying RDP traffic. Saved computers and UI preferences remain under the current Windows user's local application-data directory. Temporary `.rdp` files are created only for a user-initiated connection, contain no password, and are cleaned after use. Host readiness diagnostics are read locally and are not uploaded. Uninstall leaves user-owned profile/settings data in place unless the user removes it separately. See [PRIVACY.md](docs/PRIVACY.md).

## Release status

Version 0.8.0 is the release-polish milestone. Automated Windows CI covers restore, formatting, Release build, tests, security regression checks, development publishing, self-contained Setup/Portable packaging, SHA-256 verification, archive validation, and real silent installer install/uninstall smoke testing.

Authentic Windows runtime screenshots remain intentionally pending until they can be captured from a real validated Windows build. Generated mockups are not presented as application evidence. Current development packages are unsigned; Authenticode signing is not claimed until an authorized certificate or signing service exists. See [RELEASE.md](docs/RELEASE.md) and [CHANGELOG.md](CHANGELOG.md).

## Build

Windows 10/11 and the .NET 8 SDK are the supported development baseline.

```powershell
dotnet restore GhostRdp.sln
dotnet format GhostRdp.sln --verify-no-changes --no-restore
dotnet build GhostRdp.sln -c Release --no-restore
dotnet test GhostRdp.sln -c Release --no-build
```

See [BUILD.md](docs/BUILD.md) for full instructions and [PACKAGING.md](docs/PACKAGING.md) for Setup/Portable packaging.

## Documentation

- [Architecture](docs/ARCHITECTURE.md)
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

Authentic runtime screenshots will be added only from a real validated Windows build. The required capture set and privacy rules are documented in [RELEASE.md](docs/RELEASE.md). Mockups or generated UI are not presented as real application screenshots.

## License

This repository is licensed under the Mozilla Public License 2.0. See [LICENSE](LICENSE).
