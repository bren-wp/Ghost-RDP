# Roadmap

Status terminology: **implemented** means code exists and has passed the repository CI; **development** means actively being built; **planned** means not yet exposed as a finished runtime feature; **unsupported** means intentionally excluded.

| Milestone | Scope | Status |
| --- | --- | --- |
| PR 1 | Solution, Core, App, Host, tests, docs, CI, branding, Windows UI foundation | implemented |
| PR 2 | Saved computers and Quick Connect with schema-versioned profiles | implemented |
| PR 3 | Safe `mstsc.exe` launch integration and secure temporary `.rdp` handling | implemented |
| PR 4 | Ghost RDP Host read-only readiness diagnostics | implemented |
| PR 5 | VPN/private-overlay and RD Gateway-aware remote access UX | implemented |
| PR 6 | Settings, About expansion, accessibility and UI polish | implemented |
| PR 7 | Windows Setup and Portable packaging | implemented |
| PR 8 | Final README/release polish and authentic Windows screenshots | development |

## Current release-polish work

Version 0.8.0 aligns the App and Host release versions, adds changelog/release-validation documentation, and tightens release integrity checks. Authentic runtime screenshot evidence remains pending a real validated Windows capture environment and is not replaced with generated or design-mockup imagery.

## Later work

Session-history UX is intentionally deferred until a reliable runtime source can distinguish process launch from an authenticated/usable RDP session. Authenticode signing is also deferred until a real authorized signing certificate or signing service is available.

## Unsupported security shortcuts

Ghost RDP will not weaken Windows security controls or add hidden/unauthorized remote-access behavior. Private-network mode never configures a VPN or overlay automatically, RD Gateway support never places a password in Ghost RDP persistence or generated `.rdp` files, UI settings contain preferences rather than credential data, and packaging does not create firewall rules or a hidden service.
