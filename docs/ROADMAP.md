# Roadmap

Status terminology: **implemented** means code exists and has passed repository CI; **development** means actively being built; **planned** means not yet exposed as a finished runtime feature; **unsupported** means intentionally excluded.

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
| PR 11 | Saved-computer filtering/sorting performance, debounce, selection preservation, and query tests | development |

## Production release

Version 0.9.0 is the current published production release. It replaces the previous x64-only third-party installer path with the repository-owned Ghost RDP Setup project and provides self-contained x86, x64, and ARM64 App/Host/Setup binaries, canonical `setup.exe` and `portable.exe` compatibility downloads, native ARM64 smoke testing, PE architecture validation, SHA-256 integrity checks, and Windows Installed Apps uninstall without a separate persistent uninstall executable.

## 0.9.1 development target

The current maintenance milestone focuses on visual correctness and efficiency: ensuring concrete WPF window classes always receive the Ghost RDP palette, preventing Windows default black text/white controls from leaking into dark views, using project-owned text-field/dropdown templates, adding scalable vector UI icons, recycling saved-computer list containers, and reducing unnecessary filter/sort work while users type. No security boundary changes are introduced by this UI/performance pass.

The saved-computer view keeps search responsive with a short debounce, leaves sort/favorites changes immediate, preserves selection when the selected profile remains visible, and avoids recounting favorites on each refresh. The query logic remains local and deterministic and introduces no additional runtime dependency.

## Documentation follow-up

Authentic Windows screenshots remain a documentation follow-up and are never replaced with generated mockups presented as runtime evidence. The README may use project branding and technical diagrams while runtime screenshot evidence still requires a real validated Windows build.

## Later work

Session-history UX remains deferred until a reliable runtime source can distinguish process launch from an authenticated/usable RDP session. Authenticode signing remains deferred until a real authorized signing certificate or signing service is available.

## Unsupported security shortcuts

Ghost RDP will not weaken Windows security controls or add hidden/unauthorized remote-access behavior. Private-network mode never configures a VPN or overlay automatically, RD Gateway support never places a password in Ghost RDP persistence or generated `.rdp` files, UI settings contain preferences rather than credential data, Host remains diagnostic-only, and packaging does not create firewall rules or a hidden service.
