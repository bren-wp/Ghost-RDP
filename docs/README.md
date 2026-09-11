# Ghost RDP Documentation

This directory is the maintained technical documentation set for Ghost RDP. Documentation is versioned with the source code and must be updated in the same pull request whenever behavior, packaging, security boundaries, dependencies, release behavior, UI/UX, privacy, accessibility, or supported Windows behavior changes.

Current documentation baseline: **Ghost RDP 0.9.1 production**. The published GitHub Release is `v0.9.1`, built from source commit `c0ec01cf286ac422c0f2c41db1b456a2b0e62ca3` after the guarded multi-architecture release workflow completed successfully.

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
- [Release validation](RELEASE.md) — exact release gates, preflight requirements, current verified release, and release-branch rules.
- [Release notes](RELEASE-NOTES.md) — Ghost RDP 0.9.1 release notes used by automated publication.
- [Roadmap](ROADMAP.md) — implemented, development and planned milestones.

The root [README](../README.md) and [CHANGELOG](../CHANGELOG.md) are part of the same synchronized documentation set.

## Update rule

A pull request is not considered complete if its user-visible or architectural behavior is described incorrectly by any document above. Unaffected documents do not need artificial wording changes, but the complete documentation set must be reviewed for consistency before merge.

For every functional change, review at minimum the root README, CHANGELOG, ROADMAP, and the domain documents affected by the change. Release work must additionally review BUILD, PACKAGING, RELEASE, RELEASE-NOTES, SECURITY, PRIVACY and DEPENDENCIES.

## Current release state

Ghost RDP `v0.9.1` is published as a production GitHub Release, not a draft or prerelease. Publish Release run `34659493897` passed x86, x64, and native ARM64 branch/metadata preflight, dependency/security checks, package validation, runtime self-tests, and real Setup install/uninstall smoke tests before the final combined release was assembled and validated.

The release contains canonical `setup.exe` and `portable.exe`, architecture-specific Setup/Portable/Host binaries, Portable ZIPs, `LICENSE.txt`, `RELEASE-MANIFEST.json`, and `SHA256SUMS.txt`. Current packages remain unsigned; no Authenticode claim is made.

The repository-owned `scripts/release-preflight.ps1` continues to enforce version alignment in normal CI and finalized release metadata plus branch/version alignment in the publication workflow.

## Post-release documentation review

After v0.9.1 publication, the complete maintained documentation set was reviewed for stale release-preparation wording and contradictions. README, CHANGELOG, ROADMAP, this index, and Release validation carry the publication status. Architecture, Build, Packaging, Security, Privacy, Dependencies, Accessibility, UI/UX, Windows, Host, and Remote Access continue to describe the same validated 0.9.1 runtime behavior and therefore do not require artificial status-only edits.

## Evidence policy

CI results, release assets and runtime screenshots must be described truthfully. Generated mockups are never labeled as runtime screenshots. Current packages must never be described as Authenticode-signed unless an authorized signing mechanism actually produced and verified that signature.
