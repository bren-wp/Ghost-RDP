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

## Current development status

The initial foundation implements:

- a .NET 8 solution with separate Core, App, Host, and test projects;
- a native WPF Windows shell using the Ghost RDP dark visual system;
- actual detection of the Microsoft `mstsc.exe` runtime without launching it or passing credentials;
- a visible Ghost RDP Host identity window that reports only data it can reliably read;
- shared input validation and log-secret sanitization primitives;
- CI for formatting, Release builds, tests, security regression checks, publish output, and package validation;
- architecture, security, privacy, Windows, host, remote-access, build, and roadmap documentation.

Saved computers, Quick Connect, live connection launching, session history, favorites, full host readiness diagnostics, VPN/RD Gateway UX, setup packaging, portable packaging, and authentic screenshots are **planned** and are not represented as completed runtime features.

## Security baseline

Ghost RDP does not silently expose Remote Desktop to the Internet. The project does not implement automatic router port forwarding, UPnP port forwarding, firewall bypass, NLA disabling, UAC bypass, hidden persistence, credential theft, or stealth host behavior.

Passwords are memory-only by default. They must not be serialized to profiles, written to logs, included in `.rdp` files, or passed on a process command line. See [SECURITY.md](docs/SECURITY.md).

## Privacy baseline

The initial architecture has no telemetry, analytics, ads, fingerprinting, or central Ghost RDP server carrying RDP traffic. See [PRIVACY.md](docs/PRIVACY.md).

## Build

Windows 10/11 and the .NET 8 SDK are the supported development baseline for this milestone.

```powershell
dotnet restore GhostRdp.sln
dotnet build GhostRdp.sln -c Release
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
