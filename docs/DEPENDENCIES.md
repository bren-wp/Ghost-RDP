# Runtime Dependencies

Ghost RDP intentionally keeps the shipped Windows runtime dependency surface limited to the platform and source code contained in this repository.

## Production runtime policy

The projects under `src/` must not add third-party NuGet packages or file-based external assembly references. Production code may depend on:

- .NET 8 runtime and base class libraries;
- WPF and Windows Desktop framework components supplied by Microsoft;
- Windows platform APIs such as the registry, Service Control Manager, Windows Firewall COM policy, DNS/network-interface APIs, and `mstsc.exe`;
- other Ghost RDP projects in this repository through `ProjectReference`.

`GhostRdp.App`, `GhostRdp.Core`, `GhostRdp.Host`, and `GhostRdp.Setup` therefore do not use `PackageReference` for runtime functionality.

## End-user runtime

Production x86, x64, and ARM64 release binaries are published self-contained. End users do not need to install a separate .NET runtime before running the packaged App, Host, or Setup binaries.

Microsoft Remote Desktop (`mstsc.exe`) remains a Windows-owned component. Ghost RDP detects and launches it; Ghost RDP does not bundle a replacement RDP engine or route session traffic through a third-party SDK or Ghost RDP relay.

## Build and test tooling

Repository tests may use the existing Microsoft test SDK/MSTest packages. Those packages are development-only and are not included as runtime application dependencies.

GitHub Actions workflows use the repository source, GitHub-hosted runners/actions, the .NET SDK, PowerShell, and the GitHub CLI available on the runner. Packaging does not require a third-party installer compiler.

The repository-owned release preflight uses PowerShell and local project/Markdown files only; it does not download a package, SDK, signing helper, or metadata service.

## Enforcement

`scripts/security-regression.ps1` scans project files under `src/` and fails CI if a runtime `PackageReference` or external file `HintPath` reference is introduced. This guard runs before production packaging in the normal CI and publication workflows.

`scripts/release-preflight.ps1` independently verifies App/Host/Setup version alignment. During release preparation it also validates finalized release metadata, and during publication it enforces the exact `release/v<version>` branch. This prevents dependency-clean binaries from being published under inconsistent version metadata.

A future request that genuinely requires a third-party runtime component must first change this documented policy explicitly and undergo a separate security, privacy, licensing, maintenance, and supply-chain review. It must not be introduced incidentally as part of another feature.

## Documentation dependency policy

README images and diagrams are stored in the repository. Documentation must not depend on external badge/image rendering services for core project status or product presentation. Runtime screenshots, when added, must be repository-owned evidence captured from a real validated Windows build.
