# Privacy

Ghost RDP is designed for private remote desktop management of computers the user controls.

The current architecture includes:

- no telemetry;
- no analytics;
- no ads;
- no fingerprinting;
- no sale of user data;
- no central Ghost RDP service carrying RDP traffic.

Saved computers are stored locally for the current Windows user under the user's local application-data directory. The JSON profile store contains connection metadata such as display name, host, port, username, domain, remote-access route, optional RD Gateway hostname, notes, favorites, and tags. It does not contain a password, gateway password, token, or credential field.

Profile/store schema v2 adds route and gateway-host metadata. Known schema-v1 data is migrated in memory when read and is not rewritten merely because the application loaded it.

Quick Connect validation is memory-only and does not silently create a saved profile. Saving occurs only when the user explicitly chooses `Save as computer`.

When the user explicitly starts a connection, Ghost RDP creates a random temporary `.rdp` file containing validated connection metadata needed by Microsoft Remote Desktop. For RD Gateway connections this can include the gateway hostname and gateway-routing settings. The file does not contain a password or gateway access token. It is deleted after the Microsoft RDP process exits, with stale cleanup available for files left after an abnormal application termination.

Credential entry is owned by Windows/Microsoft Remote Desktop. Ghost RDP does not send RDP traffic through a Ghost RDP server and does not receive the password used by the current `mstsc.exe` launch flow.

Private VPN/overlay mode stores only the user's selected route intent. Ghost RDP does not read third-party VPN account credentials, install VPN software, or send a request to a Ghost RDP service to establish the tunnel.

Ghost RDP Host reads readiness information locally from Windows registry, service, firewall, DNS, and network-interface sources. These diagnostics can include the local computer/host name, Windows edition/build, current user name, RDP configuration, active firewall/network profiles, local IP addresses, and adapter names/descriptions. The diagnostic data is displayed locally and is not transmitted to Ghost RDP infrastructure.

A future relay, account system, synchronization service, or persistent credential feature would be a separate architecture decision and would require an updated threat model, privacy review, and documentation before release.
