# Ghost RDP Architecture

## Goals

Ghost RDP is a Windows-first remote desktop management product. The architecture separates reusable domain/security logic from Windows UI and host diagnostics so future platform-specific companions can be added without turning the Core project into a Windows shell wrapper.

## Solution boundaries

- `GhostRdp.Core` contains product metadata, validation, security helpers, runtime detection abstractions, and host-neutral models.
- `GhostRdp.App` is the user-facing Windows WPF application.
- `GhostRdp.Host` is a separate, visible Windows application for host-side readiness and diagnostics.
- `GhostRdp.Core.Tests` covers shared validation and security behavior.
- `GhostRdp.App.Tests` covers application-facing metadata and behavior that can be tested without UI automation.

## Trust boundaries

The local Windows account and Windows credential facilities are trusted platform boundaries. Remote hosts, DNS results, network input, profile files, imported settings, and future pairing data are untrusted inputs and must be validated before use.

The initial architecture has no Ghost RDP relay server and no backend dependency for carrying RDP traffic.

## Runtime truthfulness

The UI must not claim a connection, host readiness state, VPN state, firewall state, NLA state, or session state unless a reliable runtime owner provides that information. Features that are not implemented stay out of the primary runtime navigation rather than appearing as fake controls.

## Dependency policy

Prefer the .NET runtime and Windows platform APIs. New third-party runtime dependencies require a concrete feature justification and security review. Test-only packages are kept to Microsoft test tooling in the initial milestone.
