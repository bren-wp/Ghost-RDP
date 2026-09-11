# Ghost RDP Architecture

## Goals

Ghost RDP is a Windows-first remote desktop management product. The architecture separates reusable domain/security logic from Windows UI and host diagnostics so future platform-specific companions can be added without turning the Core project into a Windows shell wrapper.

## Solution boundaries

- `GhostRdp.Core` contains product metadata, validation, profile models/storage, security helpers, Microsoft RDP launch primitives, remote-access route models, Host readiness domain models/evaluation, runtime detection abstractions, and host-neutral helpers.
- `GhostRdp.App` is the user-facing Windows WPF application and owns non-sensitive UI preference persistence.
- `GhostRdp.Host` is a separate, visible Windows application for read-only host-side readiness diagnostics.
- `GhostRdp.Core.Tests` covers shared validation, profile persistence/migration, RDP file generation/process launch planning, Host readiness evaluation, and security behavior.
- `GhostRdp.App.Tests` covers application metadata and UI-settings persistence behavior that can be tested without UI automation.

## Saved computer persistence

Saved computers use a schema-versioned JSON document under the current user's local application-data directory. Each profile also carries its own schema version and stable UUID. The profile schema intentionally has no password property.

Schema v2 adds `RemoteAccessMode` and an optional RD Gateway hostname. Schema-v1 stores and profiles are accepted as a known legacy format and migrated in memory to v2 with the Direct route. Load does not rewrite the source file. A current-schema document is written only after a later explicit profile change.

Writes are validated before serialization and use a random temporary file followed by replacement in the same directory. Invalid JSON, unsupported future schema versions, invalid profiles, and duplicate IDs are rejected. A corrupted store is not silently replaced by the app.

Quick Connect is modeled separately from a saved profile. Validation does not persist anything. Conversion to a saved computer happens only through the explicit `Save as computer` action.

## UI settings persistence

`GhostRdp.App.Settings` stores a separate schema-versioned `settings.json` under the current user's local application-data directory. It contains only UI preferences: startup view, default computer sort, remember-last-view state, and the last eligible view.

The settings store does not contain connection endpoints, usernames, domains, gateway addresses, passwords, tokens, or credential material. Writes use the same temporary-file-then-replace pattern as profiles. Missing settings return defaults in memory. Invalid JSON, invalid enum values, and unsupported future schemas are rejected without rewriting the source on load. After a failed settings load, automatic last-view persistence stays disabled until an explicit Save or Reset succeeds.

## Remote-access routes

`RemoteAccessMode` has three explicit values:

- `Direct`: connect directly to the target address;
- `PrivateNetwork`: connect directly while documenting that an already-established VPN/overlay route is expected;
- `RdGateway`: connect through an explicitly configured RD Gateway hostname.

Private-network mode deliberately has no VPN control-plane integration. Ghost RDP does not install, start, configure, or authenticate to VPN/overlay software.

RD Gateway mode is a real Microsoft RDP configuration owner. The generated `.rdp` includes the validated gateway hostname, explicit gateway usage/profile settings, Windows-selected gateway credential source, and separate credential prompting. No gateway password/token is accepted by the profile model.

## Microsoft RDP launch boundary

A Connect action converts validated profile or Quick Connect metadata into an `RdpConnectionRequest`. Core serializes that request into a temporary `.rdp` file containing no password. The file is placed in a random per-session temporary directory.

`mstsc.exe` is resolved by `RdpRuntimeDetector` and is launched directly with `UseShellExecute = false`. The `.rdp` path is supplied as one `ProcessStartInfo.ArgumentList` item; user-controlled connection values are never concatenated into a shell command.

Microsoft Remote Desktop/Windows owns credential entry for both the target and any configured RD Gateway. Ghost RDP cleans the temporary session directory after the RDP process exits and performs stale cleanup for files left after an abnormal application termination.

## Accessibility architecture

WPF controls use visible keyboard focus visuals, access keys, and UI Automation names for important navigation and actions. Status areas use textual state and polite live announcements where appropriate so state is not communicated by color alone.

Windows High Contrast is observed through `SystemParameters.HighContrast`. When active, Ghost RDP swaps semantic brush resources to Windows system colors; when disabled, the captured standard palette is restored. This presentation layer does not change RDP, credential, firewall, NLA, gateway, or host-diagnostic behavior.

## Host readiness architecture

`GhostRdp.Core.Host` defines a host-neutral readiness snapshot and a conservative evaluator. It does not query Windows itself. This makes the decision rules testable without fabricating OS state.

`GhostRdp.Host.Diagnostics` owns Windows-specific probes:

- Windows registry: edition/build, RDP enabled state, port, and NLA;
- Service Control Manager: `TermService` runtime state;
- Windows Firewall COM policy: active profiles, firewall enabled state, block-all-inbound state, and inbound allow-rule coverage;
- DNS/network interfaces: hostname, LAN addresses, and private-network/VPN adapter indicators.

Each probe can return unavailable/unknown data independently. The evaluator refuses to emit a green ready state when a required value is unknown. The Host has no configuration-changing code path.

## Trust boundaries

The local Windows account and Windows credential facilities are trusted platform boundaries. Remote hosts, RD Gateway hostnames, DNS results, network input, profile files, imported settings, and future pairing data are untrusted inputs and must be validated before use.

Host diagnostics are local observations, not remote authorization. A detected VPN/tunnel adapter does not prove that a remote route is secure or reachable. Selecting Private Network similarly records connection intent rather than claiming a tunnel exists.

The current architecture has no Ghost RDP relay server and no backend dependency for carrying RDP traffic.

## Runtime truthfulness

The UI must not claim a connection, host readiness state, VPN state, firewall state, NLA state, gateway authentication state, or session state unless a reliable runtime owner provides that information. Connect controls are enabled only when Microsoft `mstsc.exe` is actually available. Launch success means the Microsoft RDP process was started; it does not claim that authentication or the remote session succeeded.

Ghost RDP Host reports `Ready for Remote Desktop` only when its required local Windows diagnostics are known and pass. Unknown states stay visible as unknown.

## Dependency policy

Prefer the .NET runtime and Windows platform APIs. New third-party runtime dependencies require a concrete feature justification and security review. Host readiness uses Windows registry, Service Control Manager, Firewall COM, and .NET networking APIs rather than adding a diagnostic framework dependency.
