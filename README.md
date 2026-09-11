<p align="center">
  <img src="assets/ghost-rdp-logo.svg" alt="Ghost RDP" width="420" />
</p>

# Ghost RDP

**Private remote desktop management for computers you control.**

[![Windows](https://img.shields.io/badge/Windows-10%2F11-5B7CFA)](docs/WINDOWS.md)
[![.NET](https://img.shields.io/badge/.NET-8.0-7A98FF)](docs/BUILD.md)
[![CI](https://github.com/bren-wp/Ghost-RDP/actions/workflows/ci.yml/badge.svg)](https://github.com/bren-wp/Ghost-RDP/actions/workflows/ci.yml)
[![Privacy](https://img.shields.io/badge/telemetry-none-4AD79B)](docs/PRIVACY.md)

Ghost RDP is a Windows-first desktop application for managing Remote Desktop connections to computers the user controls. The project is intentionally designed around explicit user actions, Windows security boundaries, and a no-telemetry privacy baseline.

## Implemented

- .NET 8 solution with separate Core, App, Host, and test projects;
- native WPF Windows shell using the Ghost RDP dark visual system;
- saved computers with add, edit, delete, duplicate, favorite, search, favorites filter, and sorting;
- schema-versioned local JSON profile storage with stable UUIDs and atomic replacement writes;
- Quick Connect validation with an explicit `Save as computer` action and no hidden profile creation;
- profile metadata for host/IP, RDP port, username, domain, notes, favorite state, and tags;
- actual Microsoft `mstsc.exe` detection and Connect actions that remain disabled when the runtime is unavailable;
- safe `mstsc.exe` launch integration using `ProcessStartInfo.ArgumentList` with `UseShellExecute = false`;
- random temporary `.rdp` session files containing connection metadata but no password field;
- temporary `.rdp` cleanup after the Microsoft RDP process exits plus stale-session cleanup after abnormal termination;
- Windows-owned credential entry: Ghost RDP does not pass passwords to `mstsc.exe`, process arguments, or `.rdp` files;
- a visible Ghost RDP Host identity window that reports only data it can reliably read;
- shared input validation and log-secret sanitization primitives;
- CI for formatting, Release builds, tests, security regression checks, publish output, and package validation;
- architecture, security, privacy, Windows, host, remote-access, build, and roadmap documentation.

## Security baseline

Passwords are not part of the saved computer or Quick Connect persistence schema. The current Microsoft RDP launch flow deliberately does not accept or transport a password: Windows/Microsoft Remote Desktop owns credential entry when a session starts. Secrets must not be serialized to profiles, written to logs, included in `.rdp` files, or passed on a process command line. A corrupted or unsupported profile store is not silently overwritten by the app. See [SECURITY.md](docs/SECURITY.md).

Ghost RDP does not silently expose Remote Desktop to the Internet or weaken Windows security controls.

## Privacy baseline

The current architecture has no telemetry, analytics, ads, fingerprinting, or central Ghost RDP server carrying RDP traffic. Saved computers remain under the current Windows user's local application-data directory. Temporary `.rdp` files are created only for a user-initiated connection, contain no password, and are cleaned after use. See [PRIVACY.md](docs/PRIVACY.md).

## Planned next

Ghost RDP Host readiness diagnostics are the next milestone. Session history, VPN/RD Gateway-aware UX, settings and accessibility polish, Setup/Portable packaging, and authentic screenshots remain planned.

## Build

Windows 10/11 and the .NET 8 SDK are the supported development baseline.

```powershell
dotnet restore GhostRdp.sln
dotnet format GhostRdp.sln --verify-no-changes --no-restore
dotnet build GhostRdp.sln -c Release --no-restore
dotnet test GhostRdp.sln -c Release --no-build
```

See [BUILD.md](docs/BUILD.md) for full instructions.

## Documentation

- [Architecture](docs/ARCHITECTURE.md)
- [Security](docs/SECURITY.md)
- [Privacy](docs/PRIVACY.md)
- [Windows behavior](docs/WINDOWS.md)
- [Ghost RDP Host](docs/HOST.md)
- [Remote access model](docs/REMOTE-ACCESS.md)
- [Build](docs/BUILD.md)
- [Roadmap](docs/ROADMAP.md)

## Screenshots

Authentic runtime screenshots will be added only after the relevant UI milestones are implemented and validated on Windows. Mockups are not presented as real application screenshots.

## License

This repository is licensed under the Mozilla Public License 2.0. See [LICENSE](LICENSE).
