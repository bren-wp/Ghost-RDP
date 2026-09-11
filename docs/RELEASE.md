# Release Validation

This document defines the release evidence required before Ghost RDP is described as production-ready.

## Required CI gates

A release candidate must pass the repository Windows CI from the exact commit that will be released:

1. restore;
2. format verification;
3. Release build;
4. automated tests;
5. security regression checks;
6. framework-dependent App/Host publish validation;
7. self-contained Setup/Portable packaging;
8. SHA-256 manifest validation;
9. Portable ZIP validation;
10. real silent Setup install/uninstall smoke test.

Do not merge or publish from a different head SHA than the one that passed these gates.

## Manual Windows validation

Before a public release, validate on a real supported Windows machine:

- App launches without crash and all primary navigation works;
- saved-computer add/edit/delete/duplicate/favorite/search/sort flows work;
- Quick Connect does not persist unless Save as computer is explicitly used;
- Direct, private-network, and RD Gateway routes generate the expected Microsoft RDP configuration;
- Connect remains disabled when `mstsc.exe` is unavailable;
- Microsoft Remote Desktop owns credential prompts and Ghost RDP never displays or stores a password;
- Host diagnostics refresh without modifying RDP, service, firewall, NLA, VPN, or router state;
- keyboard-only navigation, Windows High Contrast, DPI scaling, and Narrator-visible control names are checked;
- Setup installs and uninstalls cleanly and Portable mode runs without installation.

## Authentic screenshots

Screenshots must come from the exact Windows application build being documented. Do not use design mockups, generated UI, or edited images that could be mistaken for runtime evidence.

Capture at minimum:

- Saved computers view;
- Quick Connect view;
- saved-computer editor including remote-access route controls;
- Settings/About view;
- Ghost RDP Host readiness dashboard.

Before capture, use synthetic/non-sensitive sample computer names and addresses. Do not expose real usernames, internal DNS names, public IP addresses, gateway names, or other private infrastructure details.

## Release limitations that must stay visible

- Current development packages are unsigned.
- Ghost RDP does not prove that an RDP login/session succeeded merely because `mstsc.exe` started.
- VPN/private-overlay adapter detection is only an indicator and does not prove route reachability.
- Session-history UX remains deferred until a reliable runtime source can distinguish process launch from an authenticated/usable RDP session.
