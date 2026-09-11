# Privacy

Ghost RDP is designed for private remote desktop management of computers the user controls.

The initial architecture includes:

- no telemetry;
- no analytics;
- no ads;
- no fingerprinting;
- no sale of user data;
- no central Ghost RDP service carrying RDP traffic.

Saved computers are stored locally for the current Windows user under the user's local application-data directory. The JSON profile store contains connection metadata such as display name, host, port, username, domain, notes, favorites, and tags. It does not contain a password field.

Quick Connect validation is memory-only and does not silently create a saved profile. Saving occurs only when the user explicitly chooses `Save as computer`.

When the user explicitly starts a connection, Ghost RDP creates a random temporary `.rdp` file containing validated connection metadata needed by Microsoft Remote Desktop. The file does not contain a password. It is deleted after the Microsoft RDP process exits, with stale cleanup available for files left after an abnormal application termination.

Credential entry is owned by Windows/Microsoft Remote Desktop. Ghost RDP does not send RDP traffic through a Ghost RDP server and does not receive the password used by the current `mstsc.exe` launch flow.

A future relay, gateway, account system, synchronization service, or persistent credential feature would be a separate architecture decision and would require an updated threat model, privacy review, and documentation before release.
