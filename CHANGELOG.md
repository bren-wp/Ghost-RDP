# Changelog

All notable user-facing changes to Ghost RDP are recorded here. Documentation is reviewed and synchronized with every functional change.

## 0.9.2 - 2026-09-12

Security, data-preservation, and installer-ownership hardening for the current multi-architecture Windows release line.

### Security and stability

- Stale temporary `.rdp` cleanup now considers only Ghost RDP-owned GUID session directories and leaves unrelated directories under the Ghost RDP temp root untouched.
- Setup now replaces an existing installation directory only when Windows Installed Apps identifies that exact canonical path as the registered Ghost RDP installation; an existing unregistered directory is never taken over.
- Setup refuses a second install path while another Ghost RDP installation is registered, and both normal uninstall and the temporary uninstall helper independently reject targets that do not match the registered `InstallLocation`.
- Installer smoke tests now preserve a foreign sentinel directory while exercising rejected install/uninstall/helper targets, accepted same-path reinstall/update, and the normal Windows uninstall lifecycle.
- Saved-computer writes now validate the current primary store before replacement and retain its previous valid contents as `computers.json.bak`; a corrupt or unsupported current store is never silently overwritten by a normal profile save.
- When the primary saved-computer store cannot be loaded and the backup validates successfully, Ghost RDP offers an explicit recovery choice after startup. Recovery validates the backup before use, preserves the unreadable primary file under a unique `preserved-*` name, and leaves the app read-only when recovery is declined or unavailable.
- Core regression tests cover backup rotation, corrupt-primary write blocking, explicit restore, preservation of the unreadable source, corrupt-backup rejection, and rejection of rollback while the primary store is valid.

### Release verification

- GitHub Release `v0.9.2` was published from exact source commit `96936fce056db87b1fc43f07e9500111a8e3b851` by Publish Release workflow run `34664726865`; tag `v0.9.2` resolves to the same commit and the release is neither a draft nor a prerelease.
- x86 and x64 passed release metadata/branch preflight, security/runtime-dependency checks, package validation, runtime self-tests, real Setup install/uninstall smoke tests, and artifact upload on the initial release attempt.
- The first native ARM64 attempt encountered a hosted-runner `.NET` bootstrap CLR failure before Ghost RDP preflight or code execution; the ARM64 retry on the same release SHA passed the complete release chain.
- The final publish job downloaded all three architecture packages, assembled and validated the combined release, read version 0.9.2, and published the GitHub Release.
- Published assets include canonical `setup.exe` and `portable.exe`, architecture-specific Setup/Portable/Host executables, x86/x64/ARM64 Portable ZIPs, `LICENSE.txt`, `RELEASE-MANIFEST.json`, and `SHA256SUMS.txt`.
- GitHub records SHA-256 digests for uploaded assets; packages remain unsigned until an authorized Authenticode signing mechanism is configured.

## 0.9.1 - 2026-09-12

UI/UX consistency, accessibility hardening, saved-computer responsiveness, stricter dependency policy, release-preflight validation, and documentation synchronization after the first production multi-architecture release.

### Changed

- App, Host, and Setup apply the Ghost RDP window theme directly to their concrete WPF window classes instead of depending on an implicit base `Window` style.
- Standard text receives an explicit readable foreground so dark cards cannot inherit black Windows default text.
- Text fields use a rounded Ghost RDP dark template with explicit focus and disabled states.
- Combo boxes use a project-owned dark template and popup instead of falling back to a white Windows theme surface.
- Sidebar navigation includes lightweight vector icons.
- Saved-computer lists explicitly enable WPF virtualization and recycling to reduce visual-container overhead for larger profile collections.
- Saved-computer text search debounces intermediate keystrokes before filtering/sorting, while sort and favorites changes remain immediate.
- Saved-computer refresh preserves the current selection when the selected profile remains visible and reuses a cached favorites total instead of recounting the full collection on every refresh.
- Saved-computer filtering and sorting are isolated in a deterministic App query component with dedicated tests.
- WPF title/taskbar windows use the Ghost RDP vector application mark.
- Runtime projects under `src/` have an explicit no-third-party-runtime-dependency policy enforced by the security/dependency regression script.
- README status presentation no longer relies on external badge-rendering services; product imagery remains repository-owned.
- Release validation includes a repository-owned preflight that verifies App/Host/Setup version alignment and blocks release publication when branch/version or release metadata is inconsistent.

### Documentation

