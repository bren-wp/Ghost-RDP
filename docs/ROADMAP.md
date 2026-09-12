# Roadmap

Status terminology: **implemented** means code exists and has passed repository CI; **development** means actively being built/validated; **planned** means not yet exposed as a finished runtime feature; **unsupported** means intentionally excluded.

| Milestone | Scope | Status |
| --- | --- | --- |
| PR 1 | Solution, Core, App, Host, tests, docs, CI, branding, Windows UI foundation | implemented |
| PR 2 | Saved computers and Quick Connect with schema-versioned profiles | implemented |
| PR 3 | Safe `mstsc.exe` launch integration and secure temporary `.rdp` handling | implemented |
| PR 4 | Ghost RDP Host read-only readiness diagnostics | implemented |
| PR 5 | VPN/private-overlay and RD Gateway-aware remote access UX | implemented |
| PR 6 | Settings, About expansion, accessibility and UI polish | implemented |
| PR 7 | Initial Windows Setup and Portable packaging | implemented |
| PR 8 | Release-candidate documentation and integrity checks | implemented |
| PR 9 | Production stability hardening, x86/x64/ARM64 packaging, integrated Setup/uninstall, and automated GitHub Releases | implemented |
| PR 10 | Deterministic dark theme, control templates, vector icons, list virtualization, and UI/UX documentation for 0.9.1 | implemented |
| PR 11 | Saved-computer filtering/sorting performance, debounce, selection preservation, and query tests | implemented |
| PR 12 | Documentation synchronization and enforced no-third-party-runtime-dependency policy | implemented |
| PR 13 | 0.9.1 release metadata finalization and automated release-preflight validation | implemented |

## Current production release

**Ghost RDP 0.9.1** is the current published production release. The `v0.9.1` GitHub Release was published from exact commit `c0ec01cf286ac422c0f2c41db1b456a2b0e62ca3` after Publish Release workflow run `34659493897` completed successfully.

The release provides self-contained x86, x64, and ARM64 App/Host/Setup binaries, canonical `setup.exe` and `portable.exe` compatibility downloads, architecture-specific Portable ZIPs, native ARM64 execution validation, PE architecture validation, SHA-256 integrity data, and Windows Installed Apps uninstall without a separate persistent uninstall executable.

All three architecture jobs passed release metadata/branch preflight, security/runtime-dependency checks, package validation, runtime self-tests, and real Setup install/uninstall smoke tests. The final publish job assembled and validated the combined release before publication.

## 0.9.1 maintenance line

The 0.9.1 maintenance release focuses on visual correctness, accessibility, responsiveness, dependency discipline, and release-metadata integrity without changing the RDP credential/security boundary.

Implemented work includes deterministic WPF dark theming, project-owned text-field/dropdown templates, vector UI assets, saved-computer list recycling/virtualization, a short search debounce, deterministic query tests, selection preservation, cached favorite counts, and an enforced no-third-party-runtime-dependency policy.

Post-release maintenance continues on the 0.9.1 line without a version bump where appropriate. The current hardening work scopes stale temporary `.rdp` cleanup to Ghost RDP-owned GUID session directories so unrelated directories below the temp root are never considered cleanup targets.

The repository-owned `release-preflight.ps1` gate verifies App/Host/Setup version alignment on normal CI and, for release publication, additionally verifies finalized CHANGELOG/RELEASE-NOTES metadata plus the exact `release/v<version>` branch name.

## Documentation policy

The maintained documentation set has a central index in `docs/README.md`. Functional changes are reviewed against README, CHANGELOG, ROADMAP, and every affected technical document in the same pull-request cycle. Release work additionally checks Build, Packaging, Release, Release Notes, Security, Privacy, and Dependencies for consistency.

The complete documentation set was reviewed after publication of v0.9.1. Status-bearing documents were updated to the published state; technical documents whose runtime behavior did not change were left unchanged rather than receiving artificial edits.

Authentic Windows screenshots remain a documentation follow-up and are never replaced with generated mockups presented as runtime evidence. Repository-owned branding and technical diagrams may be used while authentic runtime captures are not yet committed.

## Later work

Session-history UX remains deferred until a reliable runtime source can distinguish process launch from an authenticated/usable RDP session. Authenticode signing remains deferred until a real authorized signing certificate or signing service is available.

A future feature requiring any third-party runtime dependency is not part of the current roadmap by default. It would require an explicit dependency-policy change plus security, privacy, licensing, maintenance, and supply-chain review before implementation.

## Unsupported security shortcuts

Ghost RDP will not weaken Windows security controls or add hidden/unauthorized remote-access behavior. Private-network mode never configures a VPN or overlay automatically, RD Gateway support never places a password in Ghost RDP persistence or generated `.rdp` files, UI settings contain preferences rather than credential data, Host remains diagnostic-only, and packaging does not create firewall rules or a hidden service.
