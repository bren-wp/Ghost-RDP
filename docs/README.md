# Ghost RDP Documentation

This directory is the maintained technical documentation set for Ghost RDP. Documentation is versioned with the source code and must be updated in the same pull request whenever behavior, packaging, security boundaries, dependencies, release behavior, UI/UX, privacy, accessibility, or supported Windows behavior changes.

Current documentation baseline: **0.9.1 release preparation**. Latest published production release remains **0.9.0** until the exact `release/v0.9.1` workflow succeeds and the GitHub Release exists.

## Documentation map

- [Architecture](ARCHITECTURE.md) — project boundaries, persistence, runtime ownership and dependency policy.
- [Dependencies](DEPENDENCIES.md) — no-third-party-runtime-dependency contract and CI enforcement.
- [Security](SECURITY.md) — credentials, process launch, Host, packaging and regression boundaries.
- [Privacy](PRIVACY.md) — local data, telemetry and network-data handling.
- [Accessibility](ACCESSIBILITY.md) — keyboard, focus, UI Automation and High Contrast behavior.
- [UI and UX](UI-UX.md) — visual system, controls, layout and performance behavior.
- [Windows](WINDOWS.md) — Windows runtime and host behavior.
- [Ghost RDP Host](HOST.md) — read-only incoming-RDP readiness diagnostics.
- [Remote access](REMOTE-ACCESS.md) — Direct/LAN, private-network and RD Gateway routing model.
- [Build](BUILD.md) — SDK requirements and repository build commands.
- [Packaging](PACKAGING.md) — Setup, Portable, architecture and uninstall contracts.
- [Release validation](RELEASE.md) — exact release gates, preflight requirements and release-branch rules.
- [Release notes](RELEASE-NOTES.md) — finalized 0.9.1 release notes used by automated publication.
- [Roadmap](ROADMAP.md) — implemented, development and planned milestones.

The root [README](../README.md) and [CHANGELOG](../CHANGELOG.md) are part of the same synchronized documentation set.

## Update rule

A pull request is not considered complete if its user-visible or architectural behavior is described incorrectly by any document above. Unaffected documents do not need artificial wording changes, but the complete documentation set must be reviewed for consistency before merge.

For every functional change, review at minimum the root README, CHANGELOG, ROADMAP, and the domain documents affected by the change. Release work must additionally review BUILD, PACKAGING, RELEASE, RELEASE-NOTES, SECURITY, PRIVACY and DEPENDENCIES.

## Release-documentation state

Release preparation is intentionally separated from publication state. A finalized CHANGELOG date and release-note body do not by themselves mean that a version is published. README and ROADMAP must continue to identify the actual latest GitHub Release until the release workflow succeeds.

The repository-owned `scripts/release-preflight.ps1` enforces version alignment in normal CI and finalized release metadata plus branch/version alignment in the release workflow.

## Evidence policy

CI results, release assets and runtime screenshots must be described truthfully. Generated mockups are never labeled as runtime screenshots. Current packages must never be described as Authenticode-signed unless an authorized signing mechanism actually produced and verified that signature.
