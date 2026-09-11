# Security Model

## Credential policy

The default credential policy is **password memory-only**. Passwords and equivalent secrets must not be persisted in JSON, XML, SQLite plaintext, registry plaintext, logs, process arguments, `.rdp` files, or crash reports.

The current `mstsc.exe` integration uses an even narrower boundary: Ghost RDP does not ask for or transport a password at all. Microsoft Remote Desktop/Windows owns credential entry after the user explicitly starts a connection.

Future credential persistence may use Windows Credential Manager, DPAPI, or an appropriate Windows Hello-backed mechanism only after a dedicated security review.

## Process launch policy

Ghost RDP never builds a shell command string from user-controlled connection values. Microsoft RDP is launched through `ProcessStartInfo` with `UseShellExecute = false`, and the random temporary `.rdp` path is passed as a single `ArgumentList` element.

A password is never sent to `mstsc.exe` on the command line. Ghost RDP does not invoke `cmd.exe`, PowerShell, or another shell to start an RDP session.

## Temporary `.rdp` files

A user-initiated connection creates a unique session directory below the current user's temporary directory and a random `.rdp` filename. The file contains only validated connection metadata needed by Microsoft Remote Desktop, such as the full address and optional username/domain metadata.

The generated file:

- contains no password or equivalent secret;
- requests Windows-owned credential prompting;
- keeps RDP authentication and CredSSP support enabled;
- is deleted after the Microsoft RDP process exits;
- is eligible for stale-session cleanup after 24 hours if Ghost RDP terminates before normal cleanup.

## Remote exposure policy

Ghost RDP must not automatically:

- forward TCP 3389 on a router;
- use UPnP to expose RDP;
- disable Windows Firewall;
- disable Network Level Authentication;
- bypass certificate or security checks;
- weaken UAC or endpoint security.

Remote access from another network should use a private VPN/overlay network or an administrator-managed RD Gateway.

## Host policy

Ghost RDP Host is not a stealth agent. If a background service is introduced later, it must be clearly named, visible in Windows service/app management, documented, revocable, and uninstallable.

## Logging

Security-sensitive values must be redacted before logging. The shared `SecretSanitizer` is an initial defensive primitive; future structured logging must classify and exclude secret fields at the source.

## Security regression checks

CI rejects obvious command-line password patterns, RDP password-field patterns, shell-launch patterns, and missing security documentation. Tests cover input validation, profile persistence, temporary `.rdp` cleanup, structured process arguments, command-injection-shaped input, and the absence of password data from generated `.rdp` content.