- Added `docs/UI-UX.md` covering visual hierarchy, control behavior, memory/performance decisions, accessibility, and screenshot policy.
- Added `docs/DEPENDENCIES.md` documenting the .NET/WPF/Windows-only runtime dependency surface and CI enforcement.
- Added `docs/README.md` as the documentation index and synchronization policy.
- Updated Architecture, Build, Security, Privacy, Accessibility, Windows, Host, Remote Access, Packaging, Release, Release Notes, Roadmap, UI/UX, README, and Changelog for the 0.9.1 behavior and dependency baseline.
- Finalized the 0.9.1 release metadata and release-note state before creation of the exact `release/v0.9.1` branch.
- Performed a post-publication documentation review and updated status-bearing documents to identify 0.9.1 as the current production release.
- Runtime screenshots remain restricted to authentic validated Windows builds; no generated mockups are presented as application screenshots.

### Security and supply chain

- Production `src/` projects must not introduce third-party `PackageReference` or external file `HintPath` dependencies.
- Existing development-only Microsoft test tooling remains outside the shipped runtime dependency surface.
- Security regression checks continue to reject credential leakage, unsafe shell launch patterns, Host mutation paths, prohibited installer/network changes, and runtime dependency-policy violations.
- Release publication fails before packaging if App, Host, and Setup versions differ, if the release branch does not match `release/v<version>`, or if CHANGELOG/RELEASE-NOTES still contain development-only metadata.

### Release verification

- GitHub Release `v0.9.1` was published from exact source commit `c0ec01cf286ac422c0f2c41db1b456a2b0e62ca3` by Publish Release workflow run `34659493897`.
- x86, x64, and native ARM64 release jobs all passed release metadata/branch preflight, security/runtime-dependency checks, package validation, runtime self-tests, and real Setup install/uninstall smoke tests.
- The final publish job assembled and validated the combined release before publication.
- Published assets include canonical `setup.exe` and `portable.exe`, architecture-specific Setup/Portable/Host executables, x86/x64/ARM64 Portable ZIPs, `LICENSE.txt`, `RELEASE-MANIFEST.json`, and `SHA256SUMS.txt`.
- GitHub records SHA-256 digests for uploaded assets; packages remain unsigned until an authorized Authenticode signing mechanism is configured.

## 0.9.0 - 2026-09-11

Production packaging, stability hardening, and multi-architecture Windows support.

### Added

- Self-contained x86, x64, and ARM64 App, Host, Setup, and Portable packages.
- Canonical `setup.exe` and `portable.exe` x86 compatibility downloads for 32-bit Windows and x64 compatibility.
- Native ARM64 validation on GitHub's Windows ARM64 runner.
- Ghost RDP Setup with per-user installation, Start menu shortcuts, Windows Installed Apps registration, transactional staging, rollback, and optional local-data removal during uninstall.
- Runtime self-tests for App, Host, and Setup used by CI.
- PE-machine architecture checks and combined multi-architecture release assembly.
- Automatic GitHub Release publishing from exact `release/v<version>` branches.

### Changed

- App and Host production crash handling closes safely on unexpected UI exceptions instead of leaving an unstable process running.
- WPF rendering uses layout rounding and device-pixel snapping for crisper UI rendering.
- Disabled production buttons use a non-interactive cursor and clearer visual state.
- Release packaging no longer depends on Inno Setup.

### Setup and uninstall

- Windows uninstall uses the installed `GhostRDP-Setup.exe --uninstall` entry.
- No separate `uninstall.exe` or `unins*.exe` is shipped or installed.
- Saved computers and settings are preserved by default; interactive uninstall can explicitly remove them.

### Security and privacy

- No saved or command-line RDP passwords; Windows/Microsoft Remote Desktop owns credential entry.
- No telemetry, analytics, ads, central RDP relay, automatic VPN configuration, UPnP, router port forwarding, firewall weakening, NLA disabling, or hidden service installation.
- Current packages remain unsigned until an authorized Authenticode certificate or signing service is configured.

## 0.8.0 - 2026-09-11

Release-candidate polish after the first complete Windows MVP milestone set.

### Added

- Read-only Ghost RDP Host readiness diagnostics backed by Windows registry, Service Control Manager, Firewall policy, DNS, and network-interface state.
- Explicit Direct/LAN, existing private VPN/overlay, and RD Gateway connection routes.
- Schema-versioned UI settings, expanded About/runtime transparency, keyboard access, UI Automation metadata, and Windows High Contrast support.
- Self-contained x64 Portable client/Host executables, Portable ZIP, per-user installer, SHA-256 manifest, and install/uninstall CI smoke tests.

### Security and privacy

- No saved or command-line passwords; Windows/Microsoft Remote Desktop owns credential entry.
- Host readiness remains read-only and conservative: unavailable required diagnostics never produce a green Ready state.
