# Release Validation

This document defines the gates required before a Ghost RDP commit can be published as a production Windows release.

## Current verified production release

Ghost RDP **v0.9.2** is the current verified production release. It was published from exact source commit `96936fce056db87b1fc43f07e9500111a8e3b851` by Publish Release workflow run `34664726865`. Tag `v0.9.2` resolves to that exact commit; the GitHub Release is neither a draft nor a prerelease.

x86 and x64 completed release metadata/branch preflight, restore, security/runtime-dependency checks, package build/validation, runtime self-tests, and real Setup install/uninstall smoke testing on the initial release attempt. The initial native ARM64 job encountered a hosted-runner `.NET` bootstrap `Internal CLR error` before Ghost RDP release preflight or code execution. The ARM64 job was rerun on the same release SHA and then passed the complete release chain. The publish job downloaded all three architecture packages, assembled the combined release, validated it again, read version 0.9.2, and successfully created the GitHub Release.

The published release contains canonical `setup.exe` and `portable.exe`, architecture-specific Setup/Portable/Host executables, x86/x64/ARM64 Portable ZIPs, `LICENSE.txt`, `RELEASE-MANIFEST.json`, and `SHA256SUMS.txt`. GitHub records SHA-256 digests for uploaded assets. Current binaries remain unsigned.

## Required CI gates

The exact release commit must pass:

1. restore and format verification;
2. Release build for the complete solution;
3. automated tests, including saved-computer persistence/backup/recovery regressions;
4. security and runtime-dependency regression checks;
5. release-preflight version alignment;
6. framework-dependent App/Host package validation;
7. self-contained x86, x64, and ARM64 release packaging;
8. PE-machine architecture validation;
9. SHA-256 manifest validation;
10. Portable ZIP content validation;
11. App, Host, and Setup runtime self-tests;
12. real Setup install/update/uninstall smoke testing on x86 and x64-compatible Windows, including installation-target ownership regression checks;
13. real ARM64 runtime and Setup smoke testing on a native Windows ARM64 runner, including the same installation-target ownership checks;
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

A fresh installation may create a new target directory. Setup must not replace an arbitrary existing directory: an existing target is eligible for replacement only when its canonical path matches the current-user Ghost RDP `InstallLocation` registered in Windows Installed Apps. If Ghost RDP is registered at another path, Setup must reject a second installation target rather than orphaning the registered installation.

The normal uninstall bootstrap and the temporary uninstall helper must independently verify that the requested canonical target matches the registered Ghost RDP `InstallLocation` before recursive deletion. A missing registered program directory may be treated as stale installation metadata and cleaned without deleting an unrelated directory. A mismatched or unregistered target must not be removed.

Saved computers, their local backup/recovery copies, and settings remain user-owned data and are preserved by default. Interactive uninstall may remove the complete local-data directory only after explicit user selection.

## Runtime behavior validation

- App and Host start their production self-test path without crash.
- Saved-computer and Quick Connect persistence boundaries remain credential-free.
- A second successful saved-computer write preserves the exact previous validated primary generation as `computers.json.bak`.
- A normal saved-computer Save must reject an unreadable/unsupported current primary without modifying that primary or the existing backup.
- A corrupt/invalid backup must not be treated as recoverable, and recovery must be rejected while the current primary is valid so an older backup cannot silently roll data back.
- Explicit recovery must validate the backup, preserve an unreadable primary under a unique local recovery filename, restore a loadable primary, and retain the backup; the App must remain read-only if recovery is declined or fails.
- Saved-computer filtering/sorting remains deterministic and search debounce does not create a service/background worker.
- Direct, private-network, and RD Gateway routes continue to serialize only validated connection metadata.
- `mstsc.exe` remains the Windows-owned RDP runtime and credential prompt owner.
- Host diagnostics stay read-only and do not mutate RDP, services, firewall, NLA, VPN, or router state.
- Setup does not require elevation for its normal per-user installation path.
- Setup rollback protects an existing registered install from partial replacement when finalization fails.
- Setup must reject an existing unregistered target directory without changing its sentinel data, while same-path reinstall/update for the registered installation remains supported.
- Setup must reject a different second install path while another Ghost RDP installation is registered.
- Both the normal uninstall path and direct temporary-helper path must reject a mismatched target and leave unrelated sentinel data plus the registered installation intact.
- The normal Windows uninstall lifecycle must still remove the registered program directory and Installed Apps entry while preserving unrelated directories and, by default, the complete Ghost RDP local-data directory.

## Documentation release gate

Before release, review the complete [documentation index](README.md). README, CHANGELOG, ROADMAP, BUILD, PACKAGING, RELEASE, RELEASE-NOTES, SECURITY, PRIVACY and DEPENDENCIES must agree on the version, architecture matrix, dependency policy, signing status, uninstall behavior, persistence/recovery behavior, and known limitations.

Unchanged documents do not need artificial edits, but contradictions must be resolved before the release branch is created. After publication, status-bearing documents must be updated so they identify the actual current production release rather than a release-preparation state.

After v0.9.2 publication, README, CHANGELOG, ROADMAP, the documentation index, and this release document are reviewed for the published state. Architecture, Build, Packaging, Security, Privacy, Dependencies, Accessibility, UI/UX, Windows, Host, Remote Access and the finalized Release Notes already describe the validated 0.9.2 runtime behavior and do not require artificial status-only changes.

Post-release hardening can strengthen future release gates without changing the identity of the current verified production release. Such work remains unreleased until a later exact release commit passes every required architecture, integrity, runtime, installer, metadata, and publication gate above.

## Authentic screenshots

Runtime screenshots are documentation evidence, not a substitute for CI or package validation. Any screenshot added to the repository must come from the real application and must not contain private usernames, internal DNS names, public IP addresses, gateway names, or other infrastructure identifiers. Generated mockups must not be labeled as runtime screenshots.

## Known release limitation

Current 0.9.2 packages are unsigned. Authenticode signing remains deferred until an authorized certificate or signing service is available. The release process must never claim a signature that was not actually produced and verified.
