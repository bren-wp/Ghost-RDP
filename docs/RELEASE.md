# Release Validation

This document defines the gates required before a Ghost RDP commit can be published as a production Windows release.

## Required CI gates

The exact release commit must pass:

1. restore and format verification;
2. Release build for the complete solution;
3. automated tests;
4. security and runtime-dependency regression checks;
5. release-preflight version alignment;
6. framework-dependent App/Host package validation;
7. self-contained x86, x64, and ARM64 release packaging;
8. PE-machine architecture validation;
9. SHA-256 manifest validation;
10. Portable ZIP content validation;
11. App, Host, and Setup runtime self-tests;
12. real Setup install/uninstall smoke testing on x86 and x64-compatible Windows;
13. real ARM64 runtime and Setup smoke testing on a native Windows ARM64 runner;
14. documentation synchronization review for README, CHANGELOG, ROADMAP and all affected domain documents.

A release branch must be named exactly `release/v<project-version>`. The release workflow refuses to publish a mismatched branch/version pair.

## Release preflight

`scripts/release-preflight.ps1` is the repository-owned release metadata guard.

Normal CI runs it without release-only switches to verify that Ghost RDP App, Host, and Setup carry the same version before packaging.

Release-preparation branches run:

```powershell
./scripts/release-preflight.ps1 -RequireReleaseMetadata
```

That additionally requires a dated CHANGELOG heading for the project version, an exact `# Ghost RDP <version>` release-notes heading, no development-only release wording, and a README reference to the version.

The publication workflow runs:

```powershell
./scripts/release-preflight.ps1 -RequireReleaseMetadata -RequireReleaseBranch
```

This adds the exact `release/v<version>` branch-name requirement. A version mismatch or stale release metadata stops publication before architecture packaging starts.

## Runtime dependency gate

Production projects below `src/` must remain free of third-party runtime `PackageReference` and external file-based assembly references. The security/dependency regression script enforces this before production packaging.

Development-only Microsoft test tooling is not part of the shipped runtime dependency surface. Production artifacts are self-contained .NET builds. See [DEPENDENCIES.md](DEPENDENCIES.md).

## Setup and uninstall contract

Every release must contain `setup.exe` and `portable.exe`, plus native architecture-specific artifacts. The canonical files are x86/32-bit compatibility builds, with native x64 and ARM64 builds available separately.

Setup must register Ghost RDP in Windows Installed Apps using the installed `GhostRDP-Setup.exe --uninstall` command. A distinct `uninstall.exe` or `unins*.exe` must not be shipped or installed. CI treats such a file as a release validation failure.

Saved computers and settings remain user-owned data and are preserved by default. Interactive uninstall may remove them only after explicit user selection.

## Runtime behavior validation

- App and Host start their production self-test path without crash.
- Saved-computer and Quick Connect persistence boundaries remain unchanged.
- Saved-computer filtering/sorting remains deterministic and search debounce does not create a service/background worker.
- Direct, private-network, and RD Gateway routes continue to serialize only validated connection metadata.
- `mstsc.exe` remains the Windows-owned RDP runtime and credential prompt owner.
- Host diagnostics stay read-only and do not mutate RDP, services, firewall, NLA, VPN, or router state.
- Setup does not require elevation for its normal per-user installation path.
- Setup rollback protects an existing install from partial replacement when finalization fails.

## Documentation release gate

Before release, review the complete [documentation index](README.md). README, CHANGELOG, ROADMAP, BUILD, PACKAGING, RELEASE, RELEASE-NOTES, SECURITY, PRIVACY and DEPENDENCIES must agree on the version, architecture matrix, dependency policy, signing status, uninstall behavior, and known limitations.

Unchanged documents do not need artificial edits, but contradictions must be resolved before the release branch is created.

Finalizing release notes does not mean the release is already published. README and ROADMAP must continue to report the actual latest GitHub Release until the publication workflow succeeds.

## Authentic screenshots

Runtime screenshots are documentation evidence, not a substitute for CI or package validation. Any screenshot added to the repository must come from the real application and must not contain private usernames, internal DNS names, public IP addresses, gateway names, or other infrastructure identifiers. Generated mockups must not be labeled as runtime screenshots.

## Known release limitation

Current packages are unsigned. Authenticode signing remains deferred until an authorized certificate or signing service is available. The release process must never claim a signature that was not actually produced and verified.
