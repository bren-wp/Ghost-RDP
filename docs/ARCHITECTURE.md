# Ghost RDP Architecture

## Goals

Ghost RDP is a Windows-first remote desktop management product. The architecture separates reusable domain/security logic from Windows UI and host diagnostics so future platform-specific companions can be added without turning the Core project into a Windows shell wrapper.

## Solution boundaries

- `GhostRdp.Core` contains product metadata, validation, profile models/storage, security helpers, Microsoft RDP launch primitives, runtime detection abstractions, and host-neutral models.
- `GhostRdp.App` is the user-facing Windows WPF application.
- `GhostRdp.Host` is a separate, visible Windows application for host-side readiness and diagnostics.
- `GhostRdp.Core.Tests` covers shared validation, profile persistence, RDP file generation/process launch planning, and security behavior.
- `GhostRdp.App.Tests` covers application-facing metadata and behavior that can be tested without UI automation.

## Saved computer persistence

Saved computers use a schema-versioned JSON document under the current user's local application-data directory. Each profile also carries its own schema version and stable UUID. The profile schema intentionally has no password property.

Writes are validated before serialization and use a random temporary file followed by replacement in the same directory. Invalid JSON, unsupported schema versions, invalid profiles, and duplicate IDs are rejected. A corrupted store is not silently replaced by the app.

Quick Connect is modeled separately from a saved profile. Validation does not persist anything. Conversion to a saved computer happens only through the explicit `Save as computer` action.

## Microsoft RDP launch boundary

A Connect action converts validated profile or Quick Connect metadata into an `RdpConnectionRequest`. Core serializes that request into a temporary `.rdp` file containing no password. The file is placed in a random per-session temporary directory.

`mstsc.exe` is resolved by `RdpRuntimeDetector` and is launched directly with `UseShellExecute = false`. The `.rdp` path is supplied as one `ProcessStartInfo.ArgumentList` item; user-controlled connection values are never concatenated into a shell command.

Microsoft Remote Desktop/Windows owns credential entry. Ghost RDP cleans the temporary session directory after the RDP process exits and performs stale cleanup for files left after an abnormal application termination.

## Trust boundaries

The local Windows account and Windows credential facilities are trusted platform boundaries. Remote hosts, DNS results, network input, profile files, imported settings, and future pairing data are untrusted inputs and must be validated before use.

The initial architecture has no Ghost RDP relay server and no backend dependency for carrying RDP traffic.

## Runtime truthfulness

The UI must not claim a connection, host readiness state, VPN state, firewall state, NLA state, or session state unless a reliable runtime owner provides that information. Connect controls are enabled only when Microsoft `mstsc.exe` is actually available. Launch success means the Microsoft RDP process was started; it does not claim that authentication or the remote session succeeded.

## Dependency policy

Prefer the .NET runtime and Windows platform APIs. New third-party runtime dependencies require a concrete feature justification and security review. Test-only packages are kept to Microsoft test tooling in the initial milestones.
