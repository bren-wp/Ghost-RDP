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
| PR 15 | Temporary RDP cleanup ownership hardening | implemented |
| PR 16 | Setup target-ownership and uninstall data-loss protection | implemented |
| PR 17 | Saved-profile backup and explicit recovery hardening | implemented |
| PR 18 | 0.9.2 release metadata finalization | implemented |

## Current production release

**Ghost RDP 0.9.2** is the current published production release. GitHub Release `v0.9.2` was published from exact source commit `96936fce056db87b1fc43f07e9500111a8e3b851` by Publish Release workflow run `34664726865`. The release is neither a draft nor a prerelease, and tag `v0.9.2` resolves to the same commit.

The release provides self-contained x86, x64, and ARM64 App/Host/Setup binaries, canonical `setup.exe` and `portable.exe` compatibility downloads, architecture-specific Portable ZIPs, native ARM64 execution validation, PE architecture validation, SHA-256 integrity data, and Windows Installed Apps uninstall without a separate persistent uninstall executable.

x86 and x64 passed the complete release chain on the initial release attempt. Native ARM64 initially encountered a hosted-runner `.NET` bootstrap CLR failure before Ghost RDP preflight or code execution; the retry on the same release SHA passed release metadata/branch preflight, security/runtime-dependency checks, package validation, runtime self-tests, Setup install/uninstall smoke testing, and artifact upload. The final publish job assembled and validated all architecture packages before creating the release.

## 0.9.2 production line

Ghost RDP 0.9.2 packages the post-0.9.1 security and data-preservation work without changing the RDP credential or networking boundary.

The patch release includes scoped stale temporary `.rdp` cleanup, Setup install/uninstall target-ownership hardening, and saved-computer persistence recovery hardening. Temporary cleanup considers only Ghost RDP-owned GUID session directories. Setup only replaces or removes the canonical installation path registered in Windows Installed Apps, rejects a different second install target while an installation is registered, and revalidates ownership inside the temporary uninstall helper before recursive deletion.

Saved-computer persistence retains one previous validated primary generation as `computers.json.bak`, refuses normal writes over a primary store that cannot be safely loaded, and offers explicit recovery only when the backup itself validates. Recovery preserves the unreadable original before restoration and never silently rolls back a currently valid primary store.

Regression coverage exercises installer target ownership as well as profile backup rotation, corrupt-primary write blocking, explicit restore/preservation, invalid-backup rejection, and valid-primary rollback rejection.

The repository-owned `release-preflight.ps1` gate verifies App/Host/Setup version alignment on normal CI and, for release publication, additionally verifies finalized CHANGELOG/RELEASE-NOTES metadata plus the exact `release/v<version>` branch name.

## Documentation policy

The maintained documentation set has a central index in `docs/README.md`. Functional changes are reviewed against README, CHANGELOG, ROADMAP, and every affected technical document in the same pull-request cycle. Release work additionally checks Build, Packaging, Release, Release Notes, Security, Privacy, and Dependencies for consistency.

After publication of v0.9.2, status-bearing documents are synchronized to the published state. Technical documents whose runtime behavior did not change are left unchanged rather than receiving artificial edits.

Authentic Windows screenshots remain a documentation follow-up and are never replaced with generated mockups presented as runtime evidence. Repository-owned branding and technical diagrams may be used while authentic runtime captures are not yet committed.

## Later work

Session-history UX remains deferred until a reliable runtime source can distinguish process launch from an authenticated/usable RDP session. Authenticode signing remains deferred until a real authorized signing certificate or signing service is available.

A future feature requiring any third-party runtime dependency is not part of the current roadmap by default. It would require an explicit dependency-policy change plus security, privacy, licensing, maintenance, and supply-chain review before implementation.

## Unsupported security shortcuts

Ghost RDP will not weaken Windows security controls or add hidden/unauthorized remote-access behavior. Private-network mode never configures a VPN or overlay automatically, RD Gateway support never places a password in Ghost RDP persistence or generated `.rdp` files, UI settings contain preferences rather than credential data, Host remains diagnostic-only, and packaging does not create firewall rules or a hidden service.
