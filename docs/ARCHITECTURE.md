# Ghost RDP Architecture

## Goals

Ghost RDP is a Windows-first remote desktop management product. The architecture separates reusable domain/security logic from Windows UI and host diagnostics while keeping the shipped runtime dependency surface limited to .NET/WPF, Windows platform APIs, and Ghost RDP projects in this repository.

## Solution boundaries

- `GhostRdp.Core` contains product metadata, validation, profile models/storage, security helpers, Microsoft RDP launch primitives, remote-access route models, Host readiness domain models/evaluation, runtime detection abstractions, and host-neutral helpers.
- `GhostRdp.App` is the user-facing Windows WPF application and owns non-sensitive UI preference persistence and saved-computer presentation/query behavior.
- `GhostRdp.Host` is a separate, visible Windows application for read-only host-side readiness diagnostics.
- `GhostRdp.Setup` is the repository-owned per-user Windows installer/uninstaller application.
- `GhostRdp.Core.Tests` covers shared validation, profile persistence/migration/recovery, RDP file generation/process launch planning, Host readiness evaluation, and security behavior.
- `GhostRdp.App.Tests` covers application metadata, settings persistence, and deterministic saved-computer filtering/sorting behavior that can be tested without UI automation.

## Saved computer persistence

Saved computers use a schema-versioned JSON document under the current user's local application-data directory. Each profile also carries its own schema version and stable UUID. The profile schema intentionally has no password property.

Schema v2 adds `RemoteAccessMode` and an optional RD Gateway hostname. Schema-v1 stores and profiles are accepted as a known legacy format and migrated in memory to v2 with the Direct route. Load does not rewrite the source file. A current-schema document is written only after a later explicit profile change.

Writes are validated before serialization and use a random temporary file followed by replacement in the same directory. Before replacing an existing primary store, Ghost RDP reads and validates that exact current file. If the existing primary cannot be parsed, migrated, and validated, the normal Save path fails before writing either a replacement primary or a new backup.

When a valid primary already exists, its exact previous contents are first written and revalidated through a temporary file, then retained as `computers.json.bak`. The new primary is independently serialized and validated before replacement. This provides one previous validated store generation without silently treating an unreadable current file as disposable.

Backup recovery is explicit rather than automatic. `ComputerProfileStore.RestoreBackup()` validates the backup before any primary-file mutation. If the current primary is already valid, recovery is rejected to prevent accidental rollback. When recovery is appropriate, the unreadable primary is moved to a uniquely named `preserved-*` file in the same local-data directory, the validated backup is restored through a temporary file, and a failed finalization attempts to put the preserved original back at the primary path.

The WPF App stays read-only after a profile-load failure. After the main window is rendered, it offers recovery only when `computers.json.bak` itself validates. The user must explicitly approve recovery; declining leaves both files unchanged and keeps saved-computer changes disabled for that process. Invalid JSON, unsupported future schema versions, invalid profiles, and duplicate IDs therefore remain fail-closed rather than triggering automatic data rollback.

Quick Connect is modeled separately from a saved profile. Validation does not persist anything. Conversion to a saved computer happens only through the explicit `Save as computer` action.

## Saved-computer view efficiency

The persistent profile list is distinct from the transient visible view. `ComputerProfileViewQuery` deterministically applies search, favorites filtering, and sort order without mutating stored profiles.

Search text is debounced briefly on the WPF dispatcher so intermediate keystrokes do not trigger repeated full filter/sort/materialization cycles. Explicit sort and favorites changes refresh immediately. The UI caches the favorite count between profile mutations and restores selection after a view refresh when the selected UUID remains visible. The `ListBox` uses WPF virtualization and recycling to limit visual-container overhead.

## UI settings persistence

`GhostRdp.App.Settings` stores a separate schema-versioned `settings.json` under the current user's local application-data directory. It contains only UI preferences: startup view, default computer sort, remember-last-view state, and the last eligible view.

The settings store does not contain connection endpoints, usernames, domains, gateway addresses, passwords, tokens, or credential material. Writes use the same temporary-file-then-replace pattern as profiles. Missing settings return defaults in memory. Invalid JSON, invalid enum values, and unsupported future schemas are rejected without rewriting the source on load. After a failed settings load, automatic last-view persistence stays disabled until an explicit Save or Reset succeeds.

## Remote-access routes

`RemoteAccessMode` has three explicit values:

- `Direct`: connect directly to the target address;
- `PrivateNetwork`: connect directly while documenting that an already-established VPN/overlay route is expected;
- `RdGateway`: connect through an explicitly configured RD Gateway hostname.

Private-network mode deliberately has no VPN control-plane integration. Ghost RDP does not install, start, configure, authenticate to, or depend on a third-party VPN/overlay SDK.

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

Each probe can return unavailable/unknown data independently. The evaluator refuses to emit a green ready state when a required value is unknown. The Host has no configuration-changing code path and requires no third-party diagnostic framework.

## Trust boundaries

The local Windows account and Windows credential facilities are trusted platform boundaries. Remote hosts, RD Gateway hostnames, DNS results, network input, profile files, profile backups/recovery copies, imported settings, and future pairing data are untrusted inputs and must be validated before use.

Host diagnostics are local observations, not remote authorization. A detected VPN/tunnel adapter does not prove that a remote route is secure or reachable. Selecting Private Network similarly records connection intent rather than claiming a tunnel exists.

The current architecture has no Ghost RDP relay server and no backend dependency for carrying RDP traffic.

## Runtime truthfulness

The UI must not claim a connection, host readiness state, VPN state, firewall state, NLA state, gateway authentication state, session state, or profile recovery state unless a reliable runtime owner provides that information. Connect controls are enabled only when Microsoft `mstsc.exe` is actually available. Launch success means the Microsoft RDP process was started; it does not claim that authentication or the remote session succeeded.

Ghost RDP Host reports `Ready for Remote Desktop` only when its required local Windows diagnostics are known and pass. Unknown states stay visible as unknown. Saved-computer recovery is reported as successful only after the backup was validated and the recovered primary can be loaded.

## Dependency policy

Production projects under `src/` use only .NET 8, WPF/Windows Desktop, Windows platform APIs, and Ghost RDP `ProjectReference` relationships. Third-party runtime `PackageReference` and external file-based assembly `HintPath` references are prohibited by repository policy and checked by CI.

Tests may use the existing Microsoft test tooling because it is development-only and not shipped with App, Core, Host, or Setup. Production x86/x64/ARM64 packages are self-contained, so users do not need to install the .NET runtime separately. See [DEPENDENCIES.md](DEPENDENCIES.md).

## Documentation synchronization

Architecture-affecting changes must update this document together with README, CHANGELOG, ROADMAP, and the relevant domain documents in the same pull-request cycle. See [Documentation index](README.md).
