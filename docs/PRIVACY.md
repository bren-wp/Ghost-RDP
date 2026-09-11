# Privacy

Ghost RDP is designed for private remote desktop management of computers the user controls.

The current architecture includes:

- no telemetry;
- no analytics;
- no ads;
- no fingerprinting;
- no sale of user data;
- no central Ghost RDP service carrying RDP traffic;
- no third-party runtime telemetry or network SDK.

## Local saved-computer data

Saved computers are stored locally for the current Windows user under the user's local application-data directory. The JSON profile store contains connection metadata such as display name, host, port, username, domain, remote-access route, optional RD Gateway hostname, notes, favorites, and tags. It does not contain a password, gateway password, token, or credential field.

Profile/store schema v2 adds route and gateway-host metadata. Known schema-v1 data is migrated in memory when read and is not rewritten merely because the application loaded it.

Saved-computer text search, filtering, sorting, favorite counts, and selection restoration operate locally in memory. The 0.9.1 debounce does not send search terms, profile metadata, or usage data anywhere.

## Local UI settings

Ghost RDP UI settings are stored separately in `settings.json` under the current user's local application-data directory. The settings schema contains only UI preferences: startup view, default saved-computer sort, whether the last view should be remembered, and the last eligible view. It does not contain hostnames, usernames, domains, passwords, gateway hosts, tokens, or credential data.

Loading a missing settings file creates defaults in memory only; loading invalid or unsupported settings does not silently rewrite the source file.

## Quick Connect and temporary RDP data

Quick Connect validation is memory-only and does not silently create a saved profile. Saving occurs only when the user explicitly chooses `Save as computer`.

When the user explicitly starts a connection, Ghost RDP creates a random temporary `.rdp` file containing validated connection metadata needed by Microsoft Remote Desktop. For RD Gateway connections this can include the gateway hostname and gateway-routing settings. The file does not contain a password or gateway access token. It is deleted after the Microsoft RDP process exits, with stale cleanup available for files left after an abnormal application termination.

Credential entry is owned by Windows/Microsoft Remote Desktop. Ghost RDP does not send RDP traffic through a Ghost RDP server and does not receive the password used by the current `mstsc.exe` launch flow.

## Private networks and Host diagnostics

Private VPN/overlay mode stores only the user's selected route intent. Ghost RDP does not read third-party VPN account credentials, install VPN software, integrate a third-party VPN SDK, or send a request to a Ghost RDP service to establish the tunnel.

Ghost RDP Host reads readiness information locally from Windows registry, service, firewall, DNS, and network-interface sources. These diagnostics can include the local computer/host name, Windows edition/build, current user name, RDP configuration, active firewall/network profiles, local IP addresses, and adapter names/descriptions. The diagnostic data is displayed locally and is not transmitted to Ghost RDP infrastructure.

## Setup and uninstall

Setup and Portable packaging do not add telemetry or a Ghost RDP network service. Setup installs program files and Start menu shortcuts and registers Ghost RDP in Windows Installed Apps for the current user.

Normal uninstall preserves `%LOCALAPPDATA%\Ghost RDP\computers.json` and `settings.json` so removing the application does not silently destroy user-owned connection metadata or UI preferences. The interactive uninstall UI offers a separate, explicit option to remove that local Ghost RDP data when the user wants complete local-data deletion.

## Dependency privacy boundary

Production projects under `src/` do not include third-party runtime NuGet packages or external file-based assemblies. This reduces hidden telemetry/data-processing surfaces and is enforced by repository CI. See [DEPENDENCIES.md](DEPENDENCIES.md).

Existing Microsoft test tooling is used only for repository development/testing and is not shipped in production App/Host/Setup binaries.

## Future features

A future relay, account system, synchronization service, session-history service, or persistent credential feature would be a separate architecture decision and would require an updated threat model, privacy review, dependency review, and documentation before release.

## Documentation synchronization

Privacy-relevant behavior must be reflected in README, CHANGELOG, ROADMAP, Security, Dependencies, Release documentation, and this file in the same pull-request cycle. See [Documentation index](README.md).
