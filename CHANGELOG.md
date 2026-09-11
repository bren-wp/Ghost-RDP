# Changelog

All notable user-facing changes to Ghost RDP are recorded here.

## 0.8.0 - 2026-09-11

Release-candidate polish after the first complete Windows MVP milestone set.

### Added

- Read-only Ghost RDP Host readiness diagnostics backed by Windows registry, Service Control Manager, Firewall policy, DNS, and network-interface state.
- Explicit Direct/LAN, existing private VPN/overlay, and RD Gateway connection routes.
- Schema-versioned UI settings, expanded About/runtime transparency, keyboard access, UI Automation metadata, and Windows High Contrast support.
- Self-contained x64 Portable client/Host executables, Portable ZIP, per-user Inno Setup installer, SHA-256 manifest, and install/uninstall CI smoke tests.
- Release-validation and authentic-screenshot capture guidance.

### Security and privacy

- No saved or command-line passwords; Windows/Microsoft Remote Desktop owns credential entry.
- No telemetry, analytics, ads, central RDP relay, automatic VPN configuration, UPnP, router port forwarding, firewall weakening, NLA disabling, or hidden service installation.
- Host readiness remains read-only and conservative: unavailable required diagnostics never produce a green Ready state.
- Current packages are unsigned; Authenticode is not claimed until an authorized signing certificate or service exists.

### Remaining release evidence

Authentic application screenshots are intentionally not fabricated. They must be captured from a real Windows build after UI/runtime validation and added in a later commit before the screenshot portion of milestone PR 8 is marked complete.
