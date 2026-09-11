# Ghost RDP Host

Ghost RDP Host is a separate, visible Windows application. It is not a stealth agent and the initial milestone does not install a service, add startup persistence, change firewall rules, or modify Remote Desktop configuration.

## Implemented in the initial foundation

The Host window reads and displays:

- computer name;
- operating system version string;
- current Windows user.

These values come from the local runtime and are not simulated.

## Planned readiness diagnostics

A later milestone will add real checks for Windows edition, RDP availability, Remote Desktop service state, configured RDP port, relevant Windows Firewall rules, NLA, network profile, hostname, LAN addresses, and private-network/VPN indicators where reliable APIs exist.

Each status must have a documented runtime source. Unknown states must be shown as unknown rather than guessed.
