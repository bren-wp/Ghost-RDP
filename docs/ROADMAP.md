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
| PR 13 | 0.9.1 release metadata finalization and automated release-preflight validation | development |

## Production release

Version 0.9.0 remains the latest published production release until the exact `release/v0.9.1` workflow completes successfully and creates or updates the GitHub Release. It provides self-contained x86, x64, and ARM64 App/Host/Setup binaries, canonical `setup.exe` and `portable.exe` compatibility downloads, native ARM64 smoke testing, PE architecture validation, SHA-256 integrity checks, and Windows Installed Apps uninstall without a separate persistent uninstall executable.

## 0.9.1 release preparation

The 0.9.1 maintenance line focuses on visual correctness, accessibility, responsiveness, dependency discipline, and release-metadata integrity without changing the RDP credential/security boundary.

Implemented work includes deterministic WPF dark theming, project-owned text-field/dropdown templates, vector UI assets, saved-computer list recycling/virtualization, a short search debounce, deterministic query tests, selection preservation, cached favorite counts, and an enforced no-third-party-runtime-dependency policy.

Release preparation adds a repository-owned `release-preflight.ps1` gate. It verifies App/Host/Setup version alignment on normal CI and, for release publication, additionally verifies finalized CHANGELOG/RELEASE-NOTES metadata plus the exact `release/v<version>` branch name.

The release is not considered published merely because the source version is 0.9.1 or because release notes are finalized. Publication status changes only after the exact release workflow succeeds and the GitHub Release exists.

## Documentation policy

The maintained documentation set has a central index in `docs/README.md`. Functional changes are reviewed against README, CHANGELOG, ROADMAP, and every affected technical document in the same pull-request cycle. Release preparation additionally checks Build, Packaging, Release, Release Notes, Security, Privacy, and Dependencies for consistency.

Authentic Windows screenshots remain a documentation follow-up and are never replaced with generated mockups presented as runtime evidence. Repository-owned branding and technical diagrams may be used while authentic runtime captures are not yet committed.

## Later work

Session-history UX remains deferred until a reliable runtime source can distinguish process launch from an authenticated/usable RDP session. Authenticode signing remains deferred until a real authorized signing certificate or signing service is available.

A future feature requiring any third-party runtime dependency is not part of the current roadmap by default. It would require an explicit dependency-policy change plus security, privacy, licensing, maintenance, and supply-chain review before implementation.

## Unsupported security shortcuts

Ghost RDP will not weaken Windows security controls or add hidden/unauthorized remote-access behavior. Private-network mode never configures a VPN or overlay automatically, RD Gateway support never places a password in Ghost RDP persistence or generated `.rdp` files, UI settings contain preferences rather than credential data, Host remains diagnostic-only, and packaging does not create firewall rules or a hidden service.
