# Windows Platform Notes

## Baseline

Ghost RDP App, Host, and Setup target .NET 8 on Windows with WPF. Windows 10 and Windows 11 are the intended desktop environments for the current release line.

Production release binaries are self-contained for x86, x64, and ARM64, so end users do not need to install the .NET runtime separately.

## Microsoft Remote Desktop runtime

Ghost RDP detects `mstsc.exe` by checking the Windows System32 location and entries on `PATH`. Detection does not imply that a remote endpoint is reachable.

If `mstsc.exe` is absent, Connect actions remain disabled and the application reports the runtime as unavailable.

`mstsc.exe` is a Windows-owned runtime dependency. Ghost RDP does not bundle a third-party RDP engine, browser-based RDP runtime, relay SDK, or replacement protocol implementation.

## Connect behavior

A user-initiated Connect action validates the host/IP, port, username, and domain, creates a random temporary `.rdp` file, and starts the detected `mstsc.exe` directly. The temporary file path is passed as a structured process argument rather than as a shell command string.

The `.rdp` file contains no password. Microsoft Remote Desktop/Windows owns credential entry and authentication. Ghost RDP reports only that the Microsoft RDP process was started; it does not claim that login or the remote desktop session succeeded.

Temporary session directories are cleaned when the Microsoft RDP process exits. Directories left after an abnormal Ghost RDP termination are eligible for stale cleanup after 24 hours.

## UI behavior

The 0.9.1 development line uses project-owned WPF styles/templates and vector assets so Windows default white controls/black text do not leak into the dark application surface. Windows High Contrast remains authoritative and maps semantic application brushes to system colors.

Saved-computer lists use WPF virtualization/recycling. Search filtering is debounced briefly on the WPF dispatcher; explicit sort/favorite changes remain immediate.

## Incoming RDP host editions

Ghost RDP Host treats Windows Professional, Enterprise, Education, and Windows Server families as supported incoming RDP host families when the local edition can be identified reliably. Windows Home/Core editions are reported as unsupported hosts. Unknown editions stay unknown rather than being assumed supported.

## Host readiness diagnostics

Ghost RDP Host reads the following Windows-owned state without modifying it:

- `fDenyTSConnections` for incoming Remote Desktop configuration;
- `RDP-Tcp\PortNumber` for the configured listening port;
- `RDP-Tcp\UserAuthentication` for NLA;
- Service Control Manager status for `TermService`;
- `HNetCfg.FwPolicy2` for active firewall profiles, firewall enabled state, block-all-inbound state, and firewall rules;
- DNS/network-interface APIs for hostname, LAN addresses, and tunnel/private-network adapter indicators.

The Host checks firewall rules by properties such as enabled state, inbound direction, allow action, TCP protocol, profile coverage, service name, and local port. It does not rely on a localized display name for the built-in Remote Desktop rule.

Multiple firewall profiles can be active at the same time. The readiness evaluator requires coverage for all active profiles before reporting the firewall rule as available.

The Host is read-only. It does not enable RDP, start services, alter firewall policy, change NLA, change the listening port, or configure external network exposure.

## Runtime dependency policy

Production projects use .NET/WPF and Windows platform capabilities without third-party runtime NuGet packages or external file-based assemblies. CI enforces this under `src/`. See [DEPENDENCIES.md](DEPENDENCIES.md).

## Packaging behavior

Setup installs per-user and registers Ghost RDP in Windows Installed Apps. Windows invokes the installed `GhostRDP-Setup.exe --uninstall`; no separate persistent uninstaller executable is installed. See [PACKAGING.md](PACKAGING.md).

## Documentation synchronization

Windows behavior changes must be reviewed against README, CHANGELOG, ROADMAP, Architecture, Security, Accessibility, Host, Packaging, Release, and this document. See [Documentation index](README.md).
