# Roadmap

Status terminology: **implemented** means code exists and is covered by the current branch/CI; **development** means actively being built; **planned** means not yet exposed as a finished runtime feature; **unsupported** means intentionally excluded.

| Milestone | Scope | Status |
| --- | --- | --- |
| PR 1 | Solution, Core, App, Host, tests, docs, CI, branding, Windows UI foundation | development |
| PR 2 | Saved computers and Quick Connect with schema-versioned profiles | planned |
| PR 3 | Safe `mstsc.exe` launch integration and secure temporary `.rdp` handling | planned |
| PR 4 | Ghost RDP Host readiness diagnostics | planned |
| PR 5 | VPN/private-overlay and RD Gateway-aware remote access UX | planned |
| PR 6 | Settings, About expansion, accessibility and UI polish | planned |
| PR 7 | Windows Setup and Portable packaging | planned |
| PR 8 | Authentic Windows screenshots and final README/release polish | planned |

## Unsupported security shortcuts

The project does not plan stealth installation, hidden persistence, antivirus/Defender bypass, UAC bypass, keylogging, webcam/microphone spying, hidden screenshots, credential theft, automatic public 3389 exposure, or silent remote shell behavior.
